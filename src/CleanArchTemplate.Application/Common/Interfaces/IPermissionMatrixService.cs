using CleanArchTemplate.Application.Common.Models;
using CleanArchTemplate.Domain.Enums;

namespace CleanArchTemplate.Application.Common.Interfaces;

/// <summary>
/// Service interface for permission matrix operations
/// </summary>
public interface IPermissionMatrixService
{
    /// <summary>
    /// Gets the complete role-permission matrix
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Role-permission matrix data</returns>
    Task<PermissionMatrixDto> GetRolePermissionMatrixAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the role-permission matrix filtered by category
    /// </summary>
    /// <param name="category">The permission category to filter by</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Filtered role-permission matrix data</returns>
    Task<PermissionMatrixDto> GetRolePermissionMatrixByCategoryAsync(string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the user permission matrix showing effective permissions
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User permission matrix data</returns>
    Task<UserPermissionMatrixDto> GetUserPermissionMatrixAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the user permission matrix filtered by category
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="category">The permission category to filter by</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Filtered user permission matrix data</returns>
    Task<UserPermissionMatrixDto> GetUserPermissionMatrixByCategoryAsync(Guid userId, string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates role permissions in bulk
    /// </summary>
    /// <param name="roleId">The role ID</param>
    /// <param name="permissionIds">The permission IDs to assign to the role</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task UpdateRolePermissionMatrixAsync(Guid roleId, IEnumerable<Guid> permissionIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates multiple roles' permissions in bulk
    /// </summary>
    /// <param name="rolePermissionUpdates">Dictionary of role ID to permission IDs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task BulkUpdateRolePermissionMatrixAsync(Dictionary<Guid, IEnumerable<Guid>> rolePermissionUpdates, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates user permission overrides in bulk
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="permissionOverrides">Dictionary of permission ID to permission state</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task UpdateUserPermissionOverridesAsync(Guid userId, Dictionary<Guid, PermissionState> permissionOverrides, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets permission matrix comparison between two roles
    /// </summary>
    /// <param name="roleId1">First role ID</param>
    /// <param name="roleId2">Second role ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Permission matrix comparison data</returns>
    Task<PermissionMatrixComparisonDto> CompareRolePermissionsAsync(Guid roleId1, Guid roleId2, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets permission matrix comparison between a user's effective permissions and a role
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="roleId">The role ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Permission matrix comparison data</returns>
    Task<PermissionMatrixComparisonDto> CompareUserAndRolePermissionsAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets aggregated permission matrix statistics
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Permission matrix statistics</returns>
    Task<PermissionMatrixStatisticsDto> GetPermissionMatrixStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports role-permission matrix to a structured format
    /// </summary>
    /// <param name="format">Export format (CSV, Excel, JSON)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Exported matrix data</returns>
    Task<PermissionMatrixExportDto> ExportRolePermissionMatrixAsync(string format, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports role-permission matrix from structured data
    /// </summary>
    /// <param name="importData">The import data</param>
    /// <param name="overwriteExisting">Whether to overwrite existing assignments</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Import result with success/failure details</returns>
    Task<PermissionMatrixImportResultDto> ImportRolePermissionMatrixAsync(PermissionMatrixImportDto importData, bool overwriteExisting = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates permission matrix consistency and identifies issues
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation results with any identified issues</returns>
    Task<PermissionMatrixValidationResultDto> ValidatePermissionMatrixAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets permission matrix changes history
    /// </summary>
    /// <param name="fromDate">Start date for history</param>
    /// <param name="toDate">End date for history</param>
    /// <param name="roleId">Optional role ID filter</param>
    /// <param name="userId">Optional user ID filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Permission matrix change history</returns>
    Task<IEnumerable<PermissionMatrixChangeDto>> GetPermissionMatrixHistoryAsync(
        DateTime fromDate, 
        DateTime toDate, 
        Guid? roleId = null, 
        Guid? userId = null, 
        CancellationToken cancellationToken = default);
}