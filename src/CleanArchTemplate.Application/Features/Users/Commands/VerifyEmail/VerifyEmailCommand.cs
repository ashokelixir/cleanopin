using CleanArchTemplate.Shared.Models;
using MediatR;

namespace CleanArchTemplate.Application.Features.Users.Commands.VerifyEmail;

public record VerifyEmailCommand(string Token) : IRequest<Result>;