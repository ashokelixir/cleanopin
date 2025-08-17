using MediatR;

namespace CleanArchTemplate.Application.Features.Permissions.Commands.RemovePermissionFromUser;

public record RemovePermissionFromUserCommand : IRequest
{
    public Guid UserId { get; init; }
    public Guid PermissionId { get; init; }
    public string? Reason { get; init; }
}