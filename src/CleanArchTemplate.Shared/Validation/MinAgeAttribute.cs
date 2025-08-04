using System.ComponentModel.DataAnnotations;
using CleanArchTemplate.Shared.Extensions;

namespace CleanArchTemplate.Shared.Validation;

/// <summary>
/// Validates that a date represents a minimum age
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class MinAgeAttribute : ValidationAttribute
{
    private readonly int _minAge;

    /// <summary>
    /// Initializes a new instance of the <see cref="MinAgeAttribute"/> class
    /// </summary>
    /// <param name="minAge">The minimum age required</param>
    public MinAgeAttribute(int minAge) : base($"Must be at least {minAge} years old.")
    {
        _minAge = minAge;
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

        if (value is not DateTime dateTime)
            return false;

        var age = dateTime.CalculateAge();
        return age >= _minAge;
    }

    /// <summary>
    /// Formats the error message
    /// </summary>
    /// <param name="name">The name of the field</param>
    /// <returns>The formatted error message</returns>
    public override string FormatErrorMessage(string name)
    {
        return $"The {name} field indicates an age less than {_minAge} years.";
    }
}