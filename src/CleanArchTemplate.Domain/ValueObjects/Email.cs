using System.Text.RegularExpressions;

namespace CleanArchTemplate.Domain.ValueObjects;

/// <summary>
/// Value object representing an email address
/// </summary>
public sealed class Email : IEquatable<Email>
{
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// The email address value
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Initializes a new instance of the Email class
    /// </summary>
    /// <param name="value">The email address value</param>
    /// <exception cref="ArgumentException">Thrown when the email format is invalid</exception>
    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email cannot be null or empty.", nameof(value));

        if (value.Length > 254)
            throw new ArgumentException("Email cannot exceed 254 characters.", nameof(value));

        if (!EmailRegex.IsMatch(value))
            throw new ArgumentException("Invalid email format.", nameof(value));

        Value = value.ToLowerInvariant();
    }

    /// <summary>
    /// Creates an Email instance from a string value
    /// </summary>
    /// <param name="value">The email address value</param>
    /// <returns>An Email instance</returns>
    public static Email Create(string value) => new(value);

    /// <summary>
    /// Tries to create an Email instance from a string value
    /// </summary>
    /// <param name="value">The email address value</param>
    /// <param name="email">The created Email instance if successful</param>
    /// <returns>True if the email was created successfully, false otherwise</returns>
    public static bool TryCreate(string value, out Email? email)
    {
        try
        {
            email = new Email(value);
            return true;
        }
        catch
        {
            email = null;
            return false;
        }
    }

    /// <summary>
    /// Gets the domain part of the email address
    /// </summary>
    /// <returns>The domain part of the email</returns>
    public string GetDomain()
    {
        var atIndex = Value.IndexOf('@');
        return atIndex >= 0 ? Value.Substring(atIndex + 1) : string.Empty;
    }

    /// <summary>
    /// Gets the local part of the email address (before @)
    /// </summary>
    /// <returns>The local part of the email</returns>
    public string GetLocalPart()
    {
        var atIndex = Value.IndexOf('@');
        return atIndex >= 0 ? Value.Substring(0, atIndex) : Value;
    }

    public bool Equals(Email? other)
    {
        return other is not null && Value.Equals(other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj)
    {
        return obj is Email other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode(StringComparison.OrdinalIgnoreCase);
    }

    public override string ToString()
    {
        return Value;
    }

    public static bool operator ==(Email? left, Email? right)
    {
        return left?.Equals(right) ?? right is null;
    }

    public static bool operator !=(Email? left, Email? right)
    {
        return !(left == right);
    }

    public static implicit operator string(Email email)
    {
        return email.Value;
    }
}