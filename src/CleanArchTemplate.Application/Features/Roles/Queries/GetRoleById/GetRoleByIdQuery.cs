using CleanArchTemplate.Application.Common.Models;
using CleanArchTemplate.Shared.Models;
using MediatR;

namespace CleanArchTemplate.Application.Features.Roles.Queries.GetRoleById;

public record GetRoleByIdQuery(Guid Id) : IRequest<Result<RoleDto>>;