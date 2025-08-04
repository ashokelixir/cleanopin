using AutoMapper;
using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Application.Common.Models;
using CleanArchTemplate.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanArchTemplate.Application.Features.Users.Queries.GetAllUsers;

public class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, PaginatedResult<UserSummaryDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetAllUsersQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PaginatedResult<UserSummaryDto>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Users.Query();
        
        var totalCount = await query.CountAsync(cancellationToken);
        
        var users = await query
            .Skip((request.Pagination.PageNumber - 1) * request.Pagination.PageSize)
            .Take(request.Pagination.PageSize)
            .ToListAsync(cancellationToken);

        var userDtos = _mapper.Map<List<UserSummaryDto>>(users);

        return new PaginatedResult<UserSummaryDto>(
            userDtos,
            totalCount,
            request.Pagination.PageNumber,
            request.Pagination.PageSize);
    }
}