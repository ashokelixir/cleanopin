namespace CleanArchTemplate.Application.Common.Models;

/// <summary>
/// Data transfer object for permission audit log
/// </summary>
public class PermissionAuditLogDto
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
    public Guid? RoleId { get; set; }
    public string? RoleName { get; set; }
    public Guid PermissionId { get; set; }
    public string PermissionName { get; set; } = string.Empty;
    public string PermissionResource { get; set; } = string.Empty;
    public string PermissionAction { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? Reason { get; set; }
    public string PerformedBy { get; set; } = string.Empty;
    public DateTime PerformedAt { get; set; }
}

/// <summary>
/// Filter criteria for permission audit logs
/// </summary>
public class PermissionAuditLogFilterDto
{
    public Guid? UserId { get; set; }
    public Guid? RoleId { get; set; }
    public Guid? PermissionId { get; set; }
    public string? Action { get; set; }
    public string? PerformedBy { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Resource { get; set; }
    public string? PermissionAction { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; } = "PerformedAt";
    public bool SortDescending { get; set; } = true;
}

/// <summary>
/// Bulk permission assignment data
/// </summary>
public class BulkPermissionAssignmentDto
{
    public Guid? UserId { get; set; }
    public Guid? RoleId { get; set; }
    public Guid PermissionId { get; set; }
    public string Action { get; set; } = string.Empty; // Assigned or Removed
}

/// <summary>
/// Audit log export result
/// </summary>
public class AuditLogExportResultDto
{
    public byte[] FileData { get; set; } = Array.Empty<byte>();
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public int RecordCount { get; set; }
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Compliance report data
/// </summary>
public class ComplianceReportDto
{
    public ComplianceReportType ReportType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime GeneratedAt { get; set; }
    public int TotalPermissionChanges { get; set; }
    public int UserPermissionChanges { get; set; }
    public int RolePermissionChanges { get; set; }
    public int PermissionModifications { get; set; }
    public IEnumerable<ComplianceViolationDto> Violations { get; set; } = new List<ComplianceViolationDto>();
    public IEnumerable<HighRiskActivityDto> HighRiskActivities { get; set; } = new List<HighRiskActivityDto>();
    public IEnumerable<UserAccessSummaryDto> UserAccessSummaries { get; set; } = new List<UserAccessSummaryDto>();
}

/// <summary>
/// Compliance violation data
/// </summary>
public class ComplianceViolationDto
{
    public string ViolationType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public Guid? RoleId { get; set; }
    public string? RoleName { get; set; }
    public DateTime DetectedAt { get; set; }
    public string Severity { get; set; } = string.Empty;
}

/// <summary>
/// High risk activity data
/// </summary>
public class HighRiskActivityDto
{
    public string ActivityType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PerformedBy { get; set; } = string.Empty;
    public DateTime PerformedAt { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public IEnumerable<string> AffectedResources { get; set; } = new List<string>();
}

/// <summary>
/// User access summary for compliance reporting
/// </summary>
public class UserAccessSummaryDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int TotalPermissions { get; set; }
    public int DirectPermissions { get; set; }
    public int RoleBasedPermissions { get; set; }
    public DateTime LastPermissionChange { get; set; }
    public IEnumerable<string> Roles { get; set; } = new List<string>();
    public IEnumerable<string> HighRiskPermissions { get; set; } = new List<string>();
}

/// <summary>
/// Audit statistics data
/// </summary>
public class AuditStatisticsDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalAuditEntries { get; set; }
    public int UserPermissionAssignments { get; set; }
    public int UserPermissionRemovals { get; set; }
    public int RolePermissionAssignments { get; set; }
    public int RolePermissionRemovals { get; set; }
    public int PermissionModifications { get; set; }
    public IEnumerable<AuditActionStatisticDto> ActionStatistics { get; set; } = new List<AuditActionStatisticDto>();
    public IEnumerable<AuditPerformerStatisticDto> PerformerStatistics { get; set; } = new List<AuditPerformerStatisticDto>();
    public IEnumerable<AuditResourceStatisticDto> ResourceStatistics { get; set; } = new List<AuditResourceStatisticDto>();
}

/// <summary>
/// Audit action statistics
/// </summary>
public class AuditActionStatisticDto
{
    public string Action { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

/// <summary>
/// Audit performer statistics
/// </summary>
public class AuditPerformerStatisticDto
{
    public string PerformedBy { get; set; } = string.Empty;
    public int Count { get; set; }
    public DateTime LastActivity { get; set; }
}

/// <summary>
/// Audit resource statistics
/// </summary>
public class AuditResourceStatisticDto
{
    public string Resource { get; set; } = string.Empty;
    public int Count { get; set; }
    public IEnumerable<string> MostCommonActions { get; set; } = new List<string>();
}

/// <summary>
/// Export format enumeration
/// </summary>
public enum AuditLogExportFormat
{
    Csv = 1,
    Excel = 2,
    Pdf = 3,
    Json = 4
}

/// <summary>
/// Compliance report type enumeration
/// </summary>
public enum ComplianceReportType
{
    AccessReview = 1,
    PermissionChanges = 2,
    HighRiskActivities = 3,
    ComplianceViolations = 4,
    UserAccessSummary = 5
}