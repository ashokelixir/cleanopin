using CleanArchTemplate.Application.Common.Models;
using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Shared.Models;

namespace CleanArchTemplate.Application.Common.Interfaces;

/// <summary>
/// Service interface for permission audit operations
/// </summary>
public interface IPermissionAuditService
{
    /// <summary>
    /// Logs a permission assignment to a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="permissionId">The permission ID</param>
    /// <param name="performedBy">Who performed the action</param>
    /// <param name="reason">Optional reason for the assignment</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created audit log entry</returns>
    Task<PermissionAuditLog> LogUserPermissionAssignedAsync(
        Guid userId, 
        Guid permissionId, 
        string performedBy, 
        string? reason = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs a permission removal from a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="permissionId">The permission ID</param>
    /// <param name="performedBy">Who performed the action</param>
    /// <param name="reason">Optional reason for the removal</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created audit log entry</returns>
    Task<PermissionAuditLog> LogUserPermissionRemovedAsync(
        Guid userId, 
        Guid permissionId, 
        string performedBy, 
        string? reason = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs a permission assignment to a role
    /// </summary>
    /// <param name="roleId">The role ID</param>
    /// <param name="permissionId">The permission ID</param>
    /// <param name="performedBy">Who performed the action</param>
    /// <param name="reason">Optional reason for the assignment</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created audit log entry</returns>
    Task<PermissionAuditLog> LogRolePermissionAssignedAsync(
        Guid roleId, 
        Guid permissionId, 
        string performedBy, 
        string? reason = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs a permission removal from a role
    /// </summary>
    /// <param name="roleId">The role ID</param>
    /// <param name="permissionId">The permission ID</param>
    /// <param name="performedBy">Who performed the action</param>
    /// <param name="reason">Optional reason for the removal</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created audit log entry</returns>
    Task<PermissionAuditLog> LogRolePermissionRemovedAsync(
        Guid roleId, 
        Guid permissionId, 
        string performedBy, 
        string? reason = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs a permission modification
    /// </summary>
    /// <param name="permissionId">The permission ID</param>
    /// <param name="oldValue">The old value</param>
    /// <param name="newValue">The new value</param>
    /// <param name="performedBy">Who performed the action</param>
    /// <param name="reason">Optional reason for the modification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created audit log entry</returns>
    Task<PermissionAuditLog> LogPermissionModifiedAsync(
        Guid permissionId, 
        string oldValue, 
        string newValue, 
        string performedBy, 
        string? reason = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs bulk permission assignments
    /// </summary>
    /// <param name="assignments">Collection of permission assignments</param>
    /// <param name="performedBy">Who performed the action</param>
    /// <param name="reason">Optional reason for the assignments</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of created audit log entries</returns>
    Task<IEnumerable<PermissionAuditLog>> LogBulkPermissionAssignmentsAsync(
        IEnumerable<BulkPermissionAssignmentDto> assignments, 
        string performedBy, 
        string? reason = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs with filtering and pagination
    /// </summary>
    /// <param name="filter">Audit log filter criteria</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated audit log results</returns>
    Task<PaginatedResult<PermissionAuditLogDto>> GetAuditLogsAsync(
        PermissionAuditLogFilterDto filter, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports audit logs to various formats
    /// </summary>
    /// <param name="filter">Audit log filter criteria</param>
    /// <param name="format">Export format (CSV, Excel, PDF)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Export result with file data</returns>
    Task<AuditLogExportResultDto> ExportAuditLogsAsync(
        PermissionAuditLogFilterDto filter, 
        AuditLogExportFormat format, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates compliance reports
    /// </summary>
    /// <param name="reportType">Type of compliance report</param>
    /// <param name="startDate">Report start date</param>
    /// <param name="endDate">Report end date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Compliance report data</returns>
    Task<ComplianceReportDto> GenerateComplianceReportAsync(
        ComplianceReportType reportType, 
        DateTime startDate, 
        DateTime endDate, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit statistics for a date range
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Audit statistics</returns>
    Task<AuditStatisticsDto> GetAuditStatisticsAsync(
        DateTime startDate, 
        DateTime endDate, 
        CancellationToken cancellationToken = default);
}