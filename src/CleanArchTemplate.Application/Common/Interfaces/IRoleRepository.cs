using CleanArchTemplate.Domain.Entities;

namespace CleanArchTemplate.Application.Common.Interfaces;

/// <summary>
/// Repository interface for Role entity
/// </summary>
public interface IRoleRepository : IRepository<Role>
{
    /// <summary>
    /// Gets a role by name
    /// </summary>
    /// <param name="name">The role name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The role if found, null otherwise</returns>
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets roles with their permissions
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Roles with their permissions</returns>
    Task<IEnumerable<Role>> GetRolesWithPermissionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a role with its permissions by ID
    /// </summary>
    /// <param name="id">The role ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The role with permissions if found, null otherwise</returns>
    Task<Role?> GetRoleWithPermissionsByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets roles for a specific user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User's roles</returns>
    Task<IEnumerable<Role>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a role name is already in use
    /// </summary>
    /// <param name="name">The role name to check</param>
    /// <param name="excludeRoleId">Role ID to exclude from the check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if role name exists, false otherwise</returns>
    Task<bool> IsNameExistsAsync(string name, Guid? excludeRoleId = null, CancellationToken cancellationToken = default);
}