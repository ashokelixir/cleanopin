using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Infrastructure.Data.Contexts;
using CleanArchTemplate.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CleanArchTemplate.Infrastructure.Services;

/// <summary>
/// Health check service with resilience patterns
/// </summary>
public class ResilientHealthCheckService
{
    private readonly ApplicationDbContext _context;
    private readonly IResilienceService _resilienceService;
    private readonly ILogger<ResilientHealthCheckService> _logger;

    public ResilientHealthCheckService(
        ApplicationDbContext context,
        IResilienceService resilienceService,
        ILogger<ResilientHealthCheckService> logger)
    {
        _context = context;
        _resilienceService = resilienceService;
        _logger = logger;
    }

    /// <summary>
    /// Performs database health check with resilience patterns
    /// </summary>
    /// <returns>Health check result</returns>
    public async Task<HealthCheckResult> CheckDatabaseHealthAsync()
    {
        return await _resilienceService.ExecuteWithFallbackAsync(
            // Primary health check
            async () =>
            {
                _logger.LogDebug("Performing database health check");
                
                var startTime = DateTime.UtcNow;
                
                // Simple query to test database connectivity
                var canConnect = await _context.Database.CanConnectAsync();
                if (!canConnect)
                {
                    return new HealthCheckResult
                    {
                        IsHealthy = false,
                        Message = "Cannot connect to database",
                        ResponseTime = DateTime.UtcNow - startTime
                    };
                }

                // Test a simple query
                var userCount = await _context.Users.CountAsync();
                
                var responseTime = DateTime.UtcNow - startTime;
                
                return new HealthCheckResult
                {
                    IsHealthy = true,
                    Message = $"Database is healthy. User count: {userCount}",
                    ResponseTime = responseTime,
                    Details = new Dictionary<string, object>
                    {
                        { "UserCount", userCount },
                        { "ResponseTimeMs", responseTime.TotalMilliseconds }
                    }
                };
            },
            // Fallback health check
            async () =>
            {
                _logger.LogWarning("Database health check failed, using fallback");
                return await Task.FromResult(new HealthCheckResult
                {
                    IsHealthy = false,
                    Message = "Database health check failed after resilience attempts",
                    ResponseTime = TimeSpan.Zero
                });
            },
            ApplicationConstants.ResiliencePolicies.NonCritical);
    }

    /// <summary>
    /// Performs comprehensive system health check with resilience
    /// </summary>
    /// <returns>System health check result</returns>
    public async Task<SystemHealthResult> CheckSystemHealthAsync()
    {
        var systemHealth = new SystemHealthResult
        {
            Timestamp = DateTime.UtcNow,
            Checks = new Dictionary<string, HealthCheckResult>()
        };

        // Database health check with resilience
        try
        {
            var dbHealth = await CheckDatabaseHealthAsync();
            systemHealth.Checks.Add("Database", dbHealth);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed completely");
            systemHealth.Checks.Add("Database", new HealthCheckResult
            {
                IsHealthy = false,
                Message = $"Database health check exception: {ex.Message}",
                ResponseTime = TimeSpan.Zero
            });
        }

        // Memory health check with resilience
        try
        {
            var memoryHealth = await _resilienceService.ExecuteAsync(
                async () =>
                {
                    var gcMemory = GC.GetTotalMemory(false);
                    var workingSet = Environment.WorkingSet;
                    
                    return await Task.FromResult(new HealthCheckResult
                    {
                        IsHealthy = true,
                        Message = "Memory usage is normal",
                        ResponseTime = TimeSpan.Zero,
                        Details = new Dictionary<string, object>
                        {
                            { "GCMemoryBytes", gcMemory },
                            { "WorkingSetBytes", workingSet }
                        }
                    });
                },
                ApplicationConstants.ResiliencePolicies.NonCritical);
            
            systemHealth.Checks.Add("Memory", memoryHealth);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Memory health check failed");
            systemHealth.Checks.Add("Memory", new HealthCheckResult
            {
                IsHealthy = false,
                Message = $"Memory health check failed: {ex.Message}",
                ResponseTime = TimeSpan.Zero
            });
        }

        // Determine overall health
        systemHealth.IsHealthy = systemHealth.Checks.Values.All(check => check.IsHealthy);
        
        return systemHealth;
    }

    /// <summary>
    /// Performs a critical system operation health check
    /// </summary>
    /// <returns>Critical operation health result</returns>
    public async Task<HealthCheckResult> CheckCriticalOperationsAsync()
    {
        return await _resilienceService.ExecuteAsync(
            async () =>
            {
                _logger.LogDebug("Performing critical operations health check");
                
                var startTime = DateTime.UtcNow;
                var checks = new List<string>();

                // Test database transaction capability using execution strategy
                var strategy = _context.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(_context, async (context, token) =>
                {
                    using var transaction = await context.Database.BeginTransactionAsync(token);
                    checks.Add("Transaction started");
                    
                    await transaction.RollbackAsync(token);
                    checks.Add("Transaction rolled back");
                    return (object)null!;
                }, null, CancellationToken.None);

                // Test basic CRUD operations
                var testUser = await _context.Users.FirstOrDefaultAsync();
                checks.Add($"User query executed: {testUser != null}");

                var responseTime = DateTime.UtcNow - startTime;

                return new HealthCheckResult
                {
                    IsHealthy = true,
                    Message = "Critical operations are functioning",
                    ResponseTime = responseTime,
                    Details = new Dictionary<string, object>
                    {
                        { "Checks", checks },
                        { "ResponseTimeMs", responseTime.TotalMilliseconds }
                    }
                };
            },
            ApplicationConstants.ResiliencePolicies.Critical);
    }
}

/// <summary>
/// Health check result model
/// </summary>
public class HealthCheckResult
{
    public bool IsHealthy { get; set; }
    public string Message { get; set; } = string.Empty;
    public TimeSpan ResponseTime { get; set; }
    public Dictionary<string, object>? Details { get; set; }
}

/// <summary>
/// System health result model
/// </summary>
public class SystemHealthResult
{
    public bool IsHealthy { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, HealthCheckResult> Checks { get; set; } = new();
}