using MediatR;
using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.Interfaces;
using CleanArchTemplate.Domain.Events;
using CleanArchTemplate.Application.Common.Interfaces;

namespace CleanArchTemplate.Application.Features.Permissions.Commands.BulkAssignPermissions;

public class BulkAssignPermissionsCommandHandler : IRequestHandler<BulkAssignPermissionsCommand, BulkAssignPermissionsResult>
{
    private readonly IUserPermissionRepository _userPermissionRepository;
    private readonly IRolePermissionRepository _rolePermissionRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;
    private readonly IPermissionCacheService _cacheService;

    public BulkAssignPermissionsCommandHandler(
        IUserPermissionRepository userPermissionRepository,
        IRolePermissionRepository rolePermissionRepository,
        IPermissionRepository permissionRepository,
        IUnitOfWork unitOfWork,
        IPublisher publisher,
        IPermissionCacheService cacheService)
    {
        _userPermissionRepository = userPermissionRepository;
        _rolePermissionRepository = rolePermissionRepository;
        _permissionRepository = permissionRepository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
        _cacheService = cacheService;
    }

    public async Task<BulkAssignPermissionsResult> Handle(BulkAssignPermissionsCommand request, CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        var successfulAssignments = 0;
        var failedAssignments = 0;

        var permissions = await GetPermissionsAsync(request.Permissions.Select(p => p.PermissionId), cancellationToken);

        foreach (var permissionAssignment in request.Permissions)
        {
            try
            {
                var permission = permissions.FirstOrDefault(p => p.Id == permissionAssignment.PermissionId);
                if (permission == null)
                {
                    errors.Add($"Permission with ID {permissionAssignment.PermissionId} not found");
                    failedAssignments++;
                    continue;
                }

                if (request.UserId.HasValue)
                {
                    await AssignPermissionToUserAsync(request.UserId.Value, permissionAssignment, request.Reason, cancellationToken);
                }
                else if (request.RoleId.HasValue)
                {
                    await AssignPermissionToRoleAsync(request.RoleId.Value, permissionAssignment.PermissionId, cancellationToken);
                }

                successfulAssignments++;

                // Publish domain event
                if (request.UserId.HasValue)
                {
                    await _publisher.Publish(new UserPermissionAssignedEvent(
                        request.UserId.Value,
                        permissionAssignment.PermissionId,
                        permission.Name,
                        permissionAssignment.State,
                        request.Reason), cancellationToken);
                }
                else if (request.RoleId.HasValue)
                {
                    // Note: Role name would need to be retrieved from role repository for complete event
                    // For now, using placeholder - this should be enhanced in a future iteration
                    await _publisher.Publish(new RolePermissionAssignedEvent(
                        request.RoleId.Value,
                        permissionAssignment.PermissionId,
                        "Role", // Placeholder - should retrieve actual role name
                        permission.Name), cancellationToken);
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Failed to assign permission {permissionAssignment.PermissionId}: {ex.Message}");
                failedAssignments++;
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        if (request.UserId.HasValue)
        {
            await _cacheService.InvalidateUserPermissionsAsync(request.UserId.Value, cancellationToken);
        }
        else if (request.RoleId.HasValue)
        {
            await _cacheService.InvalidateRolePermissionsAsync(request.RoleId.Value, cancellationToken);
        }

        return new BulkAssignPermissionsResult
        {
            SuccessfulAssignments = successfulAssignments,
            FailedAssignments = failedAssignments,
            Errors = errors
        };
    }

    private async Task<IEnumerable<Permission>> GetPermissionsAsync(IEnumerable<Guid> permissionIds, CancellationToken cancellationToken)
    {
        var permissions = new List<Permission>();
        foreach (var permissionId in permissionIds)
        {
            var permission = await _permissionRepository.GetByIdAsync(permissionId, cancellationToken);
            if (permission != null)
            {
                permissions.Add(permission);
            }
        }
        return permissions;
    }

    private async Task AssignPermissionToUserAsync(Guid userId, PermissionAssignmentDto assignment, string? reason, CancellationToken cancellationToken)
    {
        // Check if user already has this permission
        var existingPermission = await _userPermissionRepository.GetByUserAndPermissionAsync(userId, assignment.PermissionId, cancellationToken);
        if (existingPermission != null)
        {
            throw new InvalidOperationException($"User already has permission {assignment.PermissionId} assigned");
        }

        var userPermission = UserPermission.Create(
            userId,
            assignment.PermissionId,
            assignment.State,
            reason,
            assignment.ExpiresAt);

        await _userPermissionRepository.AddAsync(userPermission, cancellationToken);
    }

    private async Task AssignPermissionToRoleAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken)
    {
        // Check if role already has this permission
        var existingPermission = await _rolePermissionRepository.GetByRoleAndPermissionAsync(roleId, permissionId, cancellationToken);
        if (existingPermission != null)
        {
            throw new InvalidOperationException($"Role already has permission {permissionId} assigned");
        }

        var rolePermission = RolePermission.Create(roleId, permissionId);
        await _rolePermissionRepository.AddAsync(rolePermission, cancellationToken);
    }
}