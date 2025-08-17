using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.Interfaces;
using CleanArchTemplate.Infrastructure.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace CleanArchTemplate.Infrastructure.Data.Repositories;

/// <summary>
/// Permission repository implementation
/// </summary>
public class PermissionRepository : BaseRepository<Permission>, IPermissionRepository
{
    public PermissionRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Permission?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        return await _dbSet
            .FirstOrDefaultAsync(p => p.Name == name, cancellationToken);
    }

    public async Task<IEnumerable<Permission>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(category))
            return Enumerable.Empty<Permission>();

        return await _dbSet
            .Where(p => p.Category == category)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Permission>> GetRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        return await _context.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .Include(rp => rp.Permission)
            .Select(rp => rp.Permission)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Permission>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsNameExistsAsync(string name, Guid? excludePermissionId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        var query = _dbSet.Where(p => p.Name == name);

        if (excludePermissionId.HasValue)
        {
            query = query.Where(p => p.Id != excludePermissionId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<IEnumerable<string>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.IsActive)
            .Select(p => p.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(cancellationToken);
    }

    public override async Task<Permission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    public override async Task<IEnumerable<Permission>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Permission>> GetByResourceAsync(string resource, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(resource))
            return Enumerable.Empty<Permission>();

        return await _dbSet
            .Where(p => p.Resource == resource)
            .OrderBy(p => p.Action)
            .ToListAsync(cancellationToken);
    }

    public async Task<Permission?> GetByResourceAndActionAsync(string resource, string action, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(resource) || string.IsNullOrWhiteSpace(action))
            return null;

        return await _dbSet
            .FirstOrDefaultAsync(p => p.Resource == resource && p.Action == action, cancellationToken);
    }

    public async Task<IEnumerable<Permission>> GetHierarchicalPermissionsAsync(Guid permissionId, CancellationToken cancellationToken = default)
    {
        var permissions = new List<Permission>();
        var permission = await GetByIdAsync(permissionId, cancellationToken);
        
        if (permission != null)
        {
            permissions.Add(permission);
            // Add child permissions recursively
            await AddChildPermissionsRecursively(permission, permissions, cancellationToken);
        }

        return permissions;
    }

    public async Task<IEnumerable<Permission>> GetPermissionsWithHierarchyAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.ChildPermissions)
            .Include(p => p.ParentPermission)
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(string resource, string action, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(resource) || string.IsNullOrWhiteSpace(action))
            return false;

        return await _dbSet
            .AnyAsync(p => p.Resource == resource && p.Action == action, cancellationToken);
    }

    public override async Task<Permission> AddAsync(Permission permission, CancellationToken cancellationToken = default)
    {
        if (permission == null)
            throw new ArgumentNullException(nameof(permission));

        await _dbSet.AddAsync(permission, cancellationToken);
        return permission;
    }

    public override Task UpdateAsync(Permission permission, CancellationToken cancellationToken = default)
    {
        if (permission == null)
            throw new ArgumentNullException(nameof(permission));

        _dbSet.Update(permission);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Permission permission, CancellationToken cancellationToken = default)
    {
        if (permission == null)
            throw new ArgumentNullException(nameof(permission));

        _dbSet.Remove(permission);
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<Permission>> GetByResourcesAsync(IEnumerable<string> resources, CancellationToken cancellationToken = default)
    {
        if (resources == null || !resources.Any())
            return Enumerable.Empty<Permission>();

        return await _dbSet
            .Where(p => resources.Contains(p.Resource))
            .OrderBy(p => p.Resource)
            .ThenBy(p => p.Action)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Permission>> GetByActionAsync(string action, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(action))
            return Enumerable.Empty<Permission>();

        return await _dbSet
            .Where(p => p.Action == action)
            .OrderBy(p => p.Resource)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Permission>> GetByActionsAsync(IEnumerable<string> actions, CancellationToken cancellationToken = default)
    {
        if (actions == null || !actions.Any())
            return Enumerable.Empty<Permission>();

        return await _dbSet
            .Where(p => actions.Contains(p.Action))
            .OrderBy(p => p.Resource)
            .ThenBy(p => p.Action)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Permission>> GetByResourceAndActionsAsync(string resource, IEnumerable<string> actions, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(resource) || actions == null || !actions.Any())
            return Enumerable.Empty<Permission>();

        return await _dbSet
            .Where(p => p.Resource == resource && actions.Contains(p.Action))
            .OrderBy(p => p.Action)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<string>> GetResourcesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.IsActive)
            .Select(p => p.Resource)
            .Distinct()
            .OrderBy(r => r)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<string>> GetActionsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.IsActive)
            .Select(p => p.Action)
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<string>> GetActionsByResourceAsync(string resource, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(resource))
            return Enumerable.Empty<string>();

        return await _dbSet
            .Where(p => p.Resource == resource && p.IsActive)
            .Select(p => p.Action)
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<Permission> Items, int TotalCount)> GetPagedAsync(
        int pageNumber, 
        int pageSize, 
        string? searchTerm = null, 
        string? resource = null, 
        string? action = null, 
        string? category = null, 
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(p => p.Name.Contains(searchTerm) || p.Description.Contains(searchTerm));
        }

        if (!string.IsNullOrWhiteSpace(resource))
        {
            query = query.Where(p => p.Resource == resource);
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(p => p.Action == action);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(p => p.Category == category);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Resource)
            .ThenBy(p => p.Action)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task BulkAddAsync(IEnumerable<Permission> permissions, CancellationToken cancellationToken = default)
    {
        if (permissions == null || !permissions.Any())
            return;

        await _dbSet.AddRangeAsync(permissions, cancellationToken);
    }

    public async Task BulkUpdateAsync(IEnumerable<Permission> permissions, CancellationToken cancellationToken = default)
    {
        if (permissions == null || !permissions.Any())
            return;

        _dbSet.UpdateRange(permissions);
    }

    public async Task BulkActivateAsync(IEnumerable<Guid> permissionIds, CancellationToken cancellationToken = default)
    {
        if (permissionIds == null || !permissionIds.Any())
            return;

        var permissions = await _dbSet
            .Where(p => permissionIds.Contains(p.Id) && !p.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var permission in permissions)
        {
            permission.Activate();
        }

        _dbSet.UpdateRange(permissions);
    }

    public async Task BulkDeactivateAsync(IEnumerable<Guid> permissionIds, CancellationToken cancellationToken = default)
    {
        if (permissionIds == null || !permissionIds.Any())
            return;

        var permissions = await _dbSet
            .Where(p => permissionIds.Contains(p.Id) && p.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var permission in permissions)
        {
            permission.Deactivate();
        }

        _dbSet.UpdateRange(permissions);
    }

    public async Task<IEnumerable<Permission>> GetByIdsAsync(IEnumerable<Guid> permissionIds, CancellationToken cancellationToken = default)
    {
        if (permissionIds == null || !permissionIds.Any())
            return Enumerable.Empty<Permission>();

        return await _dbSet
            .Where(p => permissionIds.Contains(p.Id))
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Resource)
            .ThenBy(p => p.Action)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Permission>> GetActiveByResourcesAsync(IEnumerable<string> resources, CancellationToken cancellationToken = default)
    {
        if (resources == null || !resources.Any())
            return Enumerable.Empty<Permission>();

        return await _dbSet
            .Where(p => resources.Contains(p.Resource) && p.IsActive)
            .OrderBy(p => p.Resource)
            .ThenBy(p => p.Action)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Permission>> GetActiveByActionsAsync(IEnumerable<string> actions, CancellationToken cancellationToken = default)
    {
        if (actions == null || !actions.Any())
            return Enumerable.Empty<Permission>();

        return await _dbSet
            .Where(p => actions.Contains(p.Action) && p.IsActive)
            .OrderBy(p => p.Resource)
            .ThenBy(p => p.Action)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountByResourceAsync(string resource, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(resource))
            return 0;

        return await _dbSet
            .Where(p => p.Resource == resource && p.IsActive)
            .CountAsync(cancellationToken);
    }

    public async Task<int> CountByActionAsync(string action, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(action))
            return 0;

        return await _dbSet
            .Where(p => p.Action == action && p.IsActive)
            .CountAsync(cancellationToken);
    }

    public async Task<int> CountByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(category))
            return 0;

        return await _dbSet
            .Where(p => p.Category == category && p.IsActive)
            .CountAsync(cancellationToken);
    }

    private async Task AddChildPermissionsRecursively(Permission parent, List<Permission> permissions, CancellationToken cancellationToken)
    {
        var children = await _dbSet
            .Where(p => p.ParentPermissionId == parent.Id)
            .ToListAsync(cancellationToken);

        foreach (var child in children)
        {
            if (!permissions.Any(p => p.Id == child.Id))
            {
                permissions.Add(child);
                await AddChildPermissionsRecursively(child, permissions, cancellationToken);
            }
        }
    }
}