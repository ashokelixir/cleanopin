using FluentValidation;
using CleanArchTemplate.Domain.Interfaces;

namespace CleanArchTemplate.Application.Features.Permissions.Commands.RemovePermissionFromUser;

public class RemovePermissionFromUserCommandValidator : AbstractValidator<RemovePermissionFromUserCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IUserPermissionRepository _userPermissionRepository;

    public RemovePermissionFromUserCommandValidator(
        IUserRepository userRepository,
        IPermissionRepository permissionRepository,
        IUserPermissionRepository userPermissionRepository)
    {
        _userRepository = userRepository;
        _permissionRepository = permissionRepository;
        _userPermissionRepository = userPermissionRepository;

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required")
            .MustAsync(async (userId, cancellationToken) =>
            {
                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
                return user != null;
            })
            .WithMessage("User not found");

        RuleFor(x => x.PermissionId)
            .NotEmpty()
            .WithMessage("Permission ID is required")
            .MustAsync(async (permissionId, cancellationToken) =>
            {
                var permission = await _permissionRepository.GetByIdAsync(permissionId, cancellationToken);
                return permission != null;
            })
            .WithMessage("Permission not found");

        RuleFor(x => x.Reason)
            .MaximumLength(500)
            .WithMessage("Reason must not exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Reason));

        RuleFor(x => x)
            .MustAsync(async (command, cancellationToken) =>
            {
                var existingPermission = await _userPermissionRepository.GetByUserAndPermissionAsync(
                    command.UserId, command.PermissionId, cancellationToken);
                return existingPermission != null;
            })
            .WithMessage("User does not have this permission assigned");
    }
}