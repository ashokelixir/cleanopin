namespace CleanArchTemplate.Application.Common.Interfaces;

/// <summary>
/// Interface for tenant-specific feature flag management
/// </summary>
public interface ITenantFeatureService
{
    /// <summary>
    /// Checks if a feature is enabled for the current tenant
    /// </summary>
    /// <param name="featureName">The feature name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the feature is enabled</returns>
    Task<bool> IsFeatureEnabledAsync(string featureName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a feature is enabled for a specific tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="featureName">The feature name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the feature is enabled</returns>
    Task<bool> IsFeatureEnabledAsync(Guid tenantId, string featureName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets feature configuration for the current tenant
    /// </summary>
    /// <typeparam name="T">The type of the feature configuration</typeparam>
    /// <param name="featureName">The feature name</param>
    /// <param name="defaultValue">The default configuration if not found</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The feature configuration</returns>
    Task<T> GetFeatureConfigurationAsync<T>(string featureName, T defaultValue = default!, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets feature configuration for a specific tenant
    /// </summary>
    /// <typeparam name="T">The type of the feature configuration</typeparam>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="featureName">The feature name</param>
    /// <param name="defaultValue">The default configuration if not found</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The feature configuration</returns>
    Task<T> GetFeatureConfigurationAsync<T>(Guid tenantId, string featureName, T defaultValue = default!, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables a feature for the current tenant
    /// </summary>
    /// <param name="featureName">The feature name</param>
    /// <param name="configuration">Optional feature configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task EnableFeatureAsync(string featureName, object? configuration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables a feature for a specific tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="featureName">The feature name</param>
    /// <param name="configuration">Optional feature configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task EnableFeatureAsync(Guid tenantId, string featureName, object? configuration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disables a feature for the current tenant
    /// </summary>
    /// <param name="featureName">The feature name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DisableFeatureAsync(string featureName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disables a feature for a specific tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="featureName">The feature name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DisableFeatureAsync(Guid tenantId, string featureName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all enabled features for the current tenant
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of feature names and their configurations</returns>
    Task<Dictionary<string, object?>> GetAllEnabledFeaturesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all enabled features for a specific tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of feature names and their configurations</returns>
    Task<Dictionary<string, object?>> GetAllEnabledFeaturesAsync(Guid tenantId, CancellationToken cancellationToken = default);
}