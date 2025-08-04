using System.ComponentModel.DataAnnotations;
using CleanArchTemplate.Shared.Extensions;

namespace CleanArchTemplate.Shared.Validation;

/// <summary>
/// Validates that a date is in the future
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class FutureDateAttribute : ValidationAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FutureDateAttribute"/> class
    /// </summary>
    public FutureDateAttribute() : base("The {0} field must be a future date.")
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

        return value switch
        {
            DateTime dateTime => dateTime.IsInFuture(),
            DateTimeOffset dateTimeOffset => dateTimeOffset.DateTime.IsInFuture(),
            _ => false
        };
    }
}