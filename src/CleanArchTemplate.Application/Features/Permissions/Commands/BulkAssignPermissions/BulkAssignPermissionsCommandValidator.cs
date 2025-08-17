using FluentValidation;
using CleanArchTemplate.Domain.Interfaces;
using CleanArchTemplate.Domain.Enums;

namespace CleanArchTemplate.Application.Features.Permissions.Commands.BulkAssignPermissions;

public class BulkAssignPermissionsCommandValidator : AbstractValidator<BulkAssignPermissionsCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;

    public BulkAssignPermissionsCommandValidator(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;

        RuleFor(x => x)
            .Must(x => x.UserId.HasValue || x.RoleId.HasValue)
            .WithMessage("Either UserId or RoleId must be provided");

        RuleFor(x => x)
            .Must(x => !(x.UserId.HasValue && x.RoleId.HasValue))
            .WithMessage("Cannot assign permissions to both User and Role simultaneously");

        RuleFor(x => x.UserId)
            .MustAsync(async (userId, cancellationToken) =>
            {
                if (!userId.HasValue) return true;
                var user = await _userRepository.GetByIdAsync(userId.Value, cancellationToken);
                return user != null;
            })
            .WithMessage("User not found")
            .When(x => x.UserId.HasValue);

        RuleFor(x => x.RoleId)
            .MustAsync(async (roleId, cancellationToken) =>
            {
                if (!roleId.HasValue) return true;
                var role = await _roleRepository.GetByIdAsync(roleId.Value, cancellationToken);
                return role != null;
            })
            .WithMessage("Role not found")
            .When(x => x.RoleId.HasValue);

        RuleFor(x => x.Permissions)
            .NotEmpty()
            .WithMessage("At least one permission must be provided")
            .Must(permissions => permissions.Count() <= 100)
            .WithMessage("Cannot assign more than 100 permissions in a single operation");

        RuleFor(x => x.Reason)
            .MaximumLength(500)
            .WithMessage("Reason must not exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Reason));

        RuleForEach(x => x.Permissions)
            .SetValidator(new PermissionAssignmentDtoValidator(_permissionRepository));
    }
}

public class PermissionAssignmentDtoValidator : AbstractValidator<PermissionAssignmentDto>
{
    private readonly IPermissionRepository _permissionRepository;

    public PermissionAssignmentDtoValidator(IPermissionRepository permissionRepository)
    {
        _permissionRepository = permissionRepository;

        RuleFor(x => x.PermissionId)
            .NotEmpty()
            .WithMessage("Permission ID is required")
            .MustAsync(async (permissionId, cancellationToken) =>
            {
                var permission = await _permissionRepository.GetByIdAsync(permissionId, cancellationToken);
                return permission != null && permission.IsActive;
            })
            .WithMessage("Permission not found or inactive");

        RuleFor(x => x.State)
            .IsInEnum()
            .WithMessage("Invalid permission state");

        RuleFor(x => x.ExpiresAt)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Expiration date must be in the future")
            .When(x => x.ExpiresAt.HasValue);
    }
}