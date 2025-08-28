using CleanArchTemplate.Application.Common.Models;

namespace CleanArchTemplate.Application.Common.Interfaces;

/// <summary>
/// Interface for tenant management operations
/// </summary>
public interface ITenantService
{
    /// <summary>
    /// Creates a new tenant
    /// </summary>
    /// <param name="name">The tenant name</param>
    /// <param name="identifier">The tenant identifier</param>
    /// <param name="connectionString">Optional connection string</param>
    /// <param name="configuration">Optional configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created tenant information</returns>
    Task<TenantInfo> CreateTenantAsync(string name, string identifier, string? connectionString = null, 
        Dictionary<string, object>? configuration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a tenant by its ID
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tenant information if found</returns>
    Task<TenantInfo?> GetTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a tenant by its identifier
    /// </summary>
    /// <param name="identifier">The tenant identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tenant information if found</returns>
    Task<TenantInfo?> GetTenantByIdentifierAsync(string identifier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all tenants
    /// </summary>
    /// <param name="activeOnly">Whether to return only active tenants</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of tenants</returns>
    Task<IEnumerable<TenantInfo>> GetAllTenantsAsync(bool activeOnly = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="name">The new name</param>
    /// <param name="configuration">The new configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated tenant information</returns>
    Task<TenantInfo> UpdateTenantAsync(Guid tenantId, string name, Dictionary<string, object>? configuration = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates a tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ActivateTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeactivateTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a tenant exists with the given identifier
    /// </summary>
    /// <param name="identifier">The tenant identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the tenant exists</returns>
    Task<bool> TenantExistsAsync(string identifier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the tenant's subscription expiry date
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="expiresAt">The expiry date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SetSubscriptionExpiryAsync(Guid tenantId, DateTime? expiresAt, CancellationToken cancellationToken = default);
}