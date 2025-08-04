using FluentValidation;

namespace CleanArchTemplate.Application.Features.Roles.Commands.CreateRole;

public class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Role name is required")
            .MaximumLength(100)
            .WithMessage("Role name must not exceed 100 characters")
            .Matches(@"^[a-zA-Z0-9_-]+$")
            .WithMessage("Role name can only contain letters, numbers, underscores, and hyphens");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Role description is required")
            .MaximumLength(500)
            .WithMessage("Role description must not exceed 500 characters");
    }
}