using CleanArchTemplate.Domain.Entities;

namespace CleanArchTemplate.Domain.Interfaces;

/// <summary>
/// Repository interface for tenant configuration operations
/// </summary>
public interface ITenantConfigurationRepository : IRepository<TenantConfiguration>
{
    /// <summary>
    /// Gets a configuration by tenant ID and key
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="key">The configuration key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The configuration if found</returns>
    Task<TenantConfiguration?> GetByTenantAndKeyAsync(Guid tenantId, string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all configurations for a tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of configurations</returns>
    Task<IEnumerable<TenantConfiguration>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets configurations by tenant ID and key prefix
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="keyPrefix">The key prefix to search for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of configurations</returns>
    Task<IEnumerable<TenantConfiguration>> GetByTenantAndKeyPrefixAsync(Guid tenantId, string keyPrefix, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a configuration exists for a tenant and key
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="key">The configuration key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the configuration exists</returns>
    Task<bool> ExistsAsync(Guid tenantId, string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a configuration by tenant ID and key
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="key">The configuration key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the configuration was removed</returns>
    Task<bool> RemoveByTenantAndKeyAsync(Guid tenantId, string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets system configurations for a tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of system configurations</returns>
    Task<IEnumerable<TenantConfiguration>> GetSystemConfigurationsAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets user configurations for a tenant (non-system)
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of user configurations</returns>
    Task<IEnumerable<TenantConfiguration>> GetUserConfigurationsAsync(Guid tenantId, CancellationToken cancellationToken = default);
}