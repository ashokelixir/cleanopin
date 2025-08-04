using AutoMapper;
using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Application.Common.Models;
using CleanArchTemplate.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanArchTemplate.Application.Features.Roles.Queries.GetAllRoles;

public class GetAllRolesQueryHandler : IRequestHandler<GetAllRolesQuery, PaginatedResult<RoleSummaryDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetAllRolesQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PaginatedResult<RoleSummaryDto>> Handle(GetAllRolesQuery request, CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Roles.Query();
        
        var totalCount = await query.CountAsync(cancellationToken);
        
        var roles = await query
            .Skip((request.Pagination.PageNumber - 1) * request.Pagination.PageSize)
            .Take(request.Pagination.PageSize)
            .ToListAsync(cancellationToken);

        var roleDtos = _mapper.Map<List<RoleSummaryDto>>(roles);

        return new PaginatedResult<RoleSummaryDto>(
            roleDtos,
            totalCount,
            request.Pagination.PageNumber,
            request.Pagination.PageSize);
    }
}