using System.ComponentModel.DataAnnotations;
using CleanArchTemplate.Shared.Extensions;

namespace CleanArchTemplate.Shared.Validation;

/// <summary>
/// Validates that a string is a valid email address
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class EmailAttribute : ValidationAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EmailAttribute"/> class
    /// </summary>
    public EmailAttribute() : base("The {0} field must be a valid email address.")
    {
    }

    /// <summary>
    /// Validates the specified value
    /// </summary>
    /// <param name="value">The value to validate</param>
    /// <returns>True if the value is valid; otherwise, false</returns>
    public override bool IsValid(object? value)
    {
        if (value == null)
            return true; // Use [Required] for null validation

        if (value is not string email)
            return false;

        return email.IsValidEmail();
    }
}