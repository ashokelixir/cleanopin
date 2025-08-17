using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Shared.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CleanArchTemplate.Application.Features.Users.Commands.RemoveRole;

public class RemoveRoleFromUserCommandHandler : IRequestHandler<RemoveRoleFromUserCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<RemoveRoleFromUserCommandHandler> _logger;

    public RemoveRoleFromUserCommandHandler(
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogService,
        ILogger<RemoveRoleFromUserCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(RemoveRoleFromUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Attempting to remove role {RoleId} from user {UserId}", request.RoleId, request.UserId);

            var user = await _unitOfWork.Users.GetUserWithRolesByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("User not found with ID: {UserId}", request.UserId);
                return Result.NotFound("User not found.");
            }

            var role = await _unitOfWork.Roles.GetByIdAsync(request.RoleId, cancellationToken);
            if (role == null)
            {
                _logger.LogWarning("Role not found with ID: {RoleId}", request.RoleId);
                return Result.NotFound("Role not found.");
            }

            // Check if user has this role
            if (!user.UserRoles.Any(ur => ur.RoleId == request.RoleId))
            {
                _logger.LogWarning("User {UserId} does not have role {RoleId}", request.UserId, request.RoleId);
                return Result.NotFound("User does not have this role assigned.");
            }

            user.RemoveRole(request.RoleId);
            
            await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Log the audit event
            await _auditLogService.LogUserActionAsync(
                "RoleRemoved",
                request.UserId,
                $"Role '{role.Name}' removed from user '{user.Email.Value}'",
                new { RoleId = request.RoleId, RoleName = role.Name });

            _logger.LogInformation("Role {RoleId} successfully removed from user {UserId}", request.RoleId, request.UserId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while removing role {RoleId} from user {UserId}", request.RoleId, request.UserId);
            return Result.InternalError("An error occurred while removing the role.");
        }
    }
}