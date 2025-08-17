using MediatR;

namespace CleanArchTemplate.Application.Features.Permissions.Commands.CreatePermission;

public record CreatePermissionCommand : IRequest<Guid>
{
    public string Resource { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public Guid? ParentPermissionId { get; init; }
}