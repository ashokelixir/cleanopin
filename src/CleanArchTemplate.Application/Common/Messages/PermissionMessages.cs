using CleanArchTemplate.Application.Common.Models;

namespace CleanArchTemplate.Application.Common.Messages;

/// <summary>
/// Message published when a permission is assigned to a user
/// </summary>
public class PermissionAssignedMessage : BaseMessage
{
    /// <summary>
    /// The ID of the user who received the permission
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The ID of the permission that was assigned
    /// </summary>
    public Guid PermissionId { get; set; }

    /// <summary>
    /// The name of the permission
    /// </summary>
    public string PermissionName { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the user who assigned the permission
    /// </summary>
    public Guid AssignedByUserId { get; set; }

    /// <summary>
    /// The reason for the assignment
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Message published when a permission is removed from a user
/// </summary>
public class PermissionRemovedMessage : BaseMessage
{
    /// <summary>
    /// The ID of the user who lost the permission
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The ID of the permission that was removed
    /// </summary>
    public Guid PermissionId { get; set; }

    /// <summary>
    /// The name of the permission
    /// </summary>
    public string PermissionName { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the user who removed the permission
    /// </summary>
    public Guid RemovedByUserId { get; set; }

    /// <summary>
    /// The reason for the removal
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Message published when permissions are bulk assigned
/// </summary>
public class BulkPermissionsAssignedMessage : BaseMessage
{
    /// <summary>
    /// The ID of the user who received the permissions
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The IDs of the permissions that were assigned
    /// </summary>
    public List<Guid> PermissionIds { get; set; } = new();

    /// <summary>
    /// The names of the permissions
    /// </summary>
    public List<string> PermissionNames { get; set; } = new();

    /// <summary>
    /// The ID of the user who assigned the permissions
    /// </summary>
    public Guid AssignedByUserId { get; set; }

    /// <summary>
    /// The reason for the bulk assignment
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Message published when a permission is created
/// </summary>
public class PermissionCreatedMessage : BaseMessage
{
    /// <summary>
    /// The ID of the permission that was created
    /// </summary>
    public Guid PermissionId { get; set; }

    /// <summary>
    /// The resource the permission applies to
    /// </summary>
    public string Resource { get; set; } = string.Empty;

    /// <summary>
    /// The action the permission allows
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// The name of the permission
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The description of the permission
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The category of the permission
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// The parent permission ID if this is a child permission
    /// </summary>
    public Guid? ParentPermissionId { get; set; }
}

/// <summary>
/// Message published when a permission is updated
/// </summary>
public class PermissionUpdatedMessage : BaseMessage
{
    /// <summary>
    /// The ID of the permission that was updated
    /// </summary>
    public Guid PermissionId { get; set; }

    /// <summary>
    /// The old resource value
    /// </summary>
    public string OldResource { get; set; } = string.Empty;

    /// <summary>
    /// The new resource value
    /// </summary>
    public string NewResource { get; set; } = string.Empty;

    /// <summary>
    /// The old action value
    /// </summary>
    public string OldAction { get; set; } = string.Empty;

    /// <summary>
    /// The new action value
    /// </summary>
    public string NewAction { get; set; } = string.Empty;

    /// <summary>
    /// The old name value
    /// </summary>
    public string OldName { get; set; } = string.Empty;

    /// <summary>
    /// The new name value
    /// </summary>
    public string NewName { get; set; } = string.Empty;

    /// <summary>
    /// The old description value
    /// </summary>
    public string OldDescription { get; set; } = string.Empty;

    /// <summary>
    /// The new description value
    /// </summary>
    public string NewDescription { get; set; } = string.Empty;

    /// <summary>
    /// The old category value
    /// </summary>
    public string OldCategory { get; set; } = string.Empty;

    /// <summary>
    /// The new category value
    /// </summary>
    public string NewCategory { get; set; } = string.Empty;

    /// <summary>
    /// When the permission was updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Message published when a permission state changes (activated/deactivated)
/// </summary>
public class PermissionStateChangedMessage : BaseMessage
{
    /// <summary>
    /// The ID of the permission whose state changed
    /// </summary>
    public Guid PermissionId { get; set; }

    /// <summary>
    /// The name of the permission
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
/// Message published when a permission is assigned to a user
/// </summary>
public class UserPermissionAssignedMessage : BaseMessage
{
    /// <summary>
    /// The ID of the user who received the permission
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The ID of the permission that was assigned
    /// </summary>
    public Guid PermissionId { get; set; }

    /// <summary>
    /// The name of the permission
    /// </summary>
    public string PermissionName { get; set; } = string.Empty;

    /// <summary>
    /// The state of the permission (Allow/Deny)
    /// </summary>
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// The reason for the assignment
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// When the permission was assigned
    /// </summary>
    public DateTime AssignedAt { get; set; }
}

/// <summary>
/// Message published when a permission is removed from a user
/// </summary>
public class UserPermissionRemovedMessage : BaseMessage
{
    /// <summary>
    /// The ID of the user who lost the permission
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The ID of the permission that was removed
    /// </summary>
    public Guid PermissionId { get; set; }

    /// <summary>
    /// The name of the permission
    /// </summary>
    public string PermissionName { get; set; } = string.Empty;

    /// <summary>
    /// The state of the permission that was removed
    /// </summary>
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// The reason for the removal
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// When the permission was removed
    /// </summary>
    public DateTime RemovedAt { get; set; }
}

/// <summary>
/// Message published when a user permission is updated
/// </summary>
public class UserPermissionUpdatedMessage : BaseMessage
{
    /// <summary>
    /// The ID of the user whose permission was updated
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The ID of the permission that was updated
    /// </summary>
    public Guid PermissionId { get; set; }

    /// <summary>
    /// The old state of the permission
    /// </summary>
    public string OldState { get; set; } = string.Empty;

    /// <summary>
    /// The new state of the permission
    /// </summary>
    public string NewState { get; set; } = string.Empty;

    /// <summary>
    /// The old reason
    /// </summary>
    public string? OldReason { get; set; }

    /// <summary>
    /// The new reason
    /// </summary>
    public string? NewReason { get; set; }

    /// <summary>
    /// When the permission was updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}