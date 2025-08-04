using CleanArchTemplate.Application.Common.Models;
using CleanArchTemplate.Shared.Models;
using MediatR;

namespace CleanArchTemplate.Application.Features.Users.Queries.GetAllUsers;

public record GetAllUsersQuery(PaginationRequest Pagination) : IRequest<PaginatedResult<UserSummaryDto>>;