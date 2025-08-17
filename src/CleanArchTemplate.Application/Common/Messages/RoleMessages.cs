using CleanArchTemplate.Application.Common.Models;

namespace CleanArchTemplate.Application.Common.Messages;

/// <summary>
/// Message published when a role is created
/// </summary>
public class RoleCreatedMessage : BaseMessage
{
    /// <summary>
    /// The ID of the role that was created
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// The name of the role
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The description of the role
    /// </summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Message published when a role is updated
/// </summary>
public class RoleUpdatedMessage : BaseMessage
{
    /// <summary>
    /// The ID of the role that was updated
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// The old name of the role
    /// </summary>
    public string OldName { get; set; } = string.Empty;

    /// <summary>
    /// The new name of the role
    /// </summary>
    public string NewName { get; set; } = string.Empty;

    /// <summary>
    /// The old description of the role
    /// </summary>
    public string OldDescription { get; set; } = string.Empty;

    /// <summary>
    /// The new description of the role
    /// </summary>
    public string NewDescription { get; set; } = string.Empty;

    /// <summary>
    /// When the role was updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Message published when a role state changes (activated/deactivated)
/// </summary>
public class RoleStateChangedMessage : BaseMessage
{
    /// <summary>
    /// The ID of the role whose state changed
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// The name of the role
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The old state
    /// </summary>
    public string OldState { get; set; } = string.Empty;

    /// <summary>
    /// The new state
    /// </summary>
    public string NewState { get; set; } = string.Empty;

    /// <summary>
    /// When the state changed
    /// </summary>
    public DateTime ChangedAt { get; set; }
}

/// <summary>
/// Message published when a permission is assigned to a role
/// </summary>
public class RolePermissionAssignedMessage : BaseMessage
{
    /// <summary>
    /// The ID of the role
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// The name of the role
    /// </summary>
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the permission
    /// </summary>
    public Guid PermissionId { get; set; }

    /// <summary>
    /// The name of the permission
    /// </summary>
    public string PermissionName { get; set; } = string.Empty;

    /// <summary>
    /// When the permission was assigned
    /// </summary>
    public DateTime AssignedAt { get; set; }
}

/// <summary>
/// Message published when a permission is removed from a role
/// </summary>
public class RolePermissionRemovedMessage : BaseMessage
{
    /// <summary>
    /// The ID of the role
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// The name of the role
    /// </summary>
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the permission
    /// </summary>
    public Guid PermissionId { get; set; }

    /// <summary>
    /// The name of the permission
    /// </summary>
    public string PermissionName { get; set; } = string.Empty;

    /// <summary>
    /// When the permission was removed
    /// </summary>
    public DateTime RemovedAt { get; set; }
}

/// <summary>
/// Message published when a role is assigned to a user
/// </summary>
public class UserRoleAssignedMessage : BaseMessage
{
    /// <summary>
    /// The ID of the user
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The ID of the role
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// The name of the role
    /// </summary>
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// When the role was assigned
    /// </summary>
    public DateTime AssignedAt { get; set; }
}

/// <summary>
/// Message published when a role is removed from a user
/// </summary>
public class UserRoleRemovedMessage : BaseMessage
{
    /// <summary>
    /// The ID of the user
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The ID of the role
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// When the role was removed
    /// </summary>
    public DateTime RemovedAt { get; set; }
}