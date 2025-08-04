using CleanArchTemplate.Domain.Common;

namespace CleanArchTemplate.Domain.Entities;

/// <summary>
/// Junction entity representing the many-to-many relationship between roles and permissions
/// </summary>
public class RolePermission : BaseAuditableEntity
{
    /// <summary>
    /// The role ID
    /// </summary>
    public Guid RoleId { get; private set; }

    /// <summary>
    /// The permission ID
    /// </summary>
    public Guid PermissionId { get; private set; }

    /// <summary>
    /// Navigation property to the role
    /// </summary>
    public Role Role { get; private set; } = null!;

    /// <summary>
    /// Navigation property to the permission
    /// </summary>
    public Permission Permission { get; private set; } = null!;

    // Private constructor for EF Core
    private RolePermission() { }

    /// <summary>
    /// Creates a new role-permission relationship
    /// </summary>
    /// <param name="roleId">The role ID</param>
    /// <param name="permissionId">The permission ID</param>
    public RolePermission(Guid roleId, Guid permissionId)
    {
        if (roleId == Guid.Empty)
            throw new ArgumentException("Role ID cannot be empty.", nameof(roleId));

        if (permissionId == Guid.Empty)
            throw new ArgumentException("Permission ID cannot be empty.", nameof(permissionId));

        RoleId = roleId;
        PermissionId = permissionId;
    }
}