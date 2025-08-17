using FluentValidation;

namespace CleanArchTemplate.Application.Features.Users.Commands.InvalidateUserSessions;

public class InvalidateUserSessionsCommandValidator : AbstractValidator<InvalidateUserSessionsCommand>
{
    public InvalidateUserSessionsCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.");
    }
}