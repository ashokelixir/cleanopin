using FluentValidation;

namespace CleanArchTemplate.Application.Features.Tenants.Commands.CreateTenant;

/// <summary>
/// Validator for CreateTenantCommand
/// </summary>
public class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Tenant name is required.")
            .MaximumLength(200)
            .WithMessage("Tenant name cannot exceed 200 characters.");

        RuleFor(x => x.Identifier)
            .NotEmpty()
            .WithMessage("Tenant identifier is required.")
            .Length(2, 50)
            .WithMessage("Tenant identifier must be between 2 and 50 characters.")
            .Matches(@"^[a-z0-9][a-z0-9-]*[a-z0-9]$")
            .WithMessage("Tenant identifier can only contain lowercase letters, numbers, and hyphens, and must start and end with alphanumeric characters.")
            .Must(NotBeReservedIdentifier)
            .WithMessage("The specified identifier is reserved and cannot be used.");

        RuleFor(x => x.ConnectionString)
            .MaximumLength(1000)
            .WithMessage("Connection string cannot exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.ConnectionString));

        RuleFor(x => x.SubscriptionExpiresAt)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Subscription expiry date must be in the future.")
            .When(x => x.SubscriptionExpiresAt.HasValue);
    }

    /// <summary>
    /// Validates that the identifier is not reserved
    /// </summary>
    /// <param name="identifier">The identifier to validate</param>
    /// <returns>True if the identifier is not reserved</returns>
    private static bool NotBeReservedIdentifier(string identifier)
    {
        var reservedIdentifiers = new[] { "www", "api", "admin", "app", "mail", "ftp", "localhost", "test", "staging", "prod", "production" };
        return !reservedIdentifiers.Contains(identifier?.ToLowerInvariant());
    }
}