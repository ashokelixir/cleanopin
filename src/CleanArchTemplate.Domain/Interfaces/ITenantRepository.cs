using CleanArchTemplate.Domain.Entities;

namespace CleanArchTemplate.Domain.Interfaces;

/// <summary>
/// Repository interface for tenant operations
/// </summary>
public interface ITenantRepository
{
    /// <summary>
    /// Gets a tenant by its ID
    /// </summary>
    /// <param name="id">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tenant if found</returns>
    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a tenant by its identifier
    /// </summary>
    /// <param name="identifier">The tenant identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tenant if found</returns>
    Task<Tenant?> GetByIdentifierAsync(string identifier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active tenants
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of active tenants</returns>
    Task<IEnumerable<Tenant>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all tenants
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of all tenants</returns>
    Task<IEnumerable<Tenant>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a tenant exists with the given identifier
    /// </summary>
    /// <param name="identifier">The tenant identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the tenant exists</returns>
    Task<bool> ExistsAsync(string identifier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new tenant
    /// </summary>
    /// <param name="tenant">The tenant to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The added tenant</returns>
    Task<Tenant> AddAsync(Tenant tenant, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing tenant
    /// </summary>
    /// <param name="tenant">The tenant to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated tenant</returns>
    Task<Tenant> UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a tenant
    /// </summary>
    /// <param name="tenant">The tenant to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteAsync(Tenant tenant, CancellationToken cancellationToken = default);
}