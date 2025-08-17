using AutoMapper;
using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Application.Common.Models;
using CleanArchTemplate.Shared.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CleanArchTemplate.Application.Features.Roles.Commands.UpdateRole;

public class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, Result<RoleDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<UpdateRoleCommandHandler> _logger;

    public UpdateRoleCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IAuditLogService auditLogService,
        ILogger<UpdateRoleCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<RoleDto>> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Attempting to update role with ID: {RoleId}", request.RoleId);

            var role = await _unitOfWork.Roles.GetByIdAsync(request.RoleId, cancellationToken);
            if (role == null)
            {
                _logger.LogWarning("Role not found with ID: {RoleId}", request.RoleId);
                return Result<RoleDto>.NotFound("Role not found.");
            }

            // Check if role name already exists (excluding current role)
            var nameExists = await _unitOfWork.Roles.IsNameExistsAsync(request.Name, request.RoleId, cancellationToken);
            if (nameExists)
            {
                _logger.LogWarning("Role name already exists: {RoleName}", request.Name);
                return Result<RoleDto>.Conflict("A role with this name already exists.");
            }

            var oldName = role.Name;
            var oldDescription = role.Description;

            role.Update(request.Name, request.Description);
            
            await _unitOfWork.Roles.UpdateAsync(role, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Log the audit event
            await _auditLogService.LogRoleActionAsync(
                "RoleUpdated",
                request.RoleId,
                null,
                $"Role updated from '{oldName}' to '{role.Name}', description from '{oldDescription}' to '{role.Description}'");

            var roleDto = _mapper.Map<RoleDto>(role);
            
            _logger.LogInformation("Role successfully updated: {RoleId}", request.RoleId);
            return Result<RoleDto>.Success(roleDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating role: {RoleId}", request.RoleId);
            return Result<RoleDto>.InternalError("An error occurred while updating the role.");
        }
    }
}