using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.Interfaces;
using CleanArchTemplate.Infrastructure.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace CleanArchTemplate.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for PermissionAuditLog entity
/// </summary>
public class PermissionAuditLogRepository : IPermissionAuditLogRepository
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<PermissionAuditLog> _dbSet;

    public PermissionAuditLogRepository(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = _context.Set<PermissionAuditLog>();
    }

    public async Task<PermissionAuditLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<IEnumerable<PermissionAuditLog>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(pal => pal.User)
            .Include(pal => pal.Role)
            .Include(pal => pal.Permission)
            .OrderByDescending(pal => pal.PerformedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<PermissionAuditLog> AddAsync(PermissionAuditLog entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        await _dbSet.AddAsync(entity, cancellationToken);
        return entity;
    }

    public Task UpdateAsync(PermissionAuditLog entity, CancellationToken cancellationToken = default)
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

    public IQueryable<PermissionAuditLog> Query()
    {
        return _dbSet.AsQueryable();
    }

    /// <summary>
    /// Gets audit logs by user ID
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of audit logs for the user</returns>
    public async Task<IEnumerable<PermissionAuditLog>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(pal => pal.Permission)
            .Where(pal => pal.UserId == userId)
            .OrderByDescending(pal => pal.PerformedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets audit logs by role ID
    /// </summary>
    /// <param name="roleId">The role ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of audit logs for the role</returns>
    public async Task<IEnumerable<PermissionAuditLog>> GetByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(pal => pal.Permission)
            .Where(pal => pal.RoleId == roleId)
            .OrderByDescending(pal => pal.PerformedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets audit logs by permission ID
    /// </summary>
    /// <param name="permissionId">The permission ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of audit logs for the permission</returns>
    public async Task<IEnumerable<PermissionAuditLog>> GetByPermissionIdAsync(Guid permissionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(pal => pal.User)
            .Include(pal => pal.Role)
            .Where(pal => pal.PermissionId == permissionId)
            .OrderByDescending(pal => pal.PerformedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets audit logs by action type
    /// </summary>
    /// <param name="action">The action type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of audit logs for the action</returns>
    public async Task<IEnumerable<PermissionAuditLog>> GetByActionAsync(string action, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(pal => pal.User)
            .Include(pal => pal.Role)
            .Include(pal => pal.Permission)
            .Where(pal => pal.Action == action)
            .OrderByDescending(pal => pal.PerformedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets audit logs within a date range
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of audit logs within the date range</returns>
    public async Task<IEnumerable<PermissionAuditLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(pal => pal.User)
            .Include(pal => pal.Role)
            .Include(pal => pal.Permission)
            .Where(pal => pal.PerformedAt >= startDate && pal.PerformedAt <= endDate)
            .OrderByDescending(pal => pal.PerformedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets audit logs by performer
    /// </summary>
    /// <param name="performedBy">The performer identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of audit logs by the performer</returns>
    public async Task<IEnumerable<PermissionAuditLog>> GetByPerformerAsync(string performedBy, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(pal => pal.User)
            .Include(pal => pal.Role)
            .Include(pal => pal.Permission)
            .Where(pal => pal.PerformedBy == performedBy)
            .OrderByDescending(pal => pal.PerformedAt)
            .ToListAsync(cancellationToken);
    }
}