using CleanArchTemplate.API.Attributes;
using CleanArchTemplate.Application.Common.Models;
using CleanArchTemplate.Application.Features.Permissions.Queries.GetPermissionAuditLog;
using CleanArchTemplate.Shared.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchTemplate.API.Controllers;

/// <summary>
/// Controller for accessing permission audit trails and compliance reporting
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PermissionAuditController : ControllerBase
{
    private readonly IMediator _mediator;

    public PermissionAuditController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get permission audit log with filtering and pagination
    /// </summary>
    /// <param name="query">Audit log query parameters</param>
    /// <returns>Paginated audit log entries</returns>
    [HttpGet]
    [RequirePermission("PermissionAudit.Read")]
    [ProducesResponseType(typeof(PaginatedResult<PermissionAuditLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedResult<PermissionAuditLogDto>>> GetAuditLog([FromQuery] GetPermissionAuditLogQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get audit log entries for a specific user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="pageNumber">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="fromDate">Start date filter</param>
    /// <param name="toDate">End date filter</param>
    /// <returns>User-specific audit log entries</returns>
    [HttpGet("user/{userId:guid}")]
    [RequirePermission("PermissionAudit.Read")]
    [ProducesResponseType(typeof(PaginatedResult<PermissionAuditLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedResult<PermissionAuditLogDto>>> GetUserAuditLog(
        Guid userId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var query = new GetPermissionAuditLogQuery
        {
            UserId = userId,
            PageNumber = pageNumber,
            PageSize = pageSize,
            FromDate = fromDate,
            ToDate = toDate
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get audit log entries for a specific role
    /// </summary>
    /// <param name="roleId">Role ID</param>
    /// <param name="pageNumber">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="fromDate">Start date filter</param>
    /// <param name="toDate">End date filter</param>
    /// <returns>Role-specific audit log entries</returns>
    [HttpGet("role/{roleId:guid}")]
    [RequirePermission("PermissionAudit.Read")]
    [ProducesResponseType(typeof(PaginatedResult<PermissionAuditLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedResult<PermissionAuditLogDto>>> GetRoleAuditLog(
        Guid roleId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var query = new GetPermissionAuditLogQuery
        {
            RoleId = roleId,
            PageNumber = pageNumber,
            PageSize = pageSize,
            FromDate = fromDate,
            ToDate = toDate
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get audit log entries for a specific permission
    /// </summary>
    /// <param name="permissionId">Permission ID</param>
    /// <param name="pageNumber">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="fromDate">Start date filter</param>
    /// <param name="toDate">End date filter</param>
    /// <returns>Permission-specific audit log entries</returns>
    [HttpGet("permission/{permissionId:guid}")]
    [RequirePermission("PermissionAudit.Read")]
    [ProducesResponseType(typeof(PaginatedResult<PermissionAuditLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedResult<PermissionAuditLogDto>>> GetPermissionAuditLog(
        Guid permissionId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var query = new GetPermissionAuditLogQuery
        {
            PermissionId = permissionId,
            PageNumber = pageNumber,
            PageSize = pageSize,
            FromDate = fromDate,
            ToDate = toDate
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get audit log entries by action type
    /// </summary>
    /// <param name="action">Action type (Assigned, Removed, Modified)</param>
    /// <param name="pageNumber">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="fromDate">Start date filter</param>
    /// <param name="toDate">End date filter</param>
    /// <returns>Action-specific audit log entries</returns>
    [HttpGet("action/{action}")]
    [RequirePermission("PermissionAudit.Read")]
    [ProducesResponseType(typeof(PaginatedResult<PermissionAuditLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedResult<PermissionAuditLogDto>>> GetAuditLogByAction(
        string action,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        if (!new[] { "Assigned", "Removed", "Modified" }.Contains(action))
        {
            return BadRequest("Invalid action type. Valid actions: Assigned, Removed, Modified");
        }

        var query = new GetPermissionAuditLogQuery
        {
            Action = action,
            PageNumber = pageNumber,
            PageSize = pageSize,
            FromDate = fromDate,
            ToDate = toDate
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Export audit log data for compliance reporting
    /// </summary>
    /// <param name="format">Export format (csv, excel, json)</param>
    /// <param name="fromDate">Start date filter</param>
    /// <param name="toDate">End date filter</param>
    /// <param name="userId">Optional user filter</param>
    /// <param name="roleId">Optional role filter</param>
    /// <param name="permissionId">Optional permission filter</param>
    /// <returns>Exported audit log data</returns>
    [HttpGet("export")]
    [RequirePermission("PermissionAudit.Export")]
    [ProducesResponseType(typeof(AuditLogExportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AuditLogExportDto>> ExportAuditLog(
        [FromQuery] string format = "json",
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] Guid? roleId = null,
        [FromQuery] Guid? permissionId = null)
    {
        if (!new[] { "csv", "excel", "json" }.Contains(format.ToLower()))
        {
            return BadRequest("Invalid export format. Supported formats: csv, excel, json");
        }

        var query = new GetPermissionAuditLogQuery
        {
            UserId = userId,
            RoleId = roleId,
            PermissionId = permissionId,
            FromDate = fromDate,
            ToDate = toDate,
            PageSize = int.MaxValue // Get all records for export
        };

        var result = await _mediator.Send(query);

        var exportData = new AuditLogExportDto
        {
            ExportFormat = format.ToLower(),
            ExportedAt = DateTime.UtcNow,
            FromDate = fromDate,
            ToDate = toDate,
            TotalRecords = result.TotalCount,
            Data = result.Items,
            FileName = $"permission-audit-log-{DateTime.UtcNow:yyyyMMdd-HHmmss}.{format.ToLower()}"
        };

        return Ok(exportData);
    }

    /// <summary>
    /// Get audit log statistics and summary
    /// </summary>
    /// <param name="fromDate">Start date filter</param>
    /// <param name="toDate">End date filter</param>
    /// <returns>Audit log statistics</returns>
    [HttpGet("statistics")]
    [RequirePermission("PermissionAudit.Read")]
    [ProducesResponseType(typeof(AuditLogStatisticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AuditLogStatisticsDto>> GetAuditStatistics(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var query = new GetPermissionAuditLogQuery
        {
            FromDate = fromDate,
            ToDate = toDate,
            PageSize = int.MaxValue // Get all records for statistics
        };

        var result = await _mediator.Send(query);

        var statistics = new AuditLogStatisticsDto
        {
            TotalEntries = result.TotalCount,
            DateRange = new DateRangeDto
            {
                FromDate = fromDate,
                ToDate = toDate
            },
            ActionBreakdown = result.Items
                .GroupBy(a => a.Action)
                .ToDictionary(g => g.Key, g => g.Count()),
            TopUsers = result.Items
                .Where(a => a.UserId.HasValue)
                .GroupBy(a => new { a.UserId, a.UserName })
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => new UserAuditSummaryDto
                {
                    UserId = g.Key.UserId!.Value,
                    UserName = g.Key.UserName ?? "Unknown",
                    AuditEntryCount = g.Count()
                }),
            TopPermissions = result.Items
                .GroupBy(a => new { a.PermissionId, a.PermissionName })
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => new PermissionAuditSummaryDto
                {
                    PermissionId = g.Key.PermissionId,
                    PermissionName = g.Key.PermissionName ?? "Unknown",
                    AuditEntryCount = g.Count()
                }),
            RecentActivity = result.Items
                .OrderByDescending(a => a.PerformedAt)
                .Take(20)
        };

        return Ok(statistics);
    }

    /// <summary>
    /// Get compliance report for a specific time period
    /// </summary>
    /// <param name="fromDate">Start date</param>
    /// <param name="toDate">End date</param>
    /// <returns>Compliance report</returns>
    [HttpGet("compliance-report")]
    [RequirePermission("PermissionAudit.ComplianceReport")]
    [ProducesResponseType(typeof(ComplianceReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ComplianceReportDto>> GetComplianceReport(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate)
    {
        if (fromDate >= toDate)
        {
            return BadRequest("FromDate must be earlier than ToDate");
        }

        var query = new GetPermissionAuditLogQuery
        {
            FromDate = fromDate,
            ToDate = toDate,
            PageSize = int.MaxValue
        };

        var result = await _mediator.Send(query);

        var report = new ComplianceReportDto
        {
            ReportPeriod = new DateRangeDto { FromDate = fromDate, ToDate = toDate },
            GeneratedAt = DateTime.UtcNow,
            TotalPermissionChanges = result.TotalCount,
            PermissionAssignments = result.Items.Count(a => a.Action == "Assigned"),
            PermissionRemovals = result.Items.Count(a => a.Action == "Removed"),
            PermissionModifications = result.Items.Count(a => a.Action == "Modified"),
            UniqueUsersAffected = result.Items.Where(a => a.UserId.HasValue).Select(a => a.UserId).Distinct().Count(),
            UniqueRolesAffected = result.Items.Where(a => a.RoleId.HasValue).Select(a => a.RoleId).Distinct().Count(),
            HighRiskChanges = result.Items.Where(a => IsHighRiskChange(a)).Count(),
            UnauthorizedAttempts = result.Items.Where(a => a.Action.Contains("Failed") || a.Action.Contains("Denied")).Count()
        };

        return Ok(report);
    }

    private static bool IsHighRiskChange(PermissionAuditLogDto auditEntry)
    {
        // Define high-risk permissions or patterns
        var highRiskPermissions = new[]
        {
            "Users.Delete",
            "Roles.Delete",
            "Permissions.Delete",
            "System.Admin",
            "Database.Admin"
        };

        return highRiskPermissions.Any(hrp => auditEntry.PermissionName?.Contains(hrp) == true);
    }
}

/// <summary>
/// DTO for audit log export
/// </summary>
public class AuditLogExportDto
{
    public string ExportFormat { get; set; } = string.Empty;
    public DateTime ExportedAt { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int TotalRecords { get; set; }
    public IEnumerable<PermissionAuditLogDto> Data { get; set; } = new List<PermissionAuditLogDto>();
    public string FileName { get; set; } = string.Empty;
}

/// <summary>
/// DTO for audit log statistics
/// </summary>
public class AuditLogStatisticsDto
{
    public int TotalEntries { get; set; }
    public DateRangeDto DateRange { get; set; } = new();
    public Dictionary<string, int> ActionBreakdown { get; set; } = new();
    public IEnumerable<UserAuditSummaryDto> TopUsers { get; set; } = new List<UserAuditSummaryDto>();
    public IEnumerable<PermissionAuditSummaryDto> TopPermissions { get; set; } = new List<PermissionAuditSummaryDto>();
    public IEnumerable<PermissionAuditLogDto> RecentActivity { get; set; } = new List<PermissionAuditLogDto>();
}

/// <summary>
/// DTO for date range
/// </summary>
public class DateRangeDto
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

/// <summary>
/// DTO for user audit summary
/// </summary>
public class UserAuditSummaryDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int AuditEntryCount { get; set; }
}

/// <summary>
/// DTO for permission audit summary
/// </summary>
public class PermissionAuditSummaryDto
{
    public Guid PermissionId { get; set; }
    public string PermissionName { get; set; } = string.Empty;
    public int AuditEntryCount { get; set; }
}

/// <summary>
/// DTO for compliance report
/// </summary>
public class ComplianceReportDto
{
    public DateRangeDto ReportPeriod { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
    public int TotalPermissionChanges { get; set; }
    public int PermissionAssignments { get; set; }
    public int PermissionRemovals { get; set; }
    public int PermissionModifications { get; set; }
    public int UniqueUsersAffected { get; set; }
    public int UniqueRolesAffected { get; set; }
    public int HighRiskChanges { get; set; }
    public int UnauthorizedAttempts { get; set; }
}