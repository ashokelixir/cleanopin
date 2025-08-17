using CleanArchTemplate.Domain.Entities;

namespace CleanArchTemplate.Domain.Interfaces;

/// <summary>
/// Domain repository interface for Permission entity extending base repository
/// </summary>
public interface IPermissionRepository : IRepository<Permission>
{
    /// <summary>
    /// Gets a permission by name
    /// </summary>
    /// <param name="name">The permission name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The permission if found, null otherwise</returns>
    Task<Permission?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a permission by resource and action
    /// </summary>
    /// <param name="resource">The resource name</param>
    /// <param name="action">The action name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The permission if found, null otherwise</returns>
    Task<Permission?> GetByResourceAndActionAsync(string resource, string action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets permissions by resource
    /// </summary>
    /// <param name="resource">The resource name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Permissions for the specified resource</returns>
    Task<IEnumerable<Permission>> GetByResourceAsync(string resource, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets permissions by multiple resources
    /// </summary>
    /// <param name="resources">The resource names</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Permissions for the specified resources</returns>
    Task<IEnumerable<Permission>> GetByResourcesAsync(IEnumerable<string> resources, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets permissions by action
    /// </summary>
    /// <param name="action">The action name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Permissions for the specified action</returns>
    Task<IEnumerable<Permission>> GetByActionAsync(string action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets permissions by multiple actions
    /// </summary>
    /// <param name="actions">The action names</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Permissions for the specified actions</returns>
    Task<IEnumerable<Permission>> GetByActionsAsync(IEnumerable<string> actions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets permissions by resource and multiple actions
    /// </summary>
    /// <param name="resource">The resource name</param>
    /// <param name="actions">The action names</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Permissions for the specified resource and actions</returns>
    Task<IEnumerable<Permission>> GetByResourceAndActionsAsync(string resource, IEnumerable<string> actions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets permissions by category
    /// </summary>
    /// <param name="category">The permission category</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Permissions in the specified category</returns>
    Task<IEnumerable<Permission>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets hierarchical permissions (parent and all children) for a permission
    /// </summary>
    /// <param name="permissionId">The permission ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The permission with its hierarchy</returns>
    Task<IEnumerable<Permission>> GetHierarchicalPermissionsAsync(Guid permissionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets permissions with their parent-child relationships
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>All permissions with hierarchy information</returns>
    Task<IEnumerable<Permission>> GetPermissionsWithHierarchyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all permission categories
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of unique permission categories</returns>
    Task<IEnumerable<string>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all unique resources
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of unique resources</returns>
    Task<IEnumerable<string>> GetResourcesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all unique actions
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of unique actions</returns>
    Task<IEnumerable<string>> GetActionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets actions for a specific resource
    /// </summary>
    /// <param name="resource">The resource name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Actions available for the specified resource</returns>
    Task<IEnumerable<string>> GetActionsByResourceAsync(string resource, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a permission name is already in use
    /// </summary>
    /// <param name="name">The permission name to check</param>
    /// <param name="excludePermissionId">Permission ID to exclude from the check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if permission name exists, false otherwise</returns>
    Task<bool> IsNameExistsAsync(string name, Guid? excludePermissionId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a permission exists by resource and action
    /// </summary>
    /// <param name="resource">The resource name</param>
    /// <param name="action">The action name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if permission exists, false otherwise</returns>
    Task<bool> ExistsAsync(string resource, string action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets permissions with pagination and filtering
    /// </summary>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="searchTerm">Optional search term for filtering</param>
    /// <param name="resource">Optional resource filter</param>
    /// <param name="action">Optional action filter</param>
    /// <param name="category">Optional category filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated permissions</returns>
    Task<(IEnumerable<Permission> Items, int TotalCount)> GetPagedAsync(
        int pageNumber, 
        int pageSize, 
        string? searchTerm = null, 
        string? resource = null, 
        string? action = null, 
        string? category = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk adds permissions
    /// </summary>
    /// <param name="permissions">The permissions to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task BulkAddAsync(IEnumerable<Permission> permissions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk updates permissions
    /// </summary>
    /// <param name="permissions">The permissions to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task BulkUpdateAsync(IEnumerable<Permission> permissions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk activates permissions
    /// </summary>
    /// <param name="permissionIds">The permission IDs to activate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task BulkActivateAsync(IEnumerable<Guid> permissionIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk deactivates permissions
    /// </summary>
    /// <param name="permissionIds">The permission IDs to deactivate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task BulkDeactivateAsync(IEnumerable<Guid> permissionIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets permissions by multiple IDs
    /// </summary>
    /// <param name="permissionIds">The permission IDs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Permissions with the specified IDs</returns>
    Task<IEnumerable<Permission>> GetByIdsAsync(IEnumerable<Guid> permissionIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active permissions by multiple resources
    /// </summary>
    /// <param name="resources">The resource names</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Active permissions for the specified resources</returns>
    Task<IEnumerable<Permission>> GetActiveByResourcesAsync(IEnumerable<string> resources, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active permissions by multiple actions
    /// </summary>
    /// <param name="actions">The action names</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Active permissions for the specified actions</returns>
    Task<IEnumerable<Permission>> GetActiveByActionsAsync(IEnumerable<string> actions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts permissions by resource
    /// </summary>
    /// <param name="resource">The resource name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Count of active permissions for the resource</returns>
    Task<int> CountByResourceAsync(string resource, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts permissions by action
    /// </summary>
    /// <param name="action">The action name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Count of active permissions for the action</returns>
    Task<int> CountByActionAsync(string action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts permissions by category
    /// </summary>
    /// <param name="category">The category name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Count of active permissions for the category</returns>
    Task<int> CountByCategoryAsync(string category, CancellationToken cancellationToken = default);
}