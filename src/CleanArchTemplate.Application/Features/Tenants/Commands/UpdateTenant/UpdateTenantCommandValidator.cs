using FluentValidation;

namespace CleanArchTemplate.Application.Features.Tenants.Commands.UpdateTenant;

/// <summary>
/// Validator for UpdateTenantCommand
/// </summary>
public class UpdateTenantCommandValidator : AbstractValidator<UpdateTenantCommand>
{
    public UpdateTenantCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("Tenant ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Tenant name is required.")
            .MaximumLength(200)
            .WithMessage("Tenant name cannot exceed 200 characters.");
    }
}