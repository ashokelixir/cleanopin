using AutoMapper;
using CleanArchTemplate.Application.Common.Models;
using CleanArchTemplate.Domain.Interfaces;
using CleanArchTemplate.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanArchTemplate.Application.Features.Permissions.Queries.GetPermissions;

public class GetPermissionsQueryHandler : IRequestHandler<GetPermissionsQuery, PaginatedResult<PermissionDto>>
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IMapper _mapper;

    public GetPermissionsQueryHandler(IPermissionRepository permissionRepository, IMapper mapper)
    {
        _permissionRepository = permissionRepository;
        _mapper = mapper;
    }

    public async Task<PaginatedResult<PermissionDto>> Handle(GetPermissionsQuery request, CancellationToken cancellationToken)
    {
        // Get all permissions with hierarchy
        var allPermissions = await _permissionRepository.GetPermissionsWithHierarchyAsync(cancellationToken);
        
        // Apply filters
        var filteredPermissions = allPermissions.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(request.Resource))
        {
            filteredPermissions = filteredPermissions.Where(p => p.Resource.Contains(request.Resource));
        }

        if (!string.IsNullOrWhiteSpace(request.Action))
        {
            filteredPermissions = filteredPermissions.Where(p => p.Action.Contains(request.Action));
        }

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            filteredPermissions = filteredPermissions.Where(p => p.Category.Contains(request.Category));
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            filteredPermissions = filteredPermissions.Where(p => 
                p.Name.Contains(request.SearchTerm) ||
                p.Description.Contains(request.SearchTerm) ||
                p.Resource.Contains(request.SearchTerm) ||
                p.Action.Contains(request.SearchTerm));
        }

        if (request.IsActive.HasValue)
        {
            filteredPermissions = filteredPermissions.Where(p => p.IsActive == request.IsActive.Value);
        }

        // Apply sorting
        filteredPermissions = request.SortBy?.ToLower() switch
        {
            "resource" => request.SortDirection?.ToLower() == "desc" 
                ? filteredPermissions.OrderByDescending(p => p.Resource)
                : filteredPermissions.OrderBy(p => p.Resource),
            "action" => request.SortDirection?.ToLower() == "desc"
                ? filteredPermissions.OrderByDescending(p => p.Action)
                : filteredPermissions.OrderBy(p => p.Action),
            "category" => request.SortDirection?.ToLower() == "desc"
                ? filteredPermissions.OrderByDescending(p => p.Category)
                : filteredPermissions.OrderBy(p => p.Category),
            "createdat" => request.SortDirection?.ToLower() == "desc"
                ? filteredPermissions.OrderByDescending(p => p.CreatedAt)
                : filteredPermissions.OrderBy(p => p.CreatedAt),
            _ => request.SortDirection?.ToLower() == "desc"
                ? filteredPermissions.OrderByDescending(p => p.Name)
                : filteredPermissions.OrderBy(p => p.Name)
        };

        var totalCount = filteredPermissions.Count();

        var permissions = filteredPermissions
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var permissionDtos = _mapper.Map<List<PermissionDto>>(permissions);

        return new PaginatedResult<PermissionDto>(
            permissionDtos,
            totalCount,
            request.PageNumber,
            request.PageSize);
    }
}