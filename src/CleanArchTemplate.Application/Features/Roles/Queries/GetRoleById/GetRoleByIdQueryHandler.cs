using AutoMapper;
using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Application.Common.Models;
using CleanArchTemplate.Shared.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CleanArchTemplate.Application.Features.Roles.Queries.GetRoleById;

public class GetRoleByIdQueryHandler : IRequestHandler<GetRoleByIdQuery, Result<RoleDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetRoleByIdQueryHandler> _logger;

    public GetRoleByIdQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetRoleByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<RoleDto>> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Retrieving role by ID: {RoleId}", request.Id);

            var role = await _unitOfWork.Roles.GetRoleWithPermissionsByIdAsync(request.Id, cancellationToken);
            if (role == null)
            {
                _logger.LogWarning("Role not found with ID: {RoleId}", request.Id);
                return Result<RoleDto>.NotFound("Role not found.");
            }

            var roleDto = _mapper.Map<RoleDto>(role);
            
            _logger.LogInformation("Role successfully retrieved: {RoleId}", request.Id);
            return Result<RoleDto>.Success(roleDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving role: {RoleId}", request.Id);
            return Result<RoleDto>.InternalError("An error occurred while retrieving the role.");
        }
    }
}