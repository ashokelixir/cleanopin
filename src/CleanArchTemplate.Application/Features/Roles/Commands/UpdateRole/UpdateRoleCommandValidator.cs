using FluentValidation;

namespace CleanArchTemplate.Application.Features.Roles.Commands.UpdateRole;

public class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(x => x.RoleId)
            .NotEmpty()
            .WithMessage("Role ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Role name is required.")
            .MaximumLength(100)
            .WithMessage("Role name cannot exceed 100 characters.");

        RuleFor(x => x.Description)
            .NotNull()
            .WithMessage("Description is required.")
            .MaximumLength(500)
            .WithMessage("Description cannot exceed 500 characters.");
    }
}