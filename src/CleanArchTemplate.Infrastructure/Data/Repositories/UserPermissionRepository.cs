using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.Enums;
using CleanArchTemplate.Domain.Interfaces;
using CleanArchTemplate.Infrastructure.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace CleanArchTemplate.Infrastructure.Data.Repositories;

/// <summary>
/// UserPermission repository implementation
/// </summary>
public class UserPermissionRepository : BaseRepository<UserPermission>, IUserPermissionRepository
{
    public UserPermissionRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<UserPermission>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(up => up.UserId == userId)
            .Include(up => up.Permission)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserPermission?> GetByUserAndPermissionAsync(Guid userId, Guid permissionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(up => up.Permission)
            .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionId == permissionId, cancellationToken);
    }

    public async Task<IEnumerable<UserPermission>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _dbSet
            .Where(up => up.UserId == userId && (!up.ExpiresAt.HasValue || up.ExpiresAt.Value > now))
            .Include(up => up.Permission)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<UserPermission>> GetUserPermissionsWithDetailsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(up => up.UserId == userId)
            .Include(up => up.Permission)
            .Include(up => up.User)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<UserPermission>> GetExpiringPermissionsAsync(DateTime before, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(up => up.ExpiresAt.HasValue && up.ExpiresAt.Value <= before)
            .Include(up => up.Permission)
            .Include(up => up.User)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<UserPermission>> GetByUserIdAndStateAsync(Guid userId, PermissionState state, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(up => up.UserId == userId && up.State == state)
            .Include(up => up.Permission)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<UserPermission>> GetByPermissionIdAsync(Guid permissionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(up => up.PermissionId == permissionId)
            .Include(up => up.Permission)
            .Include(up => up.User)
            .ToListAsync(cancellationToken);
    }

    public async Task BulkAddAsync(IEnumerable<UserPermission> userPermissions, CancellationToken cancellationToken = default)
    {
        if (userPermissions == null || !userPermissions.Any())
            return;

        await _dbSet.AddRangeAsync(userPermissions, cancellationToken);
    }

    public async Task BulkRemoveByUserAndPermissionsAsync(Guid userId, IEnumerable<Guid> permissionIds, CancellationToken cancellationToken = default)
    {
        if (permissionIds == null || !permissionIds.Any())
            return;

        var userPermissions = await _dbSet
            .Where(up => up.UserId == userId && permissionIds.Contains(up.PermissionId))
            .ToListAsync(cancellationToken);

        _dbSet.RemoveRange(userPermissions);
    }

    public async Task BulkUpdateStateAsync(IEnumerable<Guid> userPermissionIds, PermissionState state, string? reason = null, CancellationToken cancellationToken = default)
    {
        if (userPermissionIds == null || !userPermissionIds.Any())
            return;

        var userPermissions = await _dbSet
            .Where(up => userPermissionIds.Contains(up.Id))
            .ToListAsync(cancellationToken);

        foreach (var userPermission in userPermissions)
        {
            userPermission.UpdateState(state, reason);
        }

        _dbSet.UpdateRange(userPermissions);
    }

    public async Task BulkSetExpirationAsync(IEnumerable<Guid> userPermissionIds, DateTime? expiresAt, CancellationToken cancellationToken = default)
    {
        if (userPermissionIds == null || !userPermissionIds.Any())
            return;

        var userPermissions = await _dbSet
            .Where(up => userPermissionIds.Contains(up.Id))
            .ToListAsync(cancellationToken);

        foreach (var userPermission in userPermissions)
        {
            userPermission.SetExpiration(expiresAt);
        }

        _dbSet.UpdateRange(userPermissions);
    }

    public async Task<IEnumerable<UserPermission>> GetByUserIdsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default)
    {
        if (userIds == null || !userIds.Any())
            return Enumerable.Empty<UserPermission>();

        return await _dbSet
            .Where(up => userIds.Contains(up.UserId))
            .Include(up => up.Permission)
            .Include(up => up.User)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<UserPermission>> GetByPermissionIdsAsync(IEnumerable<Guid> permissionIds, CancellationToken cancellationToken = default)
    {
        if (permissionIds == null || !permissionIds.Any())
            return Enumerable.Empty<UserPermission>();

        return await _dbSet
            .Where(up => permissionIds.Contains(up.PermissionId))
            .Include(up => up.Permission)
            .Include(up => up.User)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<UserPermission> Items, int TotalCount)> GetPagedAsync(
        int pageNumber, 
        int pageSize, 
        Guid? userId = null,
        Guid? permissionId = null,
        PermissionState? state = null,
        bool? includeExpired = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        // Apply filters
        if (userId.HasValue)
        {
            query = query.Where(up => up.UserId == userId.Value);
        }

        if (permissionId.HasValue)
        {
            query = query.Where(up => up.PermissionId == permissionId.Value);
        }

        if (state.HasValue)
        {
            query = query.Where(up => up.State == state.Value);
        }

        if (includeExpired.HasValue && !includeExpired.Value)
        {
            var now = DateTime.UtcNow;
            query = query.Where(up => !up.ExpiresAt.HasValue || up.ExpiresAt.Value > now);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Include(up => up.Permission)
            .Include(up => up.User)
            .OrderBy(up => up.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(up => up.UserId == userId)
            .CountAsync(cancellationToken);
    }

    public async Task<int> CountByPermissionIdAsync(Guid permissionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(up => up.PermissionId == permissionId)
            .CountAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid userId, Guid permissionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(up => up.UserId == userId && up.PermissionId == permissionId, cancellationToken);
    }

    public async Task<bool> HasPermissionAsync(Guid permissionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(up => up.PermissionId == permissionId, cancellationToken);
    }
}