using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.Interfaces;
using CleanArchTemplate.Infrastructure.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace CleanArchTemplate.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for RolePermission entity
/// </summary>
public class RolePermissionRepository : IRolePermissionRepository
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<RolePermission> _dbSet;

    public RolePermissionRepository(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = _context.Set<RolePermission>();
    }

    public async Task<RolePermission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<IEnumerable<RolePermission>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(rp => rp.Role)
            .Include(rp => rp.Permission)
            .ToListAsync(cancellationToken);
    }

    public async Task<RolePermission> AddAsync(RolePermission entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        await _dbSet.AddAsync(entity, cancellationToken);
        return entity;
    }

    public Task UpdateAsync(RolePermission entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        _dbSet.Update(entity);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null)
        {
            _dbSet.Remove(entity);
        }
    }

    public IQueryable<RolePermission> Query()
    {
        return _dbSet.AsQueryable();
    }

    /// <summary>
    /// Gets a role permission by role and permission IDs
    /// </summary>
    /// <param name="roleId">The role ID</param>
    /// <param name="permissionId">The permission ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The role permission if found, null otherwise</returns>
    public async Task<RolePermission?> GetByRoleAndPermissionAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId, cancellationToken);
    }

    /// <summary>
    /// Gets all role permissions for a specific role
    /// </summary>
    /// <param name="roleId">The role ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of role permissions</returns>
    public async Task<IEnumerable<RolePermission>> GetByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(rp => rp.Permission)
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets all role permissions for a specific permission
    /// </summary>
    /// <param name="permissionId">The permission ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of role permissions</returns>
    public async Task<IEnumerable<RolePermission>> GetByPermissionIdAsync(Guid permissionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(rp => rp.Role)
            .Where(rp => rp.PermissionId == permissionId)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets role permissions with role and permission details
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of role permissions with navigation properties</returns>
    public async Task<IEnumerable<RolePermission>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(rp => rp.Role)
            .Include(rp => rp.Permission)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Checks if a role has a specific permission
    /// </summary>
    /// <param name="roleId">The role ID</param>
    /// <param name="permissionId">The permission ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the role has the permission, false otherwise</returns>
    public async Task<bool> RoleHasPermissionAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId, cancellationToken);
    }

    /// <summary>
    /// Removes all permissions from a role
    /// </summary>
    /// <param name="roleId">The role ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task RemoveAllPermissionsFromRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var rolePermissions = await _dbSet
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync(cancellationToken);

        _dbSet.RemoveRange(rolePermissions);
    }

    /// <summary>
    /// Removes a permission from all roles
    /// </summary>
    /// <param name="permissionId">The permission ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task RemovePermissionFromAllRolesAsync(Guid permissionId, CancellationToken cancellationToken = default)
    {
        var rolePermissions = await _dbSet
            .Where(rp => rp.PermissionId == permissionId)
            .ToListAsync(cancellationToken);

        _dbSet.RemoveRange(rolePermissions);
    }
}