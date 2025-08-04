using CleanArchTemplate.Application.Common.Models;
using CleanArchTemplate.Shared.Models;
using MediatR;

namespace CleanArchTemplate.Application.Features.Roles.Queries.GetAllRoles;

public record GetAllRolesQuery(PaginationRequest Pagination) : IRequest<PaginatedResult<RoleSummaryDto>>;