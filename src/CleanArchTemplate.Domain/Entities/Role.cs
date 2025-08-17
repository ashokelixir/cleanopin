using CleanArchTemplate.Domain.Common;
using CleanArchTemplate.Domain.Events;

namespace CleanArchTemplate.Domain.Entities;

/// <summary>
/// Represents a role in the system
/// </summary>
public class Role : BaseAuditableEntity
{
    private readonly List<UserRole> _userRoles = new();
    private readonly List<RolePermission> _rolePermissions = new();

    /// <summary>
    /// The role name
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// The role description
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Indicates whether the role is active
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Navigation property for user roles
    /// </summary>
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    /// <summary>
    /// Navigation property for role permissions
    /// </summary>
    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();

    // Private constructor for EF Core
    private Role() { }

    /// <summary>
    /// Creates a new role
    /// </summary>
    /// <param name="name">The role name</param>
    /// <param name="description">The role description</param>
    public Role(string name, string description)
    {
        Name = name?.Trim() ?? throw new ArgumentNullException(nameof(name));
        Description = description?.Trim() ?? throw new ArgumentNullException(nameof(description));

        if (string.IsNullOrWhiteSpace(Name))
            throw new ArgumentException("Role name cannot be empty.", nameof(name));

        if (Name.Length > 100)
            throw new ArgumentException("Role name cannot exceed 100 characters.", nameof(name));

        if (Description.Length > 500)
            throw new ArgumentException("Role description cannot exceed 500 characters.", nameof(description));

        AddDomainEvent(new RoleCreatedEvent(Id, Name, Description));
    }

    /// <summary>
    /// Updates the role information
    /// </summary>
    /// <param name="name">The new role name</param>
    /// <param name="description">The new role description</param>
    public void Update(string name, string description)
    {
        var oldName = Name;
        var oldDescription = Description;

        Name = name?.Trim() ?? throw new ArgumentNullException(nameof(name));
        Description = description?.Trim() ?? throw new ArgumentNullException(nameof(description));

        if (string.IsNullOrWhiteSpace(Name))
            throw new ArgumentException("Role name cannot be empty.", nameof(name));

        if (Name.Length > 100)
            throw new ArgumentException("Role name cannot exceed 100 characters.", nameof(name));

        if (Description.Length > 500)
            throw new ArgumentException("Role description cannot exceed 500 characters.", nameof(description));

        AddDomainEvent(new RoleUpdatedEvent(Id, oldName, Name, oldDescription, Description));
    }

    /// <summary>
    /// Activates the role
    /// </summary>
    public void Activate()
    {
        if (IsActive)
            return;

        IsActive = true;
        AddDomainEvent(new RoleActivatedEvent(Id, Name));
    }

    /// <summary>
    /// Deactivates the role
    /// </summary>
    public void Deactivate()
    {
        if (!IsActive)
            return;

        IsActive = false;
        AddDomainEvent(new RoleDeactivatedEvent(Id, Name));
    }

    /// <summary>
    /// Adds a permission to the role
    /// </summary>
    /// <param name="permission">The permission to add</param>
    public void AddPermission(Permission permission)
    {
        if (permission == null)
            throw new ArgumentNullException(nameof(permission));

        if (_rolePermissions.Any(rp => rp.PermissionId == permission.Id))
            return; // Permission already assigned

        var rolePermission = new RolePermission(Id, permission.Id);
        _rolePermissions.Add(rolePermission);

        AddDomainEvent(new RolePermissionAssignedEvent(Id, permission.Id, Name, permission.Name));
    }

    /// <summary>
    /// Removes a permission from the role
    /// </summary>
    /// <param name="permissionId">The ID of the permission to remove</param>
    public void RemovePermission(Guid permissionId)
    {
        var rolePermission = _rolePermissions.FirstOrDefault(rp => rp.PermissionId == permissionId);
        if (rolePermission != null)
        {
            _rolePermissions.Remove(rolePermission);
            AddDomainEvent(new RolePermissionRemovedEvent(Id, permissionId, Name));
        }
    }

    /// <summary>
    /// Checks if the role has a specific permission
    /// </summary>
    /// <param name="permissionId">The permission ID to check</param>
    /// <returns>True if the role has the permission, false otherwise</returns>
    public bool HasPermission(Guid permissionId)
    {
        return _rolePermissions.Any(rp => rp.PermissionId == permissionId);
    }

    /// <summary>
    /// Gets all permission IDs for this role
    /// </summary>
    /// <returns>A collection of permission IDs</returns>
    public IEnumerable<Guid> GetPermissionIds()
    {
        return _rolePermissions.Select(rp => rp.PermissionId);
    }

    /// <summary>
    /// Gets all permissions for this role (requires permissions to be loaded)
    /// </summary>
    /// <returns>A collection of permissions</returns>
    public IEnumerable<Permission> GetPermissions()
    {
        return _rolePermissions.Select(rp => rp.Permission).Where(p => p != null);
    }

    /// <summary>
    /// Clears all permissions from the role
    /// </summary>
    public void ClearPermissions()
    {
        var permissionIds = _rolePermissions.Select(rp => rp.PermissionId).ToList();
        _rolePermissions.Clear();

        foreach (var permissionId in permissionIds)
        {
            AddDomainEvent(new RolePermissionRemovedEvent(Id, permissionId, Name));
        }
    }
}