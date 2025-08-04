using System.ComponentModel.DataAnnotations;

namespace CleanArchTemplate.Shared.Validation;

/// <summary>
/// Validates that a Guid is not empty
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class NotEmptyGuidAttribute : ValidationAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotEmptyGuidAttribute"/> class
    /// </summary>
    public NotEmptyGuidAttribute() : base("The {0} field must not be empty.")
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

        if (value is Guid guid)
            return guid != Guid.Empty;

        if (value is Guid)
            return (Guid)value != Guid.Empty;

        return false;
    }
}