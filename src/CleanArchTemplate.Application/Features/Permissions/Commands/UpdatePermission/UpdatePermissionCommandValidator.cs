using FluentValidation;
using CleanArchTemplate.Domain.Interfaces;
using CleanArchTemplate.Domain.Exceptions;

namespace CleanArchTemplate.Application.Features.Permissions.Commands.UpdatePermission;

public class UpdatePermissionCommandValidator : AbstractValidator<UpdatePermissionCommand>
{
    private readonly IPermissionRepository _permissionRepository;

    public UpdatePermissionCommandValidator(IPermissionRepository permissionRepository)
    {
        _permissionRepository = permissionRepository;

        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Permission ID is required");

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
                var permission = await _permissionRepository.GetByIdAsync(command.Id, cancellationToken);
                return permission != null;
            })
            .WithMessage("Permission not found");

        RuleFor(x => x.ParentPermissionId)
            .MustAsync(async (parentId, cancellationToken) =>
            {
                if (parentId == null) return true;
                var parent = await _permissionRepository.GetByIdAsync(parentId.Value, cancellationToken);
                return parent != null;
            })
            .WithMessage("Parent permission does not exist")
            .When(x => x.ParentPermissionId.HasValue);

        RuleFor(x => x)
            .MustAsync(async (command, cancellationToken) =>
            {
                if (command.ParentPermissionId == null) return true;
                
                // Check for circular reference
                return !await WouldCreateCircularReference(command.Id, command.ParentPermissionId.Value, cancellationToken);
            })
            .WithMessage("Setting this parent would create a circular reference in the permission hierarchy")
            .When(x => x.ParentPermissionId.HasValue);
    }

    private async Task<bool> WouldCreateCircularReference(Guid permissionId, Guid parentId, CancellationToken cancellationToken)
    {
        // If trying to set self as parent
        if (permissionId == parentId) return true;

        // Check if the proposed parent has this permission as an ancestor
        var currentParent = await _permissionRepository.GetByIdAsync(parentId, cancellationToken);
        while (currentParent?.ParentPermissionId != null)
        {
            if (currentParent.ParentPermissionId == permissionId)
                return true;
            
            currentParent = await _permissionRepository.GetByIdAsync(currentParent.ParentPermissionId.Value, cancellationToken);
        }

        return false;
    }
}