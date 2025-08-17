using CleanArchTemplate.Application.Common.Models;
using CleanArchTemplate.Shared.Models;
using MediatR;

namespace CleanArchTemplate.Application.Features.Roles.Commands.UpdateRole;

public record UpdateRoleCommand(
    Guid RoleId,
    string Name,
    string Description) : IRequest<Result<RoleDto>>;