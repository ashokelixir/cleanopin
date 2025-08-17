using AutoMapper;
using CleanArchTemplate.Application.Common.Models;
using CleanArchTemplate.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanArchTemplate.Application.Features.Permissions.Queries.GetRolePermissionMatrix;

public class GetRolePermissionMatrixQueryHandler : IRequestHandler<GetRolePermissionMatrixQuery, PermissionMatrixDto>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IMapper _mapper;

    public GetRolePermissionMatrixQueryHandler(
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IMapper mapper)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _mapper = mapper;
    }

    public async Task<PermissionMatrixDto> Handle(GetRolePermissionMatrixQuery request, CancellationToken cancellationToken)
    {
        // Get roles with permissions
        var allRoles = await _roleRepository.GetRolesWithPermissionsAsync(cancellationToken);
        
        // Apply filters
        var roles = allRoles.AsEnumerable();
        
        if (!string.IsNullOrWhiteSpace(request.RoleFilter))
        {
            roles = roles.Where(r => r.Name.Contains(request.RoleFilter));
        }

        if (!request.IncludeInactiveRoles)
        {
            roles = roles.Where(r => r.IsActive);
        }

        roles = roles.OrderBy(r => r.Name).ToList();

        // Get all permissions
        var allPermissions = await _permissionRepository.GetAllAsync(cancellationToken);
        
        // Apply filters
        var permissions = allPermissions.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(request.PermissionFilter))
        {
            permissions = permissions.Where(p => 
                p.Name.Contains(request.PermissionFilter) ||
                p.Resource.Contains(request.PermissionFilter) ||
                p.Action.Contains(request.PermissionFilter));
        }

        if (!string.IsNullOrWhiteSpace(request.CategoryFilter))
        {
            permissions = permissions.Where(p => p.Category.Contains(request.CategoryFilter));
        }

        if (!request.IncludeInactivePermissions)
        {
            permissions = permissions.Where(p => p.IsActive);
        }

        permissions = permissions
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Resource)
            .ThenBy(p => p.Action)
            .ToList();

        // Build role permission assignments
        var assignments = new List<RolePermissionAssignmentDto>();
        
        foreach (var role in roles)
        {
            foreach (var permission in permissions)
            {
                var hasPermission = role.RolePermissions.Any(rp => rp.PermissionId == permission.Id);
                assignments.Add(new RolePermissionAssignmentDto
                {
                    RoleId = role.Id,
                    PermissionId = permission.Id,
                    IsAssigned = hasPermission
                });
            }
        }

        return new PermissionMatrixDto
        {
            Roles = _mapper.Map<List<RoleDto>>(roles),
            Permissions = _mapper.Map<List<PermissionDto>>(permissions),
            Assignments = assignments
        };
    }
}