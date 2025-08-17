using MediatR;
using CleanArchTemplate.Domain.Enums;

namespace CleanArchTemplate.Application.Features.Permissions.Commands.AssignPermissionToUser;

public record AssignPermissionToUserCommand : IRequest
{
    public Guid UserId { get; init; }
    public Guid PermissionId { get; init; }
    public PermissionState State { get; init; } = PermissionState.Grant;
    public string? Reason { get; init; }
    public DateTime? ExpiresAt { get; init; }
}