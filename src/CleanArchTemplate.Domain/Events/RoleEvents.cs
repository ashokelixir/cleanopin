using CleanArchTemplate.Domain.Common;

namespace CleanArchTemplate.Domain.Events;

/// <summary>
/// Domain event raised when a role is created
/// </summary>
public sealed class RoleCreatedEvent : BaseDomainEvent
{
    public Guid RoleId { get; }
    public string Name { get; }
    public string Description { get; }

    public RoleCreatedEvent(Guid roleId, string name, string description)
    {
        RoleId = roleId;
        Name = name;
        Description = description;
    }
}

/// <summary>
/// Domain event raised when a role is updated
/// </summary>
public sealed class RoleUpdatedEvent : BaseDomainEvent
{
    public Guid RoleId { get; }
    public string OldName { get; }
    public string NewName { get; }
    public string OldDescription { get; }
    public string NewDescription { get; }

    public RoleUpdatedEvent(Guid roleId, string oldName, string newName, string oldDescription, string newDescription)
    {
        RoleId = roleId;
        OldName = oldName;
        NewName = newName;
        OldDescription = oldDescription;
        NewDescription = newDescription;
    }
}

/// <summary>
/// Domain event raised when a role is activated
/// </summary>
public sealed class RoleActivatedEvent : BaseDomainEvent
{
    public Guid RoleId { get; }
    public string Name { get; }

    public RoleActivatedEvent(Guid roleId, string name)
    {
        RoleId = roleId;
        Name = name;
    }
}

/// <summary>
/// Domain event raised when a role is deactivated
/// </summary>
public sealed class RoleDeactivatedEvent : BaseDomainEvent
{
    public Guid RoleId { get; }
    public string Name { get; }

    public RoleDeactivatedEvent(Guid roleId, string name)
    {
        RoleId = roleId;
        Name = name;
    }
}

/// <summary>
/// Domain event raised when a permission is assigned to a role
/// </summary>
public sealed class RolePermissionAssignedEvent : BaseDomainEvent
{
    public Guid RoleId { get; }
    public Guid PermissionId { get; }
    public string RoleName { get; }
    public string PermissionName { get; }

    public RolePermissionAssignedEvent(Guid roleId, Guid permissionId, string roleName, string permissionName)
    {
        RoleId = roleId;
        PermissionId = permissionId;
        RoleName = roleName;
        PermissionName = permissionName;
    }
}

/// <summary>
/// Domain event raised when a permission is removed from a role
/// </summary>
public sealed class RolePermissionRemovedEvent : BaseDomainEvent
{
    public Guid RoleId { get; }
    public Guid PermissionId { get; }
    public string RoleName { get; }

    public RolePermissionRemovedEvent(Guid roleId, Guid permissionId, string roleName)
    {
        RoleId = roleId;
        PermissionId = permissionId;
        RoleName = roleName;
    }
}