using CleanArchTemplate.Domain.Common;
using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.Enums;

namespace CleanArchTemplate.Domain.Events;

/// <summary>
/// Domain event raised when a permission is created
/// </summary>
public sealed class PermissionCreatedEvent : BaseDomainEvent
{
    public Guid PermissionId { get; }
    public string Resource { get; }
    public string Action { get; }
    public string Name { get; }
    public string Description { get; }
    public string Category { get; }
    public Guid? ParentPermissionId { get; }

    public PermissionCreatedEvent(Guid permissionId, string resource, string action, string name, 
        string description, string category, Guid? parentPermissionId)
    {
        PermissionId = permissionId;
        Resource = resource;
        Action = action;
        Name = name;
        Description = description;
        Category = category;
        ParentPermissionId = parentPermissionId;
    }
}

/// <summary>
/// Domain event raised when a permission is updated
/// </summary>
public sealed class PermissionUpdatedEvent : BaseDomainEvent
{
    public Guid PermissionId { get; }
    public string OldResource { get; }
    public string NewResource { get; }
    public string OldAction { get; }
    public string NewAction { get; }
    public string OldName { get; }
    public string NewName { get; }
    public string OldDescription { get; }
    public string NewDescription { get; }
    public string OldCategory { get; }
    public string NewCategory { get; }

    public PermissionUpdatedEvent(Guid permissionId, string oldResource, string newResource, 
        string oldAction, string newAction, string oldName, string newName,
        string oldDescription, string newDescription, string oldCategory, string newCategory)
    {
        PermissionId = permissionId;
        OldResource = oldResource;
        NewResource = newResource;
        OldAction = oldAction;
        NewAction = newAction;
        OldName = oldName;
        NewName = newName;
        OldDescription = oldDescription;
        NewDescription = newDescription;
        OldCategory = oldCategory;
        NewCategory = newCategory;
    }
}

/// <summary>
/// Domain event raised when a permission is activated
/// </summary>
public sealed class PermissionActivatedEvent : BaseDomainEvent
{
    public Guid PermissionId { get; }
    public string Name { get; }

    public PermissionActivatedEvent(Guid permissionId, string name)
    {
        PermissionId = permissionId;
        Name = name;
    }
}

/// <summary>
/// Domain event raised when a permission is deactivated
/// </summary>
public sealed class PermissionDeactivatedEvent : BaseDomainEvent
{
    public Guid PermissionId { get; }
    public string Name { get; }

    public PermissionDeactivatedEvent(Guid permissionId, string name)
    {
        PermissionId = permissionId;
        Name = name;
    }
}

/// <summary>
/// Domain event raised when a permission hierarchy changes
/// </summary>
public sealed class PermissionHierarchyChangedEvent : BaseDomainEvent
{
    public Guid PermissionId { get; }
    public Guid? OldParentId { get; }
    public Guid? NewParentId { get; }

    public PermissionHierarchyChangedEvent(Guid permissionId, Guid? oldParentId, Guid? newParentId)
    {
        PermissionId = permissionId;
        OldParentId = oldParentId;
        NewParentId = newParentId;
    }
}

/// <summary>
/// Domain event raised when a user permission is assigned (from entity)
/// </summary>
public sealed class UserPermissionCreatedEvent : BaseDomainEvent
{
    public Guid UserPermissionId { get; }
    public Guid UserId { get; }
    public Guid PermissionId { get; }
    public PermissionState State { get; }
    public string? Reason { get; }
    public DateTime? ExpiresAt { get; }

    public UserPermissionCreatedEvent(Guid userPermissionId, Guid userId, Guid permissionId, 
        PermissionState state, string? reason, DateTime? expiresAt)
    {
        UserPermissionId = userPermissionId;
        UserId = userId;
        PermissionId = permissionId;
        State = state;
        Reason = reason;
        ExpiresAt = expiresAt;
    }
}

/// <summary>
/// Domain event raised when a user permission is updated
/// </summary>
public sealed class UserPermissionUpdatedEvent : BaseDomainEvent
{
    public Guid UserPermissionId { get; }
    public Guid UserId { get; }
    public Guid PermissionId { get; }
    public PermissionState OldState { get; }
    public PermissionState NewState { get; }
    public string? OldReason { get; }
    public string? NewReason { get; }

    public UserPermissionUpdatedEvent(Guid userPermissionId, Guid userId, Guid permissionId, 
        PermissionState oldState, PermissionState newState, string? oldReason, string? newReason)
    {
        UserPermissionId = userPermissionId;
        UserId = userId;
        PermissionId = permissionId;
        OldState = oldState;
        NewState = newState;
        OldReason = oldReason;
        NewReason = newReason;
    }
}

/// <summary>
/// Domain event raised when a user permission expiration changes
/// </summary>
public sealed class UserPermissionExpirationChangedEvent : BaseDomainEvent
{
    public Guid UserPermissionId { get; }
    public Guid UserId { get; }
    public Guid PermissionId { get; }
    public DateTime? OldExpiresAt { get; }
    public DateTime? NewExpiresAt { get; }

    public UserPermissionExpirationChangedEvent(Guid userPermissionId, Guid userId, Guid permissionId, 
        DateTime? oldExpiresAt, DateTime? newExpiresAt)
    {
        UserPermissionId = userPermissionId;
        UserId = userId;
        PermissionId = permissionId;
        OldExpiresAt = oldExpiresAt;
        NewExpiresAt = newExpiresAt;
    }
}

/// <summary>
/// Domain event raised when a user permission is removed
/// </summary>
public sealed class UserPermissionRemovedEvent : BaseDomainEvent
{
    public Guid UserId { get; }
    public Guid PermissionId { get; }
    public string PermissionName { get; }
    public PermissionState State { get; }
    public string? Reason { get; }

    public UserPermissionRemovedEvent(Guid userId, Guid permissionId, string permissionName,
        PermissionState state, string? reason)
    {
        UserId = userId;
        PermissionId = permissionId;
        PermissionName = permissionName;
        State = state;
        Reason = reason;
    }
}

/// <summary>
/// Domain event raised when a permission is assigned to a user (for handlers)
/// </summary>
public sealed class UserPermissionAssignedEvent : BaseDomainEvent
{
    public Guid UserId { get; }
    public Guid PermissionId { get; }
    public string PermissionName { get; }
    public PermissionState State { get; }
    public string? Reason { get; }

    public UserPermissionAssignedEvent(Guid userId, Guid permissionId, string permissionName,
        PermissionState state, string? reason)
    {
        UserId = userId;
        PermissionId = permissionId;
        PermissionName = permissionName;
        State = state;
        Reason = reason;
    }
}

