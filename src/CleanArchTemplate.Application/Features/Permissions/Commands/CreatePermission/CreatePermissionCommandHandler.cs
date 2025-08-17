using MediatR;
using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.Interfaces;
using CleanArchTemplate.Domain.Events;
using CleanArchTemplate.Application.Common.Interfaces;

namespace CleanArchTemplate.Application.Features.Permissions.Commands.CreatePermission;

public class CreatePermissionCommandHandler : IRequestHandler<CreatePermissionCommand, Guid>
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;

    public CreatePermissionCommandHandler(
        IPermissionRepository permissionRepository,
        IUnitOfWork unitOfWork,
        IPublisher publisher)
    {
        _permissionRepository = permissionRepository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task<Guid> Handle(CreatePermissionCommand request, CancellationToken cancellationToken)
    {
        var permission = Permission.Create(
            request.Resource,
            request.Action,
            request.Description,
            request.Category,
            request.ParentPermissionId);

        await _permissionRepository.AddAsync(permission, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish domain event
        await _publisher.Publish(new PermissionCreatedEvent(permission.Id, permission.Resource, permission.Action, permission.Name, permission.Description, permission.Category, permission.ParentPermissionId), cancellationToken);

        return permission.Id;
    }
}