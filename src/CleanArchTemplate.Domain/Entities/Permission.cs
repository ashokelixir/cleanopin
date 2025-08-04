using CleanArchTemplate.Domain.Common;
using CleanArchTemplate.Domain.Events;

namespace CleanArchTemplate.Domain.Entities;

/// <summary>
/// Represents a permission in the system
/// </summary>
public class Permission : BaseAuditableEntity
{
    private readonly List<RolePermission> _rolePermissions = new();

    /// <summary>
    /// The permission name (unique identifier)
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// The permission description
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// The permission category/module
    /// </summary>
    public string Category { get; private set; } = string.Empty;

    /// <summary>
    /// Indicates whether the permission is active
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Navigation property for role permissions
    /// </summary>
    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();

    // Private constructor for EF Core
    private Permission() { }

    /// <summary>
    /// Creates a new permission
    /// </summary>
    /// <param name="name">The permission name</param>
    /// <param name="description">The permission description</param>
    /// <param name="category">The permission category</param>
    public Permission(string name, string description, string category)
    {
        Name = name?.Trim() ?? throw new ArgumentNullException(nameof(name));
        Description = description?.Trim() ?? throw new ArgumentNullException(nameof(description));
        Category = category?.Trim() ?? throw new ArgumentNullException(nameof(category));

        if (string.IsNullOrWhiteSpace(Name))
            throw new ArgumentException("Permission name cannot be empty.", nameof(name));

        if (string.IsNullOrWhiteSpace(Category))
            throw new ArgumentException("Permission category cannot be empty.", nameof(category));

        if (Name.Length > 100)
            throw new ArgumentException("Permission name cannot exceed 100 characters.", nameof(name));

        if (Description.Length > 500)
            throw new ArgumentException("Permission description cannot exceed 500 characters.", nameof(description));

        if (Category.Length > 50)
            throw new ArgumentException("Permission category cannot exceed 50 characters.", nameof(category));

        AddDomainEvent(new PermissionCreatedEvent(Id, Name, Description, Category));
    }

    /// <summary>
    /// Updates the permission information
    /// </summary>
    /// <param name="name">The new permission name</param>
    /// <param name="description">The new permission description</param>
    /// <param name="category">The new permission category</param>
    public void Update(string name, string description, string category)
    {
        var oldName = Name;
        var oldDescription = Description;
        var oldCategory = Category;

        Name = name?.Trim() ?? throw new ArgumentNullException(nameof(name));
        Description = description?.Trim() ?? throw new ArgumentNullException(nameof(description));
        Category = category?.Trim() ?? throw new ArgumentNullException(nameof(category));

        if (string.IsNullOrWhiteSpace(Name))
            throw new ArgumentException("Permission name cannot be empty.", nameof(name));

        if (string.IsNullOrWhiteSpace(Category))
            throw new ArgumentException("Permission category cannot be empty.", nameof(category));

        if (Name.Length > 100)
            throw new ArgumentException("Permission name cannot exceed 100 characters.", nameof(name));

        if (Description.Length > 500)
            throw new ArgumentException("Permission description cannot exceed 500 characters.", nameof(description));

        if (Category.Length > 50)
            throw new ArgumentException("Permission category cannot exceed 50 characters.", nameof(category));

        AddDomainEvent(new PermissionUpdatedEvent(Id, oldName, Name, oldDescription, Description, oldCategory, Category));
    }

    /// <summary>
    /// Activates the permission
    /// </summary>
    public void Activate()
    {
        if (IsActive)
            return;

        IsActive = true;
        AddDomainEvent(new PermissionActivatedEvent(Id, Name));
    }

    /// <summary>
    /// Deactivates the permission
    /// </summary>
    public void Deactivate()
    {
        if (!IsActive)
            return;

        IsActive = false;
        AddDomainEvent(new PermissionDeactivatedEvent(Id, Name));
    }
}