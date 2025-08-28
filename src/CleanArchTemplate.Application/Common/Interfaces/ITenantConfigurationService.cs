namespace CleanArchTemplate.Application.Common.Interfaces;

/// <summary>
/// Interface for tenant-specific configuration management
/// </summary>
public interface ITenantConfigurationService
{
    /// <summary>
    /// Gets a configuration value for the current tenant
    /// </summary>
    /// <typeparam name="T">The type of the configuration value</typeparam>
    /// <param name="key">The configuration key</param>
    /// <param name="defaultValue">The default value if not found</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The configuration value</returns>
    Task<T> GetConfigurationAsync<T>(string key, T defaultValue = default!, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a configuration value for a specific tenant
    /// </summary>
    /// <typeparam name="T">The type of the configuration value</typeparam>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="key">The configuration key</param>
    /// <param name="defaultValue">The default value if not found</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The configuration value</returns>
    Task<T> GetConfigurationAsync<T>(Guid tenantId, string key, T defaultValue = default!, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a configuration value for the current tenant
    /// </summary>
    /// <typeparam name="T">The type of the configuration value</typeparam>
    /// <param name="key">The configuration key</param>
    /// <param name="value">The configuration value</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SetConfigurationAsync<T>(string key, T value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a configuration value for a specific tenant
    /// </summary>
    /// <typeparam name="T">The type of the configuration value</typeparam>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="key">The configuration key</param>
    /// <param name="value">The configuration value</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SetConfigurationAsync<T>(Guid tenantId, string key, T value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all configuration values for the current tenant
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of configuration key-value pairs</returns>
    Task<Dictionary<string, object>> GetAllConfigurationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all configuration values for a specific tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of configuration key-value pairs</returns>
    Task<Dictionary<string, object>> GetAllConfigurationAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a configuration value for the current tenant
    /// </summary>
    /// <param name="key">The configuration key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RemoveConfigurationAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a configuration value for a specific tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="key">The configuration key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RemoveConfigurationAsync(Guid tenantId, string key, CancellationToken cancellationToken = default);
}