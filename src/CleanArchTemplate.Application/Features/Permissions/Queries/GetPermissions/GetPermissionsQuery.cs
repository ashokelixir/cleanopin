using CleanArchTemplate.Application.Common.Models;
using CleanArchTemplate.Shared.Models;
using MediatR;

namespace CleanArchTemplate.Application.Features.Permissions.Queries.GetPermissions;

public class GetPermissionsQuery : IRequest<PaginatedResult<PermissionDto>>
{
    public Guid? Id { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Resource { get; set; }
    public string? Action { get; set; }
    public string? Category { get; set; }
    public string? SearchTerm { get; set; }
    public bool? IsActive { get; set; }
    public string? SortBy { get; set; } = "Name";
    public string? SortDirection { get; set; } = "asc";
}