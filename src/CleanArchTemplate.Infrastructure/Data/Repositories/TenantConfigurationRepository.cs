using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.Interfaces;
using CleanArchTemplate.Infrastructure.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace CleanArchTemplate.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for tenant configuration operations
/// </summary>
public class TenantConfigurationRepository : BaseRepository<TenantConfiguration>, ITenantConfigurationRepository
{
    public TenantConfigurationRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Gets a configuration by tenant ID and key
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="key">The configuration key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The configuration if found</returns>
    public async Task<TenantConfiguration?> GetByTenantAndKeyAsync(Guid tenantId, string key, CancellationToken cancellationToken = default)
    {
        return await _context.Set<TenantConfiguration>()
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Key == key, cancellationToken);
    }

    /// <summary>
    /// Gets all configurations for a tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of configurations</returns>
    public async Task<IEnumerable<TenantConfiguration>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<TenantConfiguration>()
            .Where(c => c.TenantId == tenantId)
            .OrderBy(c => c.Key)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets configurations by tenant ID and key prefix
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="keyPrefix">The key prefix to search for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of configurations</returns>
    public async Task<IEnumerable<TenantConfiguration>> GetByTenantAndKeyPrefixAsync(Guid tenantId, string keyPrefix, CancellationToken cancellationToken = default)
    {
        return await _context.Set<TenantConfiguration>()
            .Where(c => c.TenantId == tenantId && c.Key.StartsWith(keyPrefix))
            .OrderBy(c => c.Key)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Checks if a configuration exists for a tenant and key
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="key">The configuration key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the configuration exists</returns>
    public async Task<bool> ExistsAsync(Guid tenantId, string key, CancellationToken cancellationToken = default)
    {
        return await _context.Set<TenantConfiguration>()
            .AnyAsync(c => c.TenantId == tenantId && c.Key == key, cancellationToken);
    }

    /// <summary>
    /// Removes a configuration by tenant ID and key
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="key">The configuration key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the configuration was removed</returns>
    public async Task<bool> RemoveByTenantAndKeyAsync(Guid tenantId, string key, CancellationToken cancellationToken = default)
    {
        var configuration = await GetByTenantAndKeyAsync(tenantId, key, cancellationToken);
        if (configuration == null)
        {
            return false;
        }

        _context.Set<TenantConfiguration>().Remove(configuration);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Gets system configurations for a tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of system configurations</returns>
    public async Task<IEnumerable<TenantConfiguration>> GetSystemConfigurationsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<TenantConfiguration>()
            .Where(c => c.TenantId == tenantId && c.IsSystemConfiguration)
            .OrderBy(c => c.Key)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets user configurations for a tenant (non-system)
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of user configurations</returns>
    public async Task<IEnumerable<TenantConfiguration>> GetUserConfigurationsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<TenantConfiguration>()
            .Where(c => c.TenantId == tenantId && !c.IsSystemConfiguration)
            .OrderBy(c => c.Key)
            .ToListAsync(cancellationToken);
    }
}