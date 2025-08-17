using CleanArchTemplate.Shared.Models;
using MediatR;

namespace CleanArchTemplate.Application.Features.Users.Commands.RemoveRole;

public record RemoveRoleFromUserCommand(
    Guid UserId,
    Guid RoleId) : IRequest<Result>;