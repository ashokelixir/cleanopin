using CleanArchTemplate.Domain.Common;

namespace CleanArchTemplate.Domain.Events;

/// <summary>
/// Domain event raised when a permission is created
/// </summary>
public sealed class PermissionCreatedEvent : BaseDomainEvent
{
    public Guid PermissionId { get; }
    public string Name { get; }
    public string Description { get; }
    public string Category { get; }

    public PermissionCreatedEvent(Guid permissionId, string name, string description, string category)
    {
        PermissionId = permissionId;
        Name = name;
        Description = description;
        Category = category;
    }
}

/// <summary>
/// Domain event raised when a permission is updated
/// </summary>
public sealed class PermissionUpdatedEvent : BaseDomainEvent
{
    public Guid PermissionId { get; }
    public string OldName { get; }
    public string NewName { get; }
    public string OldDescription { get; }
    public string NewDescription { get; }
    public string OldCategory { get; }
    public string NewCategory { get; }

    public PermissionUpdatedEvent(Guid permissionId, string oldName, string newName, 
        string oldDescription, string newDescription, string oldCategory, string newCategory)
    {
        PermissionId = permissionId;
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