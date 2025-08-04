using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace CleanArchTemplate.Infrastructure.Services;

/// <summary>
/// Implementation of resilience service using Polly
/// </summary>
public class ResilienceService : IResilienceService
{
    private readonly ILogger<ResilienceService> _logger;
    private readonly ResilienceSettings _settings;
    private readonly ConcurrentDictionary<string, ResiliencePipeline> _pipelines;
    public ResilienceService(ILogger<ResilienceService> logger, IOptions<ResilienceSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
        _pipelines = new ConcurrentDictionary<string, ResiliencePipeline>();
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string policyName)
    {
        var pipeline = GetOrCreatePipeline(policyName);
        
        try
        {
            return await pipeline.ExecuteAsync(async _ => await operation());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Operation failed after resilience policies for policy: {PolicyName}", policyName);
            throw;
        }
    }

    public async Task ExecuteAsync(Func<Task> operation, string policyName)
    {
        var pipeline = GetOrCreatePipeline(policyName);
        
        try
        {
            await pipeline.ExecuteAsync(async _ => await operation());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Operation failed after resilience policies for policy: {PolicyName}", policyName);
            throw;
        }
    }

    public async Task<T> ExecuteWithFallbackAsync<T>(Func<Task<T>> operation, Func<Task<T>> fallback, string policyName)
    {
        try
        {
            return await ExecuteAsync(operation, policyName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Primary operation failed, executing fallback for policy: {PolicyName}", policyName);
            return await fallback();
        }
    }

    public T Execute<T>(Func<T> operation, string policyName)
    {
        var pipeline = GetOrCreatePipeline(policyName);
        
        try
        {
            return pipeline.Execute(_ => operation());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Synchronous operation failed after resilience policies for policy: {PolicyName}", policyName);
            throw;
        }
    }

    public void Execute(Action operation, string policyName)
    {
        var pipeline = GetOrCreatePipeline(policyName);
        
        try
        {
            pipeline.Execute(_ => operation());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Synchronous operation failed after resilience policies for policy: {PolicyName}", policyName);
            throw;
        }
    }

    private ResiliencePipeline GetOrCreatePipeline(string policyName)
    {
        return _pipelines.GetOrAdd(policyName, CreatePipeline);
    }

    private ResiliencePipeline CreatePipeline(string policyName)
    {
        var pipelineBuilder = new ResiliencePipelineBuilder();
        ConfigurePipeline(pipelineBuilder, policyName);
        return pipelineBuilder.Build();
    }



    private void ConfigurePipeline(ResiliencePipelineBuilder builder, string policyName)
    {
        // Add timeout strategy
        builder.AddTimeout(new TimeoutStrategyOptions
        {
            Timeout = GetTimeoutForPolicy(policyName),
            OnTimeout = args =>
            {
                _logger.LogWarning("Operation timed out for policy: {PolicyName}, Timeout: {Timeout}", 
                    policyName, args.Timeout);
                return default;
            }
        });

        // Add retry strategy
        builder.AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = _settings.Retry.MaxRetryAttempts,
            Delay = TimeSpan.FromMilliseconds(_settings.Retry.BaseDelayMs),
            MaxDelay = TimeSpan.FromMilliseconds(_settings.Retry.MaxDelayMs),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            OnRetry = args =>
            {
                _logger.LogWarning("Retry attempt {AttemptNumber} for policy: {PolicyName}, Exception: {Exception}", 
                    args.AttemptNumber, policyName, args.Outcome.Exception?.Message);
                return default;
            }
        });

        // Add circuit breaker strategy
        builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5, // 50% failure rate
            MinimumThroughput = _settings.CircuitBreaker.MinimumThroughput,
            SamplingDuration = TimeSpan.FromSeconds(_settings.CircuitBreaker.SamplingDurationSeconds),
            BreakDuration = TimeSpan.FromSeconds(_settings.CircuitBreaker.DurationOfBreakSeconds),
            OnOpened = args =>
            {
                _logger.LogWarning("Circuit breaker opened for policy: {PolicyName}", policyName);
                return default;
            },
            OnClosed = args =>
            {
                _logger.LogInformation("Circuit breaker closed for policy: {PolicyName}", policyName);
                return default;
            },
            OnHalfOpened = args =>
            {
                _logger.LogInformation("Circuit breaker half-opened for policy: {PolicyName}", policyName);
                return default;
            }
        });
    }

    private TimeSpan GetTimeoutForPolicy(string policyName)
    {
        return policyName.ToLowerInvariant() switch
        {
            "database" => TimeSpan.FromSeconds(_settings.Timeout.DatabaseTimeoutSeconds),
            "external-api" => TimeSpan.FromSeconds(_settings.Timeout.ExternalApiTimeoutSeconds),
            _ => TimeSpan.FromSeconds(_settings.Timeout.DefaultTimeoutSeconds)
        };
    }
}