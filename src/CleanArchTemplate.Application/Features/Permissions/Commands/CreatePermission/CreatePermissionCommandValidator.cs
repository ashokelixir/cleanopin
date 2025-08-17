using FluentValidation;
using CleanArchTemplate.Domain.Interfaces;

namespace CleanArchTemplate.Application.Features.Permissions.Commands.CreatePermission;

public class CreatePermissionCommandValidator : AbstractValidator<CreatePermissionCommand>
{
    private readonly IPermissionRepository _permissionRepository;

    public CreatePermissionCommandValidator(IPermissionRepository permissionRepository)
    {
        _permissionRepository = permissionRepository;

        RuleFor(x => x.Resource)
            .NotEmpty()
            .WithMessage("Resource is required")
            .MaximumLength(100)
            .WithMessage("Resource must not exceed 100 characters")
            .Matches("^[A-Za-z][A-Za-z0-9_]*$")
            .WithMessage("Resource must start with a letter and contain only letters, numbers, and underscores");

        RuleFor(x => x.Action)
            .NotEmpty()
            .WithMessage("Action is required")
            .MaximumLength(100)
            .WithMessage("Action must not exceed 100 characters")
            .Matches("^[A-Za-z][A-Za-z0-9_]*$")
            .WithMessage("Action must start with a letter and contain only letters, numbers, and underscores");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required")
            .MaximumLength(500)
            .WithMessage("Description must not exceed 500 characters");

        RuleFor(x => x.Category)
            .NotEmpty()
            .WithMessage("Category is required")
            .MaximumLength(50)
            .WithMessage("Category must not exceed 50 characters");

        RuleFor(x => x)
            .MustAsync(async (command, cancellationToken) =>
            {
                return !await _permissionRepository.ExistsAsync(command.Resource, command.Action, cancellationToken);
            })
            .WithMessage("A permission with this resource and action combination already exists");

        RuleFor(x => x.ParentPermissionId)
            .MustAsync(async (parentId, cancellationToken) =>
            {
                if (parentId == null) return true;
                var parent = await _permissionRepository.GetByIdAsync(parentId.Value, cancellationToken);
                return parent != null;
            })
            .WithMessage("Parent permission does not exist")
            .When(x => x.ParentPermissionId.HasValue);
    }
}