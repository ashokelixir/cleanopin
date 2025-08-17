using MediatR;

namespace CleanArchTemplate.Application.Features.Permissions.Commands.UpdatePermission;

public record UpdatePermissionCommand : IRequest
{
    public Guid Id { get; init; }
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public Guid? ParentPermissionId { get; init; }
    public bool IsActive { get; init; } = true;
}