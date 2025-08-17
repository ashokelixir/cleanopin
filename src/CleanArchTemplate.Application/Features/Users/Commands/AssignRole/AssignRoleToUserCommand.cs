using CleanArchTemplate.Shared.Models;
using MediatR;

namespace CleanArchTemplate.Application.Features.Users.Commands.AssignRole;

public record AssignRoleToUserCommand(
    Guid UserId,
    Guid RoleId) : IRequest<Result>;