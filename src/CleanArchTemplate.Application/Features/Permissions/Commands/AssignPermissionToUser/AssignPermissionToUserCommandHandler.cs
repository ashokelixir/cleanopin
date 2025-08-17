using MediatR;
using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.Interfaces;
using CleanArchTemplate.Domain.Events;
using CleanArchTemplate.Application.Common.Interfaces;

namespace CleanArchTemplate.Application.Features.Permissions.Commands.AssignPermissionToUser;

public class AssignPermissionToUserCommandHandler : IRequestHandler<AssignPermissionToUserCommand>
{
    private readonly IUserPermissionRepository _userPermissionRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;
    private readonly IPermissionCacheService _cacheService;

    public AssignPermissionToUserCommandHandler(
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

    public async Task Handle(AssignPermissionToUserCommand request, CancellationToken cancellationToken)
    {
        var permission = await _permissionRepository.GetByIdAsync(request.PermissionId, cancellationToken);
        if (permission == null)
        {
            throw new ArgumentException($"Permission with ID {request.PermissionId} not found");
        }

        var userPermission = UserPermission.Create(
            request.UserId,
            request.PermissionId,
            request.State,
            request.Reason,
            request.ExpiresAt);

        await _userPermissionRepository.AddAsync(userPermission, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate user permission cache
        await _cacheService.InvalidateUserPermissionsAsync(request.UserId, cancellationToken);

        // Publish domain event
        await _publisher.Publish(new UserPermissionAssignedEvent(
            request.UserId,
            request.PermissionId,
            permission.Name,
            request.State,
            request.Reason), cancellationToken);
    }
}