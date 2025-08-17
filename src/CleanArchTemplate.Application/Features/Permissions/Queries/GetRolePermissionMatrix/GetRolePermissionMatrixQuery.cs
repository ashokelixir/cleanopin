using CleanArchTemplate.Application.Common.Models;
using MediatR;

namespace CleanArchTemplate.Application.Features.Permissions.Queries.GetRolePermissionMatrix;

public class GetRolePermissionMatrixQuery : IRequest<PermissionMatrixDto>
{
    public Guid? RoleId { get; set; }
    public string? RoleFilter { get; set; }
    public string? PermissionFilter { get; set; }
    public string? CategoryFilter { get; set; }
    public bool IncludeInactiveRoles { get; set; } = false;
    public bool IncludeInactivePermissions { get; set; } = false;
    public bool IncludeStatistics { get; set; } = false;
}