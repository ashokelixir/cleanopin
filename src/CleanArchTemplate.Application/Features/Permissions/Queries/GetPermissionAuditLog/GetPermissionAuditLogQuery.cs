using CleanArchTemplate.Application.Common.Models;
using CleanArchTemplate.Shared.Models;
using MediatR;

namespace CleanArchTemplate.Application.Features.Permissions.Queries.GetPermissionAuditLog;

public class GetPermissionAuditLogQuery : IRequest<PaginatedResult<PermissionAuditLogDto>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public Guid? UserId { get; set; }
    public Guid? RoleId { get; set; }
    public Guid? PermissionId { get; set; }
    public string? Action { get; set; }
    public string? PerformedBy { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? SortBy { get; set; } = "PerformedAt";
    public string? SortDirection { get; set; } = "desc";
}