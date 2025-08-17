using MediatR;
using CleanArchTemplate.Domain.Interfaces;
using CleanArchTemplate.Domain.Events;
using CleanArchTemplate.Domain.Exceptions;
using CleanArchTemplate.Application.Common.Interfaces;

namespace CleanArchTemplate.Application.Features.Permissions.Commands.UpdatePermission;

public class UpdatePermissionCommandHandler : IRequestHandler<UpdatePermissionCommand>
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;

    public UpdatePermissionCommandHandler(
        IPermissionRepository permissionRepository,
        IUnitOfWork unitOfWork,
        IPublisher publisher)
    {
        _permissionRepository = permissionRepository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task Handle(UpdatePermissionCommand request, CancellationToken cancellationToken)
    {
        var permission = await _permissionRepository.GetByIdAsync(request.Id, cancellationToken);
        if (permission == null)
        {
            throw new PermissionNotFoundException($"Permission with ID {request.Id}");
        }

        var oldValues = new
        {
            Description = permission.Description,
            Category = permission.Category,
            ParentPermissionId = permission.ParentPermissionId,
            IsActive = permission.IsActive
        };

        permission.Update(
            request.Description,
            request.Category,
            request.ParentPermissionId,
            request.IsActive);

        await _permissionRepository.UpdateAsync(permission, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish domain event
        await _publisher.Publish(new PermissionUpdatedEvent(
            permission.Id,
            permission.Resource,
            permission.Resource,
            permission.Action,
            permission.Action,
            permission.Name,
            permission.Name,
            oldValues.Description,
            request.Description,
            oldValues.Category,
            request.Category), cancellationToken);
    }
}