using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace CleanArchTemplate.Shared.Extensions;

/// <summary>
/// Extension methods for string operations
/// </summary>
public static class StringExtensions
{
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Checks if a string is null or empty
    /// </summary>
    /// <param name="value">The string to check</param>
    /// <returns>True if the string is null or empty; otherwise, false</returns>
    public static bool IsNullOrEmpty(this string? value) => string.IsNullOrEmpty(value);

    /// <summary>
    /// Checks if a string is null, empty, or whitespace
    /// </summary>
    /// <param name="value">The string to check</param>
    /// <returns>True if the string is null, empty, or whitespace; otherwise, false</returns>
    public static bool IsNullOrWhiteSpace(this string? value) => string.IsNullOrWhiteSpace(value);

    /// <summary>
    /// Checks if a string has a value (not null, empty, or whitespace)
    /// </summary>
    /// <param name="value">The string to check</param>
    /// <returns>True if the string has a value; otherwise, false</returns>
    public static bool HasValue(this string? value) => !string.IsNullOrWhiteSpace(value);

    /// <summary>
    /// Converts a string to title case
    /// </summary>
    /// <param name="value">The string to convert</param>
    /// <returns>The string in title case</returns>
    public static string ToTitleCase(this string value)
    {
        if (value.IsNullOrWhiteSpace())
            return value;

        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value.ToLower());
    }

    /// <summary>
    /// Truncates a string to a specified length
    /// </summary>
    /// <param name="value">The string to truncate</param>
    /// <param name="maxLength">The maximum length</param>
    /// <param name="suffix">The suffix to append if truncated</param>
    /// <returns>The truncated string</returns>
    public static string Truncate(this string value, int maxLength, string suffix = "...")
    {
        if (value.IsNullOrEmpty() || value.Length <= maxLength)
            return value;

        return value[..(maxLength - suffix.Length)] + suffix;
    }

    /// <summary>
    /// Validates if a string is a valid email address
    /// </summary>
    /// <param name="email">The email to validate</param>
    /// <returns>True if the email is valid; otherwise, false</returns>
    public static bool IsValidEmail(this string email)
    {
        if (email.IsNullOrWhiteSpace())
            return false;

        return EmailRegex.IsMatch(email);
    }

    /// <summary>
    /// Converts a string to a secure hash using SHA256
    /// </summary>
    /// <param name="value">The string to hash</param>
    /// <returns>The hashed string</returns>
    public static string ToSha256Hash(this string value)
    {
        if (value.IsNullOrEmpty())
            return value;

        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(value));
        return Convert.ToBase64String(hashedBytes);
    }

    /// <summary>
    /// Removes special characters from a string, keeping only alphanumeric characters and spaces
    /// </summary>
    /// <param name="value">The string to clean</param>
    /// <returns>The cleaned string</returns>
    public static string RemoveSpecialCharacters(this string value)
    {
        if (value.IsNullOrEmpty())
            return value;

        return Regex.Replace(value, @"[^a-zA-Z0-9\s]", string.Empty);
    }

    /// <summary>
    /// Converts a string to kebab-case
    /// </summary>
    /// <param name="value">The string to convert</param>
    /// <returns>The string in kebab-case</returns>
    public static string ToKebabCase(this string value)
    {
        if (value.IsNullOrWhiteSpace())
            return value;

        return Regex.Replace(value, @"([a-z])([A-Z])", "$1-$2")
            .Replace(" ", "-")
            .ToLower();
    }

    /// <summary>
    /// Converts a string to snake_case
    /// </summary>
    /// <param name="value">The string to convert</param>
    /// <returns>The string in snake_case</returns>
    public static string ToSnakeCase(this string value)
    {
        if (value.IsNullOrWhiteSpace())
            return value;

        return Regex.Replace(value, @"([a-z])([A-Z])", "$1_$2")
            .Replace(" ", "_")
            .ToLower();
    }
}