using MediatR;
using CleanArchTemplate.Domain.Enums;

namespace CleanArchTemplate.Application.Features.Permissions.Commands.BulkAssignPermissions;

public record BulkAssignPermissionsCommand : IRequest<BulkAssignPermissionsResult>
{
    public Guid? UserId { get; init; }
    public Guid? RoleId { get; init; }
    public IEnumerable<PermissionAssignmentDto> Permissions { get; init; } = new List<PermissionAssignmentDto>();
    public string? Reason { get; init; }
}

public record PermissionAssignmentDto
{
    public Guid PermissionId { get; init; }
    public PermissionState State { get; init; } = PermissionState.Grant;
    public DateTime? ExpiresAt { get; init; }
}

public record BulkAssignPermissionsResult
{
    public int SuccessfulAssignments { get; init; }
    public int FailedAssignments { get; init; }
    public IEnumerable<string> Errors { get; init; } = new List<string>();
}