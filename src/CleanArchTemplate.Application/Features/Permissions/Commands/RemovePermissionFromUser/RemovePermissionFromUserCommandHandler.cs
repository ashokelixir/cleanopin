using MediatR;
using CleanArchTemplate.Domain.Interfaces;
using CleanArchTemplate.Domain.Events;
using CleanArchTemplate.Application.Common.Interfaces;

namespace CleanArchTemplate.Application.Features.Permissions.Commands.RemovePermissionFromUser;

public class RemovePermissionFromUserCommandHandler : IRequestHandler<RemovePermissionFromUserCommand>
{
    private readonly IUserPermissionRepository _userPermissionRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;
    private readonly IPermissionCacheService _cacheService;

    public RemovePermissionFromUserCommandHandler(
        IUserPermissionRepository userPermissionRepository,
        IPermissionRepository permissionRepository,
        IUnitOfWork unitOfWork,
        IPublisher publisher,
        IPermissionCacheService cacheService)
    {
        _userPermissionRepository = userPermissionRepository;
        _permissionRepository = permissionRepository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
        _cacheService = cacheService;
    }

    public async Task Handle(RemovePermissionFromUserCommand request, CancellationToken cancellationToken)
    {
        var userPermission = await _userPermissionRepository.GetByUserAndPermissionAsync(
            request.UserId, request.PermissionId, cancellationToken);

        if (userPermission == null)
        {
            throw new ArgumentException($"User permission assignment not found for User ID {request.UserId} and Permission ID {request.PermissionId}");
        }

        var permission = await _permissionRepository.GetByIdAsync(request.PermissionId, cancellationToken);
        var permissionName = permission?.Name ?? "Unknown";

        await _userPermissionRepository.DeleteAsync(userPermission.Id, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate user permission cache
        await _cacheService.InvalidateUserPermissionsAsync(request.UserId, cancellationToken);

        // Publish domain event
        await _publisher.Publish(new UserPermissionRemovedEvent(
            request.UserId,
            request.PermissionId,
            permissionName,
            userPermission.State,
            request.Reason), cancellationToken);
    }
}