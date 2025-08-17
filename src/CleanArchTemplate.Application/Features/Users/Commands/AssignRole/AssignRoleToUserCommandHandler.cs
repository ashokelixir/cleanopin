using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Shared.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CleanArchTemplate.Application.Features.Users.Commands.AssignRole;

public class AssignRoleToUserCommandHandler : IRequestHandler<AssignRoleToUserCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<AssignRoleToUserCommandHandler> _logger;

    public AssignRoleToUserCommandHandler(
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogService,
        ILogger<AssignRoleToUserCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(AssignRoleToUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Attempting to assign role {RoleId} to user {UserId}", request.RoleId, request.UserId);

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

            if (!role.IsActive)
            {
                _logger.LogWarning("Cannot assign inactive role {RoleId} to user {UserId}", request.RoleId, request.UserId);
                return Result.BadRequest("Cannot assign an inactive role.");
            }

            // Check if user already has this role
            if (user.UserRoles.Any(ur => ur.RoleId == request.RoleId))
            {
                _logger.LogWarning("User {UserId} already has role {RoleId}", request.UserId, request.RoleId);
                return Result.Conflict("User already has this role assigned.");
            }

            user.AddRole(role);
            
            await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Log the audit event
            await _auditLogService.LogUserActionAsync(
                "RoleAssigned",
                request.UserId,
                $"Role '{role.Name}' assigned to user '{user.Email.Value}'",
                new { RoleId = request.RoleId, RoleName = role.Name });

            _logger.LogInformation("Role {RoleId} successfully assigned to user {UserId}", request.RoleId, request.UserId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while assigning role {RoleId} to user {UserId}", request.RoleId, request.UserId);
            return Result.InternalError("An error occurred while assigning the role.");
        }
    }
}