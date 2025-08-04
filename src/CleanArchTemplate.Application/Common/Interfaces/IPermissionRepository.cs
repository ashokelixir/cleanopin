using CleanArchTemplate.Domain.Entities;

namespace CleanArchTemplate.Application.Common.Interfaces;

/// <summary>
/// Repository interface for Permission entity
/// </summary>
public interface IPermissionRepository : IRepository<Permission>
{
    /// <summary>
    /// Gets a permission by name
    /// </summary>
    /// <param name="name">The permission name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The permission if found, null otherwise</returns>
    Task<Permission?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets permissions by category
    /// </summary>
    /// <param name="category">The permission category</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Permissions in the specified category</returns>
    Task<IEnumerable<Permission>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets permissions for a specific role
    /// </summary>
    /// <param name="roleId">The role ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Role's permissions</returns>
    Task<IEnumerable<Permission>> GetRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets permissions for a specific user (through their roles)
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User's permissions</returns>
    Task<IEnumerable<Permission>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a permission name is already in use
    /// </summary>
    /// <param name="name">The permission name to check</param>
    /// <param name="excludePermissionId">Permission ID to exclude from the check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if permission name exists, false otherwise</returns>
    Task<bool> IsNameExistsAsync(string name, Guid? excludePermissionId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all permission categories
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of unique permission categories</returns>
    Task<IEnumerable<string>> GetCategoriesAsync(CancellationToken cancellationToken = default);
}