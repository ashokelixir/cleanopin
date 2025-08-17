using CleanArchTemplate.Domain.Common;
using CleanArchTemplate.Domain.Events;

namespace CleanArchTemplate.Domain.Entities;

/// <summary>
/// Represents a permission in the system with resource-action model and hierarchical support
/// </summary>
public class Permission : BaseAuditableEntity
{
    private readonly List<RolePermission> _rolePermissions = new();
    private readonly List<UserPermission> _userPermissions = new();
    private readonly List<Permission> _childPermissions = new();

    /// <summary>
    /// The resource this permission applies to (e.g., "Users", "Reports", "Settings")
    /// </summary>
    public string Resource { get; private set; } = string.Empty;

    /// <summary>
    /// The action this permission allows (e.g., "Create", "Read", "Update", "Delete")
    /// </summary>
    public string Action { get; private set; } = string.Empty;

    /// <summary>
    /// The computed permission name in format "{Resource}.{Action}"
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
    /// The parent permission ID for hierarchical permissions
    /// </summary>
    public Guid? ParentPermissionId { get; private set; }

    /// <summary>
    /// Navigation property to the parent permission
    /// </summary>
    public Permission? ParentPermission { get; private set; }

    /// <summary>
    /// Navigation property for child permissions
    /// </summary>
    public IReadOnlyCollection<Permission> ChildPermissions => _childPermissions.AsReadOnly();

    /// <summary>
    /// Navigation property for role permissions
    /// </summary>
    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();

    /// <summary>
    /// Navigation property for user permissions
    /// </summary>
    public IReadOnlyCollection<UserPermission> UserPermissions => _userPermissions.AsReadOnly();

    // Private constructor for EF Core
    private Permission() { }

    /// <summary>
    /// Creates a new permission with resource-action model
    /// </summary>
    /// <param name="resource">The resource this permission applies to</param>
    /// <param name="action">The action this permission allows</param>
    /// <param name="description">The permission description</param>
    /// <param name="category">The permission category</param>
    /// <param name="parentPermissionId">Optional parent permission ID for hierarchical permissions</param>
    public Permission(string resource, string action, string description, string category, Guid? parentPermissionId = null)
    {
        Resource = resource?.Trim() ?? throw new ArgumentNullException(nameof(resource));
        Action = action?.Trim() ?? throw new ArgumentNullException(nameof(action));
        Description = description?.Trim() ?? throw new ArgumentNullException(nameof(description));
        Category = category?.Trim() ?? throw new ArgumentNullException(nameof(category));
        ParentPermissionId = parentPermissionId;

        ValidateInputs(Resource, Action, Description, Category);

        Name = $"{Resource}.{Action}";

        AddDomainEvent(new PermissionCreatedEvent(Id, Resource, Action, Name, Description, Category, ParentPermissionId));
    }

    /// <summary>
    /// Creates a new permission with resource-action model
    /// </summary>
    /// <param name="resource">The resource this permission applies to</param>
    /// <param name="action">The action this permission allows</param>
    /// <param name="description">The permission description</param>
    /// <param name="category">The permission category</param>
    /// <param name="parentPermissionId">Optional parent permission ID for hierarchical permissions</param>
    /// <returns>A new Permission instance</returns>
    public static Permission Create(string resource, string action, string description, string category, Guid? parentPermissionId = null)
    {
        return new Permission(resource, action, description, category, parentPermissionId);
    }

    /// <summary>
    /// Creates a new permission (legacy constructor for backward compatibility)
    /// </summary>
    /// <param name="name">The permission name</param>
    /// <param name="description">The permission description</param>
    /// <param name="category">The permission category</param>
    [Obsolete("Use the resource-action constructor instead")]
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

        // Parse resource and action from name if possible
        var parts = Name.Split('.');
        if (parts.Length == 2)
        {
            Resource = parts[0];
            Action = parts[1];
        }
        else
        {
            Resource = "Legacy";
            Action = Name;
        }

        AddDomainEvent(new PermissionCreatedEvent(Id, Resource, Action, Name, Description, Category, null));
    }

    /// <summary>
    /// Updates the permission information with resource-action model
    /// </summary>
    /// <param name="resource">The new resource</param>
    /// <param name="action">The new action</param>
    /// <param name="description">The new permission description</param>
    /// <param name="category">The new permission category</param>
    public void Update(string resource, string action, string description, string category)
    {
        var oldResource = Resource;
        var oldAction = Action;
        var oldName = Name;
        var oldDescription = Description;
        var oldCategory = Category;

        Resource = resource?.Trim() ?? throw new ArgumentNullException(nameof(resource));
        Action = action?.Trim() ?? throw new ArgumentNullException(nameof(action));
        Description = description?.Trim() ?? throw new ArgumentNullException(nameof(description));
        Category = category?.Trim() ?? throw new ArgumentNullException(nameof(category));

        ValidateInputs(Resource, Action, Description, Category);

        Name = $"{Resource}.{Action}";

        AddDomainEvent(new PermissionUpdatedEvent(Id, oldResource, Resource, oldAction, Action, 
            oldName, Name, oldDescription, Description, oldCategory, Category));
    }

    /// <summary>
    /// Updates the permission information with resource-action model and status
    /// </summary>
    /// <param name="description">The new permission description</param>
    /// <param name="category">The new permission category</param>
    /// <param name="parentPermissionId">The new parent permission ID</param>
    /// <param name="isActive">The new active status</param>
    public void Update(string description, string category, Guid? parentPermissionId, bool isActive)
    {
        var oldDescription = Description;
        var oldCategory = Category;
        var oldParentPermissionId = ParentPermissionId;
        var oldIsActive = IsActive;

        Description = description?.Trim() ?? throw new ArgumentNullException(nameof(description));
        Category = category?.Trim() ?? throw new ArgumentNullException(nameof(category));
        ParentPermissionId = parentPermissionId;
        IsActive = isActive;

        if (string.IsNullOrWhiteSpace(Category))
            throw new ArgumentException("Permission category cannot be empty.", nameof(category));

        if (Description.Length > 500)
            throw new ArgumentException("Permission description cannot exceed 500 characters.", nameof(description));

        if (Category.Length > 50)
            throw new ArgumentException("Permission category cannot exceed 50 characters.", nameof(category));

        AddDomainEvent(new PermissionUpdatedEvent(Id, Resource, Resource, Action, Action, 
            Name, Name, oldDescription, Description, oldCategory, Category));

        if (oldParentPermissionId != ParentPermissionId)
        {
            AddDomainEvent(new PermissionHierarchyChangedEvent(Id, oldParentPermissionId, ParentPermissionId));
        }

        if (oldIsActive != IsActive)
        {
            if (IsActive)
                AddDomainEvent(new PermissionActivatedEvent(Id, Name));
            else
                AddDomainEvent(new PermissionDeactivatedEvent(Id, Name));
        }
    }

    /// <summary>
    /// Updates the permission information (legacy method for backward compatibility)
    /// </summary>
    /// <param name="name">The new permission name</param>
    /// <param name="description">The new permission description</param>
    /// <param name="category">The new permission category</param>
    [Obsolete("Use the resource-action Update method instead")]
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

        // Parse resource and action from name if possible
        var parts = Name.Split('.');
        if (parts.Length == 2)
        {
            Resource = parts[0];
            Action = parts[1];
        }

        AddDomainEvent(new PermissionUpdatedEvent(Id, Resource, Resource, Action, Action, 
            oldName, Name, oldDescription, Description, oldCategory, Category));
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

    /// <summary>
    /// Sets the parent permission for hierarchical relationships
    /// </summary>
    /// <param name="parentPermissionId">The parent permission ID</param>
    public void SetParent(Guid? parentPermissionId)
    {
        if (parentPermissionId == Id)
            throw new ArgumentException("Permission cannot be its own parent.", nameof(parentPermissionId));

        var oldParentId = ParentPermissionId;
        ParentPermissionId = parentPermissionId;

        AddDomainEvent(new PermissionHierarchyChangedEvent(Id, oldParentId, ParentPermissionId));
    }

    /// <summary>
    /// Adds a child permission to this permission
    /// </summary>
    /// <param name="childPermission">The child permission to add</param>
    public void AddChild(Permission childPermission)
    {
        if (childPermission == null)
            throw new ArgumentNullException(nameof(childPermission));

        if (childPermission.Id == Id)
            throw new ArgumentException("Permission cannot be its own child.", nameof(childPermission));

        if (_childPermissions.Any(c => c.Id == childPermission.Id))
            return; // Already exists

        _childPermissions.Add(childPermission);
        childPermission.SetParent(Id);
    }

    /// <summary>
    /// Removes a child permission from this permission
    /// </summary>
    /// <param name="childPermission">The child permission to remove</param>
    public void RemoveChild(Permission childPermission)
    {
        if (childPermission == null)
            throw new ArgumentNullException(nameof(childPermission));

        if (_childPermissions.Remove(childPermission))
        {
            childPermission.SetParent(null);
        }
    }

    /// <summary>
    /// Validates if this permission can have the specified parent without creating circular references
    /// </summary>
    /// <param name="potentialParentId">The potential parent permission ID</param>
    /// <returns>True if the parent can be set without circular references</returns>
    public bool CanHaveParent(Guid potentialParentId)
    {
        if (potentialParentId == Id)
            return false;

        // Check if the potential parent is already a descendant
        return !IsDescendant(potentialParentId);
    }

    /// <summary>
    /// Checks if the specified permission ID is a descendant of this permission
    /// </summary>
    /// <param name="permissionId">The permission ID to check</param>
    /// <returns>True if the permission is a descendant</returns>
    private bool IsDescendant(Guid permissionId)
    {
        return _childPermissions.Any(child => 
            child.Id == permissionId || child.IsDescendant(permissionId));
    }

    /// <summary>
    /// Validates the input parameters for permission creation/update
    /// </summary>
    /// <param name="resource">The resource</param>
    /// <param name="action">The action</param>
    /// <param name="description">The description</param>
    /// <param name="category">The category</param>
    private static void ValidateInputs(string resource, string action, string description, string category)
    {
        if (string.IsNullOrWhiteSpace(resource))
            throw new ArgumentException("Permission resource cannot be empty.", nameof(resource));

        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Permission action cannot be empty.", nameof(action));

        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Permission category cannot be empty.", nameof(category));

        if (resource.Length > 100)
            throw new ArgumentException("Permission resource cannot exceed 100 characters.", nameof(resource));

        if (action.Length > 100)
            throw new ArgumentException("Permission action cannot exceed 100 characters.", nameof(action));

        if (description.Length > 500)
            throw new ArgumentException("Permission description cannot exceed 500 characters.", nameof(description));

        if (category.Length > 50)
            throw new ArgumentException("Permission category cannot exceed 50 characters.", nameof(category));

        // Validate resource and action format (alphanumeric, underscore, hyphen)
        if (!IsValidIdentifier(resource))
            throw new ArgumentException("Permission resource must contain only alphanumeric characters, underscores, and hyphens.", nameof(resource));

        if (!IsValidIdentifier(action))
            throw new ArgumentException("Permission action must contain only alphanumeric characters, underscores, and hyphens.", nameof(action));
    }

    /// <summary>
    /// Validates if a string is a valid identifier (alphanumeric, underscore, hyphen)
    /// </summary>
    /// <param name="value">The value to validate</param>
    /// <returns>True if valid</returns>
    private static bool IsValidIdentifier(string value)
    {
        return !string.IsNullOrWhiteSpace(value) && 
               value.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-');
    }
}