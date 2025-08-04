using CleanArchTemplate.Application.Common.Models;
using CleanArchTemplate.Shared.Models;
using MediatR;

namespace CleanArchTemplate.Application.Features.Roles.Commands.CreateRole;

public record CreateRoleCommand(
    string Name,
    string Description) : IRequest<Result<RoleDto>>;