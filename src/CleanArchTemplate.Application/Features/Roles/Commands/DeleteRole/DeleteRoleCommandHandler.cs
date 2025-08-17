using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Shared.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CleanArchTemplate.Application.Features.Roles.Commands.DeleteRole;

public class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<DeleteRoleCommandHandler> _logger;

    public DeleteRoleCommandHandler(
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogService,
        ILogger<DeleteRoleCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Attempting to delete role with ID: {RoleId}", request.RoleId);

            var role = await _unitOfWork.Roles.GetByIdAsync(request.RoleId, cancellationToken);
            if (role == null)
            {
                _logger.LogWarning("Role not found with ID: {RoleId}", request.RoleId);
                return Result.NotFound("Role not found.");
            }

            // Check if role is assigned to any users
            var userRoles = await _unitOfWork.Roles.GetUserRolesAsync(request.RoleId, cancellationToken);
            if (userRoles.Any())
            {
                _logger.LogWarning("Cannot delete role {RoleId} as it is assigned to users", request.RoleId);
                return Result.BadRequest("Cannot delete role as it is assigned to users. Please remove all user assignments first.");
            }

            // Deactivate role instead of hard delete for audit purposes
            role.Deactivate();
            
            await _unitOfWork.Roles.UpdateAsync(role, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Log the audit event
            await _auditLogService.LogRoleActionAsync(
                "RoleDeactivated",
                request.RoleId,
                null,
                $"Role '{role.Name}' was deactivated");

            _logger.LogInformation("Role successfully deactivated: {RoleId}", request.RoleId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting role: {RoleId}", request.RoleId);
            return Result.InternalError("An error occurred while deleting the role.");
        }
    }
}