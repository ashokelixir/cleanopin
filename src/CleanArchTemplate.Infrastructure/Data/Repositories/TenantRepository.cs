using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.Interfaces;
using CleanArchTemplate.Infrastructure.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace CleanArchTemplate.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for tenant operations
/// </summary>
public class TenantRepository : ITenantRepository
{
    private readonly ApplicationDbContext _context;

    public TenantRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets a tenant by its ID
    /// </summary>
    /// <param name="id">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tenant if found</returns>
    public async Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    /// <summary>
    /// Gets a tenant by its identifier
    /// </summary>
    /// <param name="identifier">The tenant identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tenant if found</returns>
    public async Task<Tenant?> GetByIdentifierAsync(string identifier, CancellationToken cancellationToken = default)
    {
        return await _context.Tenants
            .FirstOrDefaultAsync(t => t.Identifier == identifier.ToLowerInvariant(), cancellationToken);
    }

    /// <summary>
    /// Gets all active tenants
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of active tenants</returns>
    public async Task<IEnumerable<Tenant>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Tenants
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets all tenants
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of all tenants</returns>
    public async Task<IEnumerable<Tenant>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Tenants
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Checks if a tenant exists with the given identifier
    /// </summary>
    /// <param name="identifier">The tenant identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the tenant exists</returns>
    public async Task<bool> ExistsAsync(string identifier, CancellationToken cancellationToken = default)
    {
        return await _context.Tenants
            .AnyAsync(t => t.Identifier == identifier.ToLowerInvariant(), cancellationToken);
    }

    /// <summary>
    /// Adds a new tenant
    /// </summary>
    /// <param name="tenant">The tenant to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The added tenant</returns>
    public async Task<Tenant> AddAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync(cancellationToken);
        return tenant;
    }

    /// <summary>
    /// Updates an existing tenant
    /// </summary>
    /// <param name="tenant">The tenant to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated tenant</returns>
    public async Task<Tenant> UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        _context.Tenants.Update(tenant);
        await _context.SaveChangesAsync(cancellationToken);
        return tenant;
    }

    /// <summary>
    /// Deletes a tenant
    /// </summary>
    /// <param name="tenant">The tenant to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task DeleteAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        _context.Tenants.Remove(tenant);
        await _context.SaveChangesAsync(cancellationToken);
    }
}