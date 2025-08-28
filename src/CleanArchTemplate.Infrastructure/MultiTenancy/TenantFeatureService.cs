using CleanArchTemplate.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace CleanArchTemplate.Infrastructure.MultiTenancy;

/// <summary>
/// Service for tenant-specific feature flag management
/// </summary>
public class TenantFeatureService : ITenantFeatureService
{
    private readonly ITenantConfigurationService _configurationService;
    private readonly ILogger<TenantFeatureService> _logger;
    private const string FeaturePrefix = "feature.";

    public TenantFeatureService(
        ITenantConfigurationService configurationService,
        ILogger<TenantFeatureService> logger)
    {
        _configurationService = configurationService;
        _logger = logger;
    }

    /// <summary>
    /// Checks if a feature is enabled for the current tenant
    /// </summary>
    /// <param name="featureName">The feature name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the feature is enabled</returns>
    public async Task<bool> IsFeatureEnabledAsync(string featureName, CancellationToken cancellationToken = default)
    {
        try
        {
            var featureKey = GetFeatureKey(featureName);
            var featureConfig = await _configurationService.GetConfigurationAsync<FeatureConfiguration?>(featureKey, null, cancellationToken);
            
            var isEnabled = featureConfig?.Enabled ?? false;
            _logger.LogDebug("Feature {FeatureName} is {Status} for current tenant", featureName, isEnabled ? "enabled" : "disabled");
            
            return isEnabled;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if feature {FeatureName} is enabled for current tenant", featureName);
            return false;
        }
    }

    /// <summary>
    /// Checks if a feature is enabled for a specific tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="featureName">The feature name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the feature is enabled</returns>
    public async Task<bool> IsFeatureEnabledAsync(Guid tenantId, string featureName, CancellationToken cancellationToken = default)
    {
        try
        {
            var featureKey = GetFeatureKey(featureName);
            var featureConfig = await _configurationService.GetConfigurationAsync<FeatureConfiguration?>(tenantId, featureKey, null, cancellationToken);
            
            var isEnabled = featureConfig?.Enabled ?? false;
            _logger.LogDebug("Feature {FeatureName} is {Status} for tenant {TenantId}", featureName, isEnabled ? "enabled" : "disabled", tenantId);
            
            return isEnabled;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if feature {FeatureName} is enabled for tenant {TenantId}", featureName, tenantId);
            return false;
        }
    }

    /// <summary>
    /// Gets feature configuration for the current tenant
    /// </summary>
    /// <typeparam name="T">The type of the feature configuration</typeparam>
    /// <param name="featureName">The feature name</param>
    /// <param name="defaultValue">The default configuration if not found</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The feature configuration</returns>
    public async Task<T> GetFeatureConfigurationAsync<T>(string featureName, T defaultValue = default!, CancellationToken cancellationToken = default)
    {
        try
        {
            var featureKey = GetFeatureKey(featureName);
            var featureConfig = await _configurationService.GetConfigurationAsync<FeatureConfiguration?>(featureKey, null, cancellationToken);
            
            if (featureConfig?.Configuration == null)
            {
                _logger.LogDebug("Feature configuration not found for {FeatureName}, returning default value", featureName);
                return defaultValue;
            }

            if (featureConfig.Configuration is T directValue)
            {
                return directValue;
            }

            // Try to convert the configuration to the requested type
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<T>(System.Text.Json.JsonSerializer.Serialize(featureConfig.Configuration)) ?? defaultValue;
            }
            catch
            {
                _logger.LogWarning("Failed to convert feature configuration for {FeatureName} to type {Type}", featureName, typeof(T).Name);
                return defaultValue;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting feature configuration for {FeatureName}", featureName);
            return defaultValue;
        }
    }

    /// <summary>
    /// Gets feature configuration for a specific tenant
    /// </summary>
    /// <typeparam name="T">The type of the feature configuration</typeparam>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="featureName">The feature name</param>
    /// <param name="defaultValue">The default configuration if not found</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The feature configuration</returns>
    public async Task<T> GetFeatureConfigurationAsync<T>(Guid tenantId, string featureName, T defaultValue = default!, CancellationToken cancellationToken = default)
    {
        try
        {
            var featureKey = GetFeatureKey(featureName);
            var featureConfig = await _configurationService.GetConfigurationAsync<FeatureConfiguration?>(tenantId, featureKey, null, cancellationToken);
            
            if (featureConfig?.Configuration == null)
            {
                _logger.LogDebug("Feature configuration not found for {FeatureName} and tenant {TenantId}, returning default value", featureName, tenantId);
                return defaultValue;
            }

            if (featureConfig.Configuration is T directValue)
            {
                return directValue;
            }

            // Try to convert the configuration to the requested type
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<T>(System.Text.Json.JsonSerializer.Serialize(featureConfig.Configuration)) ?? defaultValue;
            }
            catch
            {
                _logger.LogWarning("Failed to convert feature configuration for {FeatureName} and tenant {TenantId} to type {Type}", featureName, tenantId, typeof(T).Name);
                return defaultValue;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting feature configuration for {FeatureName} and tenant {TenantId}", featureName, tenantId);
            return defaultValue;
        }
    }

    /// <summary>
    /// Enables a feature for the current tenant
    /// </summary>
    /// <param name="featureName">The feature name</param>
    /// <param name="configuration">Optional feature configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task EnableFeatureAsync(string featureName, object? configuration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var featureKey = GetFeatureKey(featureName);
            var featureConfig = new FeatureConfiguration
            {
                Enabled = true,
                Configuration = configuration,
                EnabledAt = DateTime.UtcNow
            };

            await _configurationService.SetConfigurationAsync(featureKey, featureConfig, cancellationToken);
            _logger.LogInformation("Enabled feature {FeatureName} for current tenant", featureName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling feature {FeatureName} for current tenant", featureName);
            throw;
        }
    }

    /// <summary>
    /// Enables a feature for a specific tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="featureName">The feature name</param>
    /// <param name="configuration">Optional feature configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task EnableFeatureAsync(Guid tenantId, string featureName, object? configuration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var featureKey = GetFeatureKey(featureName);
            var featureConfig = new FeatureConfiguration
            {
                Enabled = true,
                Configuration = configuration,
                EnabledAt = DateTime.UtcNow
            };

            await _configurationService.SetConfigurationAsync(tenantId, featureKey, featureConfig, cancellationToken);
            _logger.LogInformation("Enabled feature {FeatureName} for tenant {TenantId}", featureName, tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling feature {FeatureName} for tenant {TenantId}", featureName, tenantId);
            throw;
        }
    }

    /// <summary>
    /// Disables a feature for the current tenant
    /// </summary>
    /// <param name="featureName">The feature name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task DisableFeatureAsync(string featureName, CancellationToken cancellationToken = default)
    {
        try
        {
            var featureKey = GetFeatureKey(featureName);
            var existingConfig = await _configurationService.GetConfigurationAsync<FeatureConfiguration?>(featureKey, null, cancellationToken);
            
            var featureConfig = new FeatureConfiguration
            {
                Enabled = false,
                Configuration = existingConfig?.Configuration,
                EnabledAt = existingConfig?.EnabledAt,
                DisabledAt = DateTime.UtcNow
            };

            await _configurationService.SetConfigurationAsync(featureKey, featureConfig, cancellationToken);
            _logger.LogInformation("Disabled feature {FeatureName} for current tenant", featureName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling feature {FeatureName} for current tenant", featureName);
            throw;
        }
    }

    /// <summary>
    /// Disables a feature for a specific tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="featureName">The feature name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task DisableFeatureAsync(Guid tenantId, string featureName, CancellationToken cancellationToken = default)
    {
        try
        {
            var featureKey = GetFeatureKey(featureName);
            var existingConfig = await _configurationService.GetConfigurationAsync<FeatureConfiguration?>(tenantId, featureKey, null, cancellationToken);
            
            var featureConfig = new FeatureConfiguration
            {
                Enabled = false,
                Configuration = existingConfig?.Configuration,
                EnabledAt = existingConfig?.EnabledAt,
                DisabledAt = DateTime.UtcNow
            };

            await _configurationService.SetConfigurationAsync(tenantId, featureKey, featureConfig, cancellationToken);
            _logger.LogInformation("Disabled feature {FeatureName} for tenant {TenantId}", featureName, tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling feature {FeatureName} for tenant {TenantId}", featureName, tenantId);
            throw;
        }
    }

    /// <summary>
    /// Gets all enabled features for the current tenant
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of feature names and their configurations</returns>
    public async Task<Dictionary<string, object?>> GetAllEnabledFeaturesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var allConfigurations = await _configurationService.GetAllConfigurationAsync(cancellationToken);
            var enabledFeatures = new Dictionary<string, object?>();

            foreach (var kvp in allConfigurations)
            {
                if (kvp.Key.StartsWith(FeaturePrefix) && kvp.Value is FeatureConfiguration featureConfig && featureConfig.Enabled)
                {
                    var featureName = kvp.Key.Substring(FeaturePrefix.Length);
                    enabledFeatures[featureName] = featureConfig.Configuration;
                }
            }

            _logger.LogDebug("Found {Count} enabled features for current tenant", enabledFeatures.Count);
            return enabledFeatures;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all enabled features for current tenant");
            return new Dictionary<string, object?>();
        }
    }

    /// <summary>
    /// Gets all enabled features for a specific tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of feature names and their configurations</returns>
    public async Task<Dictionary<string, object?>> GetAllEnabledFeaturesAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var allConfigurations = await _configurationService.GetAllConfigurationAsync(tenantId, cancellationToken);
            var enabledFeatures = new Dictionary<string, object?>();

            foreach (var kvp in allConfigurations)
            {
                if (kvp.Key.StartsWith(FeaturePrefix) && kvp.Value is FeatureConfiguration featureConfig && featureConfig.Enabled)
                {
                    var featureName = kvp.Key.Substring(FeaturePrefix.Length);
                    enabledFeatures[featureName] = featureConfig.Configuration;
                }
            }

            _logger.LogDebug("Found {Count} enabled features for tenant {TenantId}", enabledFeatures.Count, tenantId);
            return enabledFeatures;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all enabled features for tenant {TenantId}", tenantId);
            return new Dictionary<string, object?>();
        }
    }

    /// <summary>
    /// Gets the configuration key for a feature
    /// </summary>
    /// <param name="featureName">The feature name</param>
    /// <returns>The configuration key</returns>
    private static string GetFeatureKey(string featureName)
    {
        return $"{FeaturePrefix}{featureName.ToLowerInvariant()}";
    }

    /// <summary>
    /// Internal class for feature configuration
    /// </summary>
    private class FeatureConfiguration
    {
        public bool Enabled { get; set; }
        public object? Configuration { get; set; }
        public DateTime? EnabledAt { get; set; }
        public DateTime? DisabledAt { get; set; }
    }
}