using System.ComponentModel.DataAnnotations;
using CleanArchTemplate.Shared.Extensions;

namespace CleanArchTemplate.Shared.Validation;

/// <summary>
/// Validates that a date is in the past
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class PastDateAttribute : ValidationAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PastDateAttribute"/> class
    /// </summary>
    public PastDateAttribute() : base("The {0} field must be a past date.")
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
            DateTime dateTime => dateTime.IsInPast(),
            DateTimeOffset dateTimeOffset => dateTimeOffset.DateTime.IsInPast(),
            _ => false
        };
    }
}