using CleanArchTemplate.Shared.Models;
using MediatR;

namespace CleanArchTemplate.Application.Features.Users.Commands.InvalidateUserSessions;

public record InvalidateUserSessionsCommand(Guid UserId) : IRequest<Result>;