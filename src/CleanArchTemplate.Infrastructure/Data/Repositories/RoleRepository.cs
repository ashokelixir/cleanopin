using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.Interfaces;
using CleanArchTemplate.Infrastructure.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace CleanArchTemplate.Infrastructure.Data.Repositories;

/// <summary>
/// Role repository implementation
/// </summary>
public class RoleRepository : IRoleRepository
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<Role> _dbSet;

    public RoleRepository(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = _context.Set<Role>();
    }

    public async Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        return await _dbSet
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken);
    }

    public async Task<IEnumerable<Role>> GetRolesWithPermissionsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .ToListAsync(cancellationToken);
    }

    public async Task<Role?> GetRoleWithPermissionsByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Role>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Include(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
            .Select(ur => ur.Role)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsNameExistsAsync(string name, Guid? excludeRoleId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        var query = _dbSet.Where(r => r.Name == name);

        if (excludeRoleId.HasValue)
        {
            query = query.Where(r => r.Id != excludeRoleId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .Include(r => r.UserRoles)
                .ThenInclude(ur => ur.User)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Role>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Role role, CancellationToken cancellationToken = default)
    {
        if (role == null)
            throw new ArgumentNullException(nameof(role));

        await _dbSet.AddAsync(role, cancellationToken);
    }

    public Task UpdateAsync(Role role, CancellationToken cancellationToken = default)
    {
        if (role == null)
            throw new ArgumentNullException(nameof(role));

        _dbSet.Update(role);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Role role, CancellationToken cancellationToken = default)
    {
        if (role == null)
            throw new ArgumentNullException(nameof(role));

        _dbSet.Remove(role);
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<Role>> GetByIdsAsync(IEnumerable<Guid> roleIds, CancellationToken cancellationToken = default)
    {
        if (roleIds == null)
            throw new ArgumentNullException(nameof(roleIds));

        var roleIdsList = roleIds.ToList();
        if (!roleIdsList.Any())
            return new List<Role>();

        return await _dbSet
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .Where(r => roleIdsList.Contains(r.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<Role?> GetByIdWithPermissionsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<bool> HasPermissionAsync(Guid permissionId, CancellationToken cancellationToken = default)
    {
        return await _context.RolePermissions
            .AnyAsync(rp => rp.PermissionId == permissionId, cancellationToken);
    }
}