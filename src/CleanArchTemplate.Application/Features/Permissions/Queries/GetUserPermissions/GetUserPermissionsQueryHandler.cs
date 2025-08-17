using AutoMapper;
using CleanArchTemplate.Application.Common.Models;
using CleanArchTemplate.Domain.Interfaces;
using CleanArchTemplate.Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanArchTemplate.Application.Features.Permissions.Queries.GetUserPermissions;

public class GetUserPermissionsQueryHandler : IRequestHandler<GetUserPermissionsQuery, UserPermissionMatrixDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IUserPermissionRepository _userPermissionRepository;
    private readonly IPermissionEvaluationService _permissionEvaluationService;
    private readonly IMapper _mapper;

    public GetUserPermissionsQueryHandler(
        IUserRepository userRepository,
        IPermissionRepository permissionRepository,
        IUserPermissionRepository userPermissionRepository,
        IPermissionEvaluationService permissionEvaluationService,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _permissionRepository = permissionRepository;
        _userPermissionRepository = userPermissionRepository;
        _permissionEvaluationService = permissionEvaluationService;
        _mapper = mapper;
    }

    public async Task<UserPermissionMatrixDto> Handle(GetUserPermissionsQuery request, CancellationToken cancellationToken)
    {
        // Get user with roles and permissions
        var user = await _userRepository.GetUserWithRolesAndPermissionsAsync(request.UserId, cancellationToken);

        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {request.UserId} not found.");
        }

        // Get all permissions
        var allPermissionsFromRepo = await _permissionRepository.GetAllAsync(cancellationToken);
        
        // Apply filters
        var allPermissions = allPermissionsFromRepo.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(request.PermissionFilter))
        {
            allPermissions = allPermissions.Where(p => 
                p.Name.Contains(request.PermissionFilter) ||
                p.Resource.Contains(request.PermissionFilter) ||
                p.Action.Contains(request.PermissionFilter));
        }

        if (!string.IsNullOrWhiteSpace(request.CategoryFilter))
        {
            allPermissions = allPermissions.Where(p => p.Category.Contains(request.CategoryFilter));
        }

        if (!request.IncludeInactivePermissions)
        {
            allPermissions = allPermissions.Where(p => p.IsActive);
        }

        allPermissions = allPermissions
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Resource)
            .ThenBy(p => p.Action)
            .ToList();

        // Get role-based permissions
        var rolePermissions = user.UserRoles
            .Where(ur => ur.Role.IsActive)
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission)
            .Where(p => p.IsActive)
            .Distinct()
            .ToList();

        // Get user permission overrides
        var allUserOverrides = await _userPermissionRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        
        var userOverrides = allUserOverrides.AsEnumerable();

        if (!request.IncludeExpiredOverrides)
        {
            userOverrides = userOverrides.Where(up => 
                up.ExpiresAt == null || up.ExpiresAt > DateTime.UtcNow);
        }

        userOverrides = userOverrides.ToList();

        // Calculate effective permissions using the evaluation service
        var effectivePermissions = new List<Domain.Entities.Permission>();
        var userRoles = user.UserRoles.Select(ur => ur.Role).ToList();
        
        foreach (var permission in allPermissions)
        {
            var hasPermission = _permissionEvaluationService.HasPermission(
                user, 
                permission.Name, 
                allPermissions, 
                userRoles);
            
            if (hasPermission)
            {
                effectivePermissions.Add(permission);
            }
        }

        // Map to DTOs
        var userDto = _mapper.Map<UserDto>(user);
        var rolePermissionDtos = _mapper.Map<List<PermissionDto>>(rolePermissions);
        var userOverrideDtos = userOverrides.Select(uo => new UserPermissionOverrideDto
        {
            UserId = uo.UserId,
            Permission = _mapper.Map<PermissionDto>(uo.Permission),
            State = uo.State,
            Reason = uo.Reason,
            ExpiresAt = uo.ExpiresAt
        }).ToList();
        var effectivePermissionDtos = _mapper.Map<List<PermissionDto>>(effectivePermissions);

        return new UserPermissionMatrixDto
        {
            User = userDto,
            RolePermissions = rolePermissionDtos,
            UserOverrides = userOverrideDtos,
            EffectivePermissions = effectivePermissionDtos
        };
    }
}