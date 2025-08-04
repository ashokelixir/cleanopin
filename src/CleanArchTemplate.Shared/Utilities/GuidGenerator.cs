namespace CleanArchTemplate.Shared.Utilities;

/// <summary>
/// Utility class for generating GUIDs
/// </summary>
public static class GuidGenerator
{
    /// <summary>
    /// Generates a new GUID
    /// </summary>
    /// <returns>A new GUID</returns>
    public static Guid NewGuid() => Guid.NewGuid();

    /// <summary>
    /// Generates a new GUID as a string
    /// </summary>
    /// <returns>A new GUID as a string</returns>
    public static string NewGuidString() => Guid.NewGuid().ToString();

    /// <summary>
    /// Generates a new GUID as a string without hyphens
    /// </summary>
    /// <returns>A new GUID as a string without hyphens</returns>
    public static string NewGuidStringWithoutHyphens() => Guid.NewGuid().ToString("N");

    /// <summary>
    /// Generates a new GUID as an uppercase string
    /// </summary>
    /// <returns>A new GUID as an uppercase string</returns>
    public static string NewGuidStringUpper() => Guid.NewGuid().ToString().ToUpper();

    /// <summary>
    /// Generates a new GUID as a lowercase string
    /// </summary>
    /// <returns>A new GUID as a lowercase string</returns>
    public static string NewGuidStringLower() => Guid.NewGuid().ToString().ToLower();

    /// <summary>
    /// Checks if a string is a valid GUID
    /// </summary>
    /// <param name="value">The string to check</param>
    /// <returns>True if the string is a valid GUID; otherwise, false</returns>
    public static bool IsValidGuid(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && Guid.TryParse(value, out _);
    }

    /// <summary>
    /// Tries to parse a string as a GUID
    /// </summary>
    /// <param name="value">The string to parse</param>
    /// <param name="guid">The parsed GUID</param>
    /// <returns>True if the string was successfully parsed; otherwise, false</returns>
    public static bool TryParseGuid(string? value, out Guid guid)
    {
        return Guid.TryParse(value, out guid);
    }
}