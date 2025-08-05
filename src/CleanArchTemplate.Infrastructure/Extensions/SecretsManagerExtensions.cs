using Amazon.SecretsManager;
using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Infrastructure.Services;
using CleanArchTemplate.Shared.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CleanArchTemplate.Infrastructure.Extensions;

/// <summary>
/// Extension methods for AWS Secrets Manager integration
/// </summary>
public static class SecretsManagerExtensions
{
    /// <summary>
    /// Adds AWS Secrets Manager as a configuration source
    /// </summary>
    /// <param name="builder">The configuration builder</param>
    /// <param name="secretNames">List of secret names to load</param>
    /// <param name="region">AWS region (optional, defaults to ap-south-1)</param>
    /// <param name="environment">Environment prefix for secret names (optional)</param>
    /// <param name="keyPrefix">Prefix to add to configuration keys (optional)</param>
    /// <param name="optional">Whether the secrets are optional (optional, defaults to true)</param>
    /// <returns>The configuration builder</returns>
    public static IConfigurationBuilder AddSecretsManager(
        this IConfigurationBuilder builder,
        IEnumerable<string> secretNames,
        string? region = null,
        string? environment = null,
        string? keyPrefix = null,
        bool optional = true)
    {
        if (secretNames == null || !secretNames.Any())
            throw new ArgumentException("At least one secret name must be provided", nameof(secretNames));

        var regionEndpoint = string.IsNullOrEmpty(region) 
            ? Amazon.RegionEndpoint.USEast1 
            : Amazon.RegionEndpoint.GetBySystemName(region);

        return builder.Add(new SecretsManagerConfigurationSource
        {
            Region = regionEndpoint,
            Environment = environment ?? string.Empty,
            SecretNames = secretNames.ToList(),
            KeyPrefix = keyPrefix ?? string.Empty,
            Optional = optional
        });
    }

    /// <summary>
    /// Adds AWS Secrets Manager as a configuration source for a single secret
    /// </summary>
    /// <param name="builder">The configuration builder</param>
    /// <param name="secretName">The secret name to load</param>
    /// <param name="region">AWS region (optional, defaults to ap-south-1)</param>
    /// <param name="environment">Environment prefix for secret name (optional)</param>
    /// <param name="keyPrefix">Prefix to add to configuration keys (optional)</param>
    /// <param name="optional">Whether the secret is optional (optional, defaults to true)</param>
    /// <returns>The configuration builder</returns>
    public static IConfigurationBuilder AddSecretsManager(
        this IConfigurationBuilder builder,
        string secretName,
        string? region = null,
        string? environment = null,
        string? keyPrefix = null,
        bool optional = true)
    {
        return builder.AddSecretsManager(
            new[] { secretName }, 
            region, 
            environment, 
            keyPrefix, 
            optional);
    }

    /// <summary>
    /// Adds AWS Secrets Manager services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddSecretsManager(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure settings
        services.Configure<SecretsManagerSettings>(
            configuration.GetSection(SecretsManagerSettings.SectionName));

        // Add memory cache for secrets caching
        services.AddMemoryCache();

        // Add AWS Secrets Manager client
        services.AddSingleton<IAmazonSecretsManager>(serviceProvider =>
        {
            var settings = serviceProvider.GetRequiredService<IOptions<SecretsManagerSettings>>().Value;
            var regionEndpoint = Amazon.RegionEndpoint.GetBySystemName(settings.Region);
            
            return new AmazonSecretsManagerClient(regionEndpoint);
        });

        // Add Secrets Manager service
        services.AddSingleton<ISecretsManagerService, SecretsManagerService>();

        return services;
    }

    /// <summary>
    /// Preloads secrets during application startup
    /// </summary>
    /// <param name="serviceProvider">The service provider</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    public static async Task PreloadSecretsAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var secretsService = serviceProvider.GetService<ISecretsManagerService>();
            if (secretsService is SecretsManagerService concreteService)
            {
                await concreteService.PreloadSecretsAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            var logger = serviceProvider.GetService<ILogger<SecretsManagerService>>();
            logger?.LogError(ex, "Failed to preload secrets during startup");
            
            // Don't throw here to prevent application startup failure
            // The secrets will be loaded on-demand instead
        }
    }

    /// <summary>
    /// Configures automatic secret rotation detection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddSecretRotationDetection(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHostedService<SecretRotationBackgroundService>();
        return services;
    }
}

/// <summary>
/// Background service for detecting and handling secret rotation
/// </summary>
public class SecretRotationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SecretRotationBackgroundService> _logger;
    private readonly SecretsManagerSettings _settings;

    public SecretRotationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<SecretRotationBackgroundService> logger,
        IOptions<SecretsManagerSettings> settings)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.EnableRotationDetection)
        {
            _logger.LogInformation("Secret rotation detection is disabled");
            return;
        }

        _logger.LogInformation("Starting secret rotation detection service");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckForRotatedSecretsAsync(stoppingToken);
                
                // Check every 5 minutes
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during secret rotation detection");
                
                // Wait before retrying
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        _logger.LogInformation("Secret rotation detection service stopped");
    }

    private async Task CheckForRotatedSecretsAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask; // Placeholder to avoid async warning
        using var scope = _serviceProvider.CreateScope();
        var secretsService = scope.ServiceProvider.GetService<ISecretsManagerService>();
        
        if (secretsService == null)
        {
            _logger.LogWarning("Secrets Manager service not available for rotation detection");
            return;
        }

        // This is a simplified implementation
        // In a real-world scenario, you would track secret versions and detect changes
        _logger.LogDebug("Checking for rotated secrets...");
        
        // For now, we'll just clear the cache periodically to ensure fresh secrets
        // This could be enhanced to track actual secret versions and rotation events
        if (_settings.PreloadSecrets.Any())
        {
            foreach (var secretName in _settings.PreloadSecrets)
            {
                try
                {
                    // Invalidate cache to force refresh on next access
                    secretsService.InvalidateCache(secretName);
                    _logger.LogDebug("Invalidated cache for secret {SecretName} for rotation check", secretName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to invalidate cache for secret {SecretName}", secretName);
                }
            }
        }
    }
}
