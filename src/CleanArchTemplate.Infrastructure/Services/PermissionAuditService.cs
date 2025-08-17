using AutoMapper;
using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Application.Common.Models;
using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.Interfaces;
using CleanArchTemplate.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace CleanArchTemplate.Infrastructure.Services;

/// <summary>
/// Service implementation for permission audit operations
/// </summary>
public class PermissionAuditService : IPermissionAuditService
{
    private readonly IPermissionAuditLogRepository _auditLogRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<PermissionAuditService> _logger;

    public PermissionAuditService(
        IPermissionAuditLogRepository auditLogRepository,
        IPermissionRepository permissionRepository,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<PermissionAuditService> logger)
    {
        _auditLogRepository = auditLogRepository ?? throw new ArgumentNullException(nameof(auditLogRepository));
        _permissionRepository = permissionRepository ?? throw new ArgumentNullException(nameof(permissionRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PermissionAuditLog> LogUserPermissionAssignedAsync(
        Guid userId, 
        Guid permissionId, 
        string performedBy, 
        string? reason = null, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(performedBy))
            throw new ArgumentException("PerformedBy cannot be null or empty.", nameof(performedBy));

        _logger.LogInformation("Logging user permission assignment: UserId={UserId}, PermissionId={PermissionId}, PerformedBy={PerformedBy}", 
            userId, permissionId, performedBy);

        var auditLog = PermissionAuditLog.CreateUserPermissionAssigned(userId, permissionId, performedBy, reason);
        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return auditLog;
    }

    public async Task<PermissionAuditLog> LogUserPermissionRemovedAsync(
        Guid userId, 
        Guid permissionId, 
        string performedBy, 
        string? reason = null, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Logging user permission removal: UserId={UserId}, PermissionId={PermissionId}, PerformedBy={PerformedBy}", 
            userId, permissionId, performedBy);

        var auditLog = PermissionAuditLog.CreateUserPermissionRemoved(userId, permissionId, performedBy, reason);
        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return auditLog;
    }

    public async Task<PermissionAuditLog> LogRolePermissionAssignedAsync(
        Guid roleId, 
        Guid permissionId, 
        string performedBy, 
        string? reason = null, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Logging role permission assignment: RoleId={RoleId}, PermissionId={PermissionId}, PerformedBy={PerformedBy}", 
            roleId, permissionId, performedBy);

        var auditLog = PermissionAuditLog.CreateRolePermissionAssigned(roleId, permissionId, performedBy, reason);
        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return auditLog;
    }

    public async Task<PermissionAuditLog> LogRolePermissionRemovedAsync(
        Guid roleId, 
        Guid permissionId, 
        string performedBy, 
        string? reason = null, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Logging role permission removal: RoleId={RoleId}, PermissionId={PermissionId}, PerformedBy={PerformedBy}", 
            roleId, permissionId, performedBy);

        var auditLog = PermissionAuditLog.CreateRolePermissionRemoved(roleId, permissionId, performedBy, reason);
        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return auditLog;
    }

    public async Task<PermissionAuditLog> LogPermissionModifiedAsync(
        Guid permissionId, 
        string oldValue, 
        string newValue, 
        string performedBy, 
        string? reason = null, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Logging permission modification: PermissionId={PermissionId}, PerformedBy={PerformedBy}", 
            permissionId, performedBy);

        var auditLog = PermissionAuditLog.CreatePermissionModified(permissionId, oldValue, newValue, performedBy, reason);
        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return auditLog;
    }

    public async Task<IEnumerable<PermissionAuditLog>> LogBulkPermissionAssignmentsAsync(
        IEnumerable<BulkPermissionAssignmentDto> assignments, 
        string performedBy, 
        string? reason = null, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Logging bulk permission assignments: Count={Count}, PerformedBy={PerformedBy}", 
            assignments.Count(), performedBy);

        var auditLogs = new List<PermissionAuditLog>();

        foreach (var assignment in assignments)
        {
            PermissionAuditLog auditLog;

            if (assignment.UserId.HasValue)
            {
                auditLog = assignment.Action == "Assigned"
                    ? PermissionAuditLog.CreateUserPermissionAssigned(assignment.UserId.Value, assignment.PermissionId, performedBy, reason)
                    : PermissionAuditLog.CreateUserPermissionRemoved(assignment.UserId.Value, assignment.PermissionId, performedBy, reason);
            }
            else if (assignment.RoleId.HasValue)
            {
                auditLog = assignment.Action == "Assigned"
                    ? PermissionAuditLog.CreateRolePermissionAssigned(assignment.RoleId.Value, assignment.PermissionId, performedBy, reason)
                    : PermissionAuditLog.CreateRolePermissionRemoved(assignment.RoleId.Value, assignment.PermissionId, performedBy, reason);
            }
            else
            {
                continue; // Skip invalid assignments
            }

            auditLogs.Add(auditLog);
            await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return auditLogs;
    }

    public async Task<PaginatedResult<PermissionAuditLogDto>> GetAuditLogsAsync(
        PermissionAuditLogFilterDto filter, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting audit logs with filter: PageNumber={PageNumber}, PageSize={PageSize}", 
            filter.PageNumber, filter.PageSize);

        var query = _auditLogRepository.Query()
            .Include(al => al.User)
            .Include(al => al.Role)
            .Include(al => al.Permission)
            .AsQueryable();

        // Apply filters
        if (filter.UserId.HasValue)
            query = query.Where(al => al.UserId == filter.UserId.Value);

        if (filter.RoleId.HasValue)
            query = query.Where(al => al.RoleId == filter.RoleId.Value);

        if (filter.PermissionId.HasValue)
            query = query.Where(al => al.PermissionId == filter.PermissionId.Value);

        if (!string.IsNullOrEmpty(filter.Action))
            query = query.Where(al => al.Action == filter.Action);

        if (!string.IsNullOrEmpty(filter.PerformedBy))
            query = query.Where(al => al.PerformedBy.Contains(filter.PerformedBy));

        if (filter.StartDate.HasValue)
            query = query.Where(al => al.PerformedAt >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(al => al.PerformedAt <= filter.EndDate.Value);

        if (!string.IsNullOrEmpty(filter.Resource))
            query = query.Where(al => al.Permission.Resource == filter.Resource);

        if (!string.IsNullOrEmpty(filter.PermissionAction))
            query = query.Where(al => al.Permission.Action == filter.PermissionAction);

        // Apply sorting
        query = filter.SortBy?.ToLower() switch
        {
            "performedat" => filter.SortDescending 
                ? query.OrderByDescending(al => al.PerformedAt)
                : query.OrderBy(al => al.PerformedAt),
            "action" => filter.SortDescending 
                ? query.OrderByDescending(al => al.Action)
                : query.OrderBy(al => al.Action),
            "performedby" => filter.SortDescending 
                ? query.OrderByDescending(al => al.PerformedBy)
                : query.OrderBy(al => al.PerformedBy),
            _ => query.OrderByDescending(al => al.PerformedAt)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(MapToDto).ToList();

        return new PaginatedResult<PermissionAuditLogDto>(
            dtos,
            totalCount,
            filter.PageNumber,
            filter.PageSize);
    }

    public async Task<AuditLogExportResultDto> ExportAuditLogsAsync(
        PermissionAuditLogFilterDto filter, 
        AuditLogExportFormat format, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Exporting audit logs in format: {Format}", format);

        // Get all matching records (without pagination for export)
        var exportFilter = new PermissionAuditLogFilterDto
        {
            UserId = filter.UserId,
            RoleId = filter.RoleId,
            PermissionId = filter.PermissionId,
            Action = filter.Action,
            PerformedBy = filter.PerformedBy,
            StartDate = filter.StartDate,
            EndDate = filter.EndDate,
            Resource = filter.Resource,
            PermissionAction = filter.PermissionAction,
            PageNumber = 1,
            PageSize = int.MaxValue,
            SortBy = filter.SortBy,
            SortDescending = filter.SortDescending
        };

        var result = await GetAuditLogsAsync(exportFilter, cancellationToken);

        return format switch
        {
            AuditLogExportFormat.Csv => await ExportToCsvAsync(result.Items, cancellationToken),
            AuditLogExportFormat.Json => await ExportToJsonAsync(result.Items, cancellationToken),
            AuditLogExportFormat.Excel => await ExportToExcelAsync(result.Items, cancellationToken),
            AuditLogExportFormat.Pdf => await ExportToPdfAsync(result.Items, cancellationToken),
            _ => throw new ArgumentException($"Unsupported export format: {format}")
        };
    }

    public async Task<ComplianceReportDto> GenerateComplianceReportAsync(
        ComplianceReportType reportType, 
        DateTime startDate, 
        DateTime endDate, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating compliance report: Type={ReportType}, StartDate={StartDate}, EndDate={EndDate}", 
            reportType, startDate, endDate);

        var auditLogs = await _auditLogRepository.GetByDateRangeAsync(startDate, endDate, cancellationToken);
        var auditLogsList = auditLogs.ToList();

        var report = new ComplianceReportDto
        {
            ReportType = reportType,
            StartDate = startDate,
            EndDate = endDate,
            GeneratedAt = DateTime.UtcNow,
            TotalPermissionChanges = auditLogsList.Count,
            UserPermissionChanges = auditLogsList.Count(al => al.UserId.HasValue),
            RolePermissionChanges = auditLogsList.Count(al => al.RoleId.HasValue),
            PermissionModifications = auditLogsList.Count(al => al.Action == "Modified")
        };

        switch (reportType)
        {
            case ComplianceReportType.AccessReview:
                report.UserAccessSummaries = await GenerateUserAccessSummariesAsync(cancellationToken);
                break;
            case ComplianceReportType.HighRiskActivities:
                report.HighRiskActivities = await GenerateHighRiskActivitiesAsync(auditLogsList, cancellationToken);
                break;
            case ComplianceReportType.ComplianceViolations:
                report.Violations = await GenerateComplianceViolationsAsync(auditLogsList, cancellationToken);
                break;
        }

        return report;
    }

    public async Task<AuditStatisticsDto> GetAuditStatisticsAsync(
        DateTime startDate, 
        DateTime endDate, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting audit statistics: StartDate={StartDate}, EndDate={EndDate}", startDate, endDate);

        var auditLogs = await _auditLogRepository.GetByDateRangeAsync(startDate, endDate, cancellationToken);
        var auditLogsList = auditLogs.ToList();

        var statistics = new AuditStatisticsDto
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalAuditEntries = auditLogsList.Count,
            UserPermissionAssignments = auditLogsList.Count(al => al.UserId.HasValue && al.Action == "Assigned"),
            UserPermissionRemovals = auditLogsList.Count(al => al.UserId.HasValue && al.Action == "Removed"),
            RolePermissionAssignments = auditLogsList.Count(al => al.RoleId.HasValue && al.Action == "Assigned"),
            RolePermissionRemovals = auditLogsList.Count(al => al.RoleId.HasValue && al.Action == "Removed"),
            PermissionModifications = auditLogsList.Count(al => al.Action == "Modified")
        };

        // Action statistics
        var actionGroups = auditLogsList.GroupBy(al => al.Action).ToList();
        statistics.ActionStatistics = actionGroups.Select(g => new AuditActionStatisticDto
        {
            Action = g.Key,
            Count = g.Count(),
            Percentage = (double)g.Count() / auditLogsList.Count * 100
        }).ToList();

        // Performer statistics
        var performerGroups = auditLogsList.GroupBy(al => al.PerformedBy).ToList();
        statistics.PerformerStatistics = performerGroups.Select(g => new AuditPerformerStatisticDto
        {
            PerformedBy = g.Key,
            Count = g.Count(),
            LastActivity = g.Max(al => al.PerformedAt)
        }).ToList();

        return statistics;
    }

    private PermissionAuditLogDto MapToDto(PermissionAuditLog auditLog)
    {
        return new PermissionAuditLogDto
        {
            Id = auditLog.Id,
            UserId = auditLog.UserId,
            UserName = auditLog.User?.FirstName + " " + auditLog.User?.LastName,
            UserEmail = auditLog.User?.Email,
            RoleId = auditLog.RoleId,
            RoleName = auditLog.Role?.Name,
            PermissionId = auditLog.PermissionId,
            PermissionName = auditLog.Permission?.Name ?? string.Empty,
            PermissionResource = auditLog.Permission?.Resource ?? string.Empty,
            PermissionAction = auditLog.Permission?.Action ?? string.Empty,
            Action = auditLog.Action,
            OldValue = auditLog.OldValue,
            NewValue = auditLog.NewValue,
            Reason = auditLog.Reason,
            PerformedBy = auditLog.PerformedBy,
            PerformedAt = auditLog.PerformedAt
        };
    }

    private async Task<AuditLogExportResultDto> ExportToCsvAsync(
        IEnumerable<PermissionAuditLogDto> auditLogs, 
        CancellationToken cancellationToken)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Id,UserId,UserName,UserEmail,RoleId,RoleName,PermissionId,PermissionName,PermissionResource,PermissionAction,Action,OldValue,NewValue,Reason,PerformedBy,PerformedAt");

        foreach (var log in auditLogs)
        {
            csv.AppendLine($"{log.Id},{log.UserId},{log.UserName},{log.UserEmail},{log.RoleId},{log.RoleName},{log.PermissionId},{log.PermissionName},{log.PermissionResource},{log.PermissionAction},{log.Action},{log.OldValue},{log.NewValue},{log.Reason},{log.PerformedBy},{log.PerformedAt:yyyy-MM-dd HH:mm:ss}");
        }

        return new AuditLogExportResultDto
        {
            FileData = Encoding.UTF8.GetBytes(csv.ToString()),
            FileName = $"permission_audit_log_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv",
            ContentType = "text/csv",
            RecordCount = auditLogs.Count(),
            GeneratedAt = DateTime.UtcNow
        };
    }

    private async Task<AuditLogExportResultDto> ExportToJsonAsync(
        IEnumerable<PermissionAuditLogDto> auditLogs, 
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(auditLogs, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        return new AuditLogExportResultDto
        {
            FileData = Encoding.UTF8.GetBytes(json),
            FileName = $"permission_audit_log_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json",
            ContentType = "application/json",
            RecordCount = auditLogs.Count(),
            GeneratedAt = DateTime.UtcNow
        };
    }

    private async Task<AuditLogExportResultDto> ExportToExcelAsync(
        IEnumerable<PermissionAuditLogDto> auditLogs, 
        CancellationToken cancellationToken)
    {
        // For now, return CSV format as Excel implementation would require additional dependencies
        // In a real implementation, you would use libraries like EPPlus or ClosedXML
        var csvResult = await ExportToCsvAsync(auditLogs, cancellationToken);
        
        return new AuditLogExportResultDto
        {
            FileData = csvResult.FileData,
            FileName = $"permission_audit_log_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx",
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            RecordCount = auditLogs.Count(),
            GeneratedAt = DateTime.UtcNow
        };
    }

    private async Task<AuditLogExportResultDto> ExportToPdfAsync(
        IEnumerable<PermissionAuditLogDto> auditLogs, 
        CancellationToken cancellationToken)
    {
        // For now, return CSV format as PDF implementation would require additional dependencies
        // In a real implementation, you would use libraries like iTextSharp or PdfSharp
        var csvResult = await ExportToCsvAsync(auditLogs, cancellationToken);
        
        return new AuditLogExportResultDto
        {
            FileData = csvResult.FileData,
            FileName = $"permission_audit_log_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf",
            ContentType = "application/pdf",
            RecordCount = auditLogs.Count(),
            GeneratedAt = DateTime.UtcNow
        };
    }

    private async Task<IEnumerable<UserAccessSummaryDto>> GenerateUserAccessSummariesAsync(
        CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);
        var summaries = new List<UserAccessSummaryDto>();

        foreach (var user in users)
        {
            // This would need to be implemented based on your permission evaluation logic
            summaries.Add(new UserAccessSummaryDto
            {
                UserId = user.Id,
                UserName = $"{user.FirstName} {user.LastName}",
                Email = user.Email,
                TotalPermissions = 0, // Calculate based on user's effective permissions
                DirectPermissions = 0, // Calculate based on user's direct permissions
                RoleBasedPermissions = 0, // Calculate based on user's role permissions
                LastPermissionChange = DateTime.UtcNow,
                Roles = new List<string>(),
                HighRiskPermissions = new List<string>()
            });
        }

        return summaries;
    }

    private async Task<IEnumerable<HighRiskActivityDto>> GenerateHighRiskActivitiesAsync(
        IList<PermissionAuditLog> auditLogs, 
        CancellationToken cancellationToken)
    {
        var highRiskActivities = new List<HighRiskActivityDto>();

        // Define high-risk patterns
        var adminPermissionChanges = auditLogs.Where(al => 
            al.Permission?.Resource?.Contains("Admin", StringComparison.OrdinalIgnoreCase) == true ||
            al.Permission?.Action?.Contains("Delete", StringComparison.OrdinalIgnoreCase) == true);

        foreach (var activity in adminPermissionChanges)
        {
            highRiskActivities.Add(new HighRiskActivityDto
            {
                ActivityType = "Administrative Permission Change",
                Description = $"{activity.Action} permission {activity.Permission?.Name} for {(activity.UserId.HasValue ? "user" : "role")}",
                PerformedBy = activity.PerformedBy,
                PerformedAt = activity.PerformedAt,
                RiskLevel = "High",
                AffectedResources = new[] { activity.Permission?.Resource ?? "Unknown" }
            });
        }

        return highRiskActivities;
    }

    private async Task<IEnumerable<ComplianceViolationDto>> GenerateComplianceViolationsAsync(
        IList<PermissionAuditLog> auditLogs, 
        CancellationToken cancellationToken)
    {
        var violations = new List<ComplianceViolationDto>();

        // Example: Detect permission assignments without proper reason
        var assignmentsWithoutReason = auditLogs.Where(al => 
            al.Action == "Assigned" && string.IsNullOrEmpty(al.Reason));

        foreach (var violation in assignmentsWithoutReason)
        {
            violations.Add(new ComplianceViolationDto
            {
                ViolationType = "Missing Justification",
                Description = "Permission assignment without proper reason/justification",
                UserId = violation.UserId,
                RoleId = violation.RoleId,
                DetectedAt = violation.PerformedAt,
                Severity = "Medium"
            });
        }

        return violations;
    }
}