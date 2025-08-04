using System.ComponentModel.DataAnnotations;
using CleanArchTemplate.Shared.Extensions;

namespace CleanArchTemplate.Shared.Utilities;

/// <summary>
/// Helper class for common validation operations
/// </summary>
public static class ValidationHelper
{
    /// <summary>
    /// Validates an object using data annotations
    /// </summary>
    /// <param name="obj">The object to validate</param>
    /// <returns>A list of validation errors</returns>
    public static IList<string> ValidateObject(object obj)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(obj);
        
        Validator.TryValidateObject(obj, validationContext, validationResults, true);
        
        return validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToList();
    }

    /// <summary>
    /// Validates an object and returns whether it's valid
    /// </summary>
    /// <param name="obj">The object to validate</param>
    /// <param name="errors">The validation errors</param>
    /// <returns>True if the object is valid; otherwise, false</returns>
    public static bool TryValidateObject(object obj, out IList<string> errors)
    {
        errors = ValidateObject(obj);
        return !errors.Any();
    }

    /// <summary>
    /// Validates that a string is not null, empty, or whitespace
    /// </summary>
    /// <param name="value">The string to validate</param>
    /// <param name="fieldName">The name of the field</param>
    /// <returns>The validation error message, or null if valid</returns>
    public static string? ValidateRequired(string? value, string fieldName)
    {
        return value.IsNullOrWhiteSpace() ? $"{fieldName} is required." : null;
    }

    /// <summary>
    /// Validates that a string meets minimum and maximum length requirements
    /// </summary>
    /// <param name="value">The string to validate</param>
    /// <param name="fieldName">The name of the field</param>
    /// <param name="minLength">The minimum length</param>
    /// <param name="maxLength">The maximum length</param>
    /// <returns>The validation error message, or null if valid</returns>
    public static string? ValidateLength(string? value, string fieldName, int minLength, int maxLength)
    {
        if (value.IsNullOrEmpty())
            return null; // Use ValidateRequired for null/empty validation

        var length = value!.Length;
        
        if (length < minLength)
            return $"{fieldName} must be at least {minLength} characters long.";
        
        if (length > maxLength)
            return $"{fieldName} must not exceed {maxLength} characters.";
            
        return null;
    }

    /// <summary>
    /// Validates that a numeric value is within a specified range
    /// </summary>
    /// <param name="value">The value to validate</param>
    /// <param name="fieldName">The name of the field</param>
    /// <param name="min">The minimum value</param>
    /// <param name="max">The maximum value</param>
    /// <returns>The validation error message, or null if valid</returns>
    public static string? ValidateRange(int value, string fieldName, int min, int max)
    {
        return value switch
        {
            var v when v < min => $"{fieldName} must be at least {min}.",
            var v when v > max => $"{fieldName} must not exceed {max}.",
            _ => null
        };
    }

    /// <summary>
    /// Validates that a decimal value is within a specified range
    /// </summary>
    /// <param name="value">The value to validate</param>
    /// <param name="fieldName">The name of the field</param>
    /// <param name="min">The minimum value</param>
    /// <param name="max">The maximum value</param>
    /// <returns>The validation error message, or null if valid</returns>
    public static string? ValidateRange(decimal value, string fieldName, decimal min, decimal max)
    {
        return value switch
        {
            var v when v < min => $"{fieldName} must be at least {min}.",
            var v when v > max => $"{fieldName} must not exceed {max}.",
            _ => null
        };
    }

    /// <summary>
    /// Validates that a Guid is not empty
    /// </summary>
    /// <param name="value">The Guid to validate</param>
    /// <param name="fieldName">The name of the field</param>
    /// <returns>The validation error message, or null if valid</returns>
    public static string? ValidateGuid(Guid value, string fieldName)
    {
        return value == Guid.Empty ? $"{fieldName} must not be empty." : null;
    }

    /// <summary>
    /// Validates that an email address is valid
    /// </summary>
    /// <param name="email">The email to validate</param>
    /// <param name="fieldName">The name of the field</param>
    /// <returns>The validation error message, or null if valid</returns>
    public static string? ValidateEmail(string? email, string fieldName)
    {
        if (email.IsNullOrWhiteSpace())
            return null; // Use ValidateRequired for null/empty validation

        return email!.IsValidEmail() ? null : $"{fieldName} must be a valid email address.";
    }

    /// <summary>
    /// Validates that a collection is not null or empty
    /// </summary>
    /// <param name="collection">The collection to validate</param>
    /// <param name="fieldName">The name of the field</param>
    /// <returns>The validation error message, or null if valid</returns>
    public static string? ValidateCollection<T>(IEnumerable<T>? collection, string fieldName)
    {
        return collection.IsNullOrEmpty() ? $"{fieldName} must contain at least one item." : null;
    }
}