using CleanArchTemplate.Shared.Models;
using MediatR;

namespace CleanArchTemplate.Application.Features.Users.Commands.DeleteUser;

public record DeleteUserCommand(Guid UserId) : IRequest<Result>;