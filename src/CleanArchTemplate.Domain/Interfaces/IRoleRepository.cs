using CleanArchTemplate.Domain.Entities;

namespace CleanArchTemplate.Domain.Interfaces;

/// <summary>
/// Domain repository interface for Role entity
/// </summary>
public interface IRoleRepository
{
    /// <summary>
    /// Gets a role by ID
    /// </summary>
    /// <param name="id">The role ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The role if found, null otherwise</returns>
    Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all roles
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>All roles</returns>
    Task<IEnumerable<Role>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a role by name
    /// </summary>
    /// <param name="name">The role name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The role if found, null otherwise</returns>
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets roles for a specific user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User's roles</returns>
    Task<IEnumerable<Role>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a role with its permissions by ID
    /// </summary>
    /// <param name="id">The role ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The role with permissions if found, null otherwise</returns>
    Task<Role?> GetRoleWithPermissionsByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all roles with their permissions
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>All roles with permissions</returns>
    Task<IEnumerable<Role>> GetRolesWithPermissionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a role name is already in use
    /// </summary>
    /// <param name="name">The role name to check</param>
    /// <param name="excludeRoleId">Role ID to exclude from the check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if role name exists, false otherwise</returns>
    Task<bool> IsNameExistsAsync(string name, Guid? excludeRoleId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new role
    /// </summary>
    /// <param name="role">The role to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task AddAsync(Role role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing role
    /// </summary>
    /// <param name="role">The role to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task UpdateAsync(Role role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a role
    /// </summary>
    /// <param name="role">The role to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task RemoveAsync(Role role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets roles by multiple IDs
    /// </summary>
    /// <param name="roleIds">The role IDs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Roles with the specified IDs</returns>
    Task<IEnumerable<Role>> GetByIdsAsync(IEnumerable<Guid> roleIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a role with its permissions by ID
    /// </summary>
    /// <param name="id">The role ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The role with permissions if found, null otherwise</returns>
    Task<Role?> GetByIdWithPermissionsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a role has a specific permission
    /// </summary>
    /// <param name="permissionId">The permission ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if any role has the permission, false otherwise</returns>
    Task<bool> HasPermissionAsync(Guid permissionId, CancellationToken cancellationToken = default);
}