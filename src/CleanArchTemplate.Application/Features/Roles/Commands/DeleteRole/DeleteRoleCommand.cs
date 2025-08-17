using CleanArchTemplate.Shared.Models;
using MediatR;

namespace CleanArchTemplate.Application.Features.Roles.Commands.DeleteRole;

public record DeleteRoleCommand(Guid RoleId) : IRequest<Result>;