using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace CleanArchTemplate.Shared.Utilities;

/// <summary>
/// Utility class for password operations
/// </summary>
public static class PasswordHelper
{
    private static readonly Regex PasswordRegex = new(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]",
        RegexOptions.Compiled);

    /// <summary>
    /// Validates password strength
    /// </summary>
    /// <param name="password">The password to validate</param>
    /// <param name="minLength">Minimum password length</param>
    /// <returns>True if password meets requirements; otherwise, false</returns>
    public static bool IsValidPassword(string password, int minLength = 8)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < minLength)
            return false;

        return PasswordRegex.IsMatch(password);
    }

    /// <summary>
    /// Generates a random password
    /// </summary>
    /// <param name="length">The password length</param>
    /// <returns>A random password</returns>
    public static string GenerateRandomPassword(int length = 12)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789@$!%*?&";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    /// <summary>
    /// Hashes a password using BCrypt
    /// </summary>
    /// <param name="password">The password to hash</param>
    /// <returns>The hashed password</returns>
    public static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    /// <summary>
    /// Verifies a password against a hash
    /// </summary>
    /// <param name="password">The password to verify</param>
    /// <param name="hash">The hash to verify against</param>
    /// <returns>True if the password matches the hash; otherwise, false</returns>
    public static bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}