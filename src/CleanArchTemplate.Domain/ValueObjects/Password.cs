using System.Text.RegularExpressions;

namespace CleanArchTemplate.Domain.ValueObjects;

/// <summary>
/// Value object representing a password with validation rules
/// </summary>
public sealed class Password : IEquatable<Password>
{
    private static readonly Regex PasswordRegex = new(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]",
        RegexOptions.Compiled);

    /// <summary>
    /// The password value (should be hashed in practice)
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Minimum password length
    /// </summary>
    public const int MinLength = 8;

    /// <summary>
    /// Maximum password length
    /// </summary>
    public const int MaxLength = 128;

    /// <summary>
    /// Initializes a new instance of the Password class
    /// </summary>
    /// <param name="value">The password value</param>
    /// <exception cref="ArgumentException">Thrown when the password doesn't meet requirements</exception>
    public Password(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Password cannot be null or empty.", nameof(value));

        if (value.Length < MinLength)
            throw new ArgumentException($"Password must be at least {MinLength} characters long.", nameof(value));

        if (value.Length > MaxLength)
            throw new ArgumentException($"Password cannot exceed {MaxLength} characters.", nameof(value));

        if (!PasswordRegex.IsMatch(value))
            throw new ArgumentException(
                "Password must contain at least one lowercase letter, one uppercase letter, one digit, and one special character.",
                nameof(value));

        Value = value;
    }

    /// <summary>
    /// Creates a Password instance from a string value
    /// </summary>
    /// <param name="value">The password value</param>
    /// <returns>A Password instance</returns>
    public static Password Create(string value) => new(value);

    /// <summary>
    /// Tries to create a Password instance from a string value
    /// </summary>
    /// <param name="value">The password value</param>
    /// <param name="password">The created Password instance if successful</param>
    /// <returns>True if the password was created successfully, false otherwise</returns>
    public static bool TryCreate(string value, out Password? password)
    {
        try
        {
            password = new Password(value);
            return true;
        }
        catch
        {
            password = null;
            return false;
        }
    }

    /// <summary>
    /// Validates a password against the requirements
    /// </summary>
    /// <param name="value">The password to validate</param>
    /// <returns>A list of validation errors, empty if valid</returns>
    public static List<string> Validate(string value)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add("Password cannot be null or empty.");
            return errors;
        }

        if (value.Length < MinLength)
            errors.Add($"Password must be at least {MinLength} characters long.");

        if (value.Length > MaxLength)
            errors.Add($"Password cannot exceed {MaxLength} characters.");

        if (!value.Any(char.IsLower))
            errors.Add("Password must contain at least one lowercase letter.");

        if (!value.Any(char.IsUpper))
            errors.Add("Password must contain at least one uppercase letter.");

        if (!value.Any(char.IsDigit))
            errors.Add("Password must contain at least one digit.");

        if (!value.Any(c => "@$!%*?&".Contains(c)))
            errors.Add("Password must contain at least one special character (@$!%*?&).");

        return errors;
    }

    /// <summary>
    /// Checks if the password meets all requirements
    /// </summary>
    /// <param name="value">The password to check</param>
    /// <returns>True if the password is valid, false otherwise</returns>
    public static bool IsValid(string value)
    {
        return Validate(value).Count == 0;
    }

    public bool Equals(Password? other)
    {
        return other is not null && Value.Equals(other.Value, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        return obj is Password other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode(StringComparison.Ordinal);
    }

    public override string ToString()
    {
        return new string('*', Math.Min(Value.Length, 8)); // Mask the password for security
    }

    public static bool operator ==(Password? left, Password? right)
    {
        return left?.Equals(right) ?? right is null;
    }

    public static bool operator !=(Password? left, Password? right)
    {
        return !(left == right);
    }
}