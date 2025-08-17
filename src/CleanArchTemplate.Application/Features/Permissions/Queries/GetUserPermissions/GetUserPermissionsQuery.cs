using CleanArchTemplate.Application.Common.Models;
using MediatR;

namespace CleanArchTemplate.Application.Features.Permissions.Queries.GetUserPermissions;

public class GetUserPermissionsQuery : IRequest<UserPermissionMatrixDto>
{
    public Guid UserId { get; set; }
    public string? PermissionFilter { get; set; }
    public string? CategoryFilter { get; set; }
    public bool IncludeInactivePermissions { get; set; } = false;
    public bool IncludeExpiredOverrides { get; set; } = false;
    public bool OverridesOnly { get; set; } = false;
}