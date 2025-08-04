namespace CleanArchTemplate.Application.Common.Interfaces;

/// <summary>
/// Service interface for password operations
/// </summary>
public interface IPasswordService
{
    /// <summary>
    /// Hashes a password using a secure hashing algorithm
    /// </summary>
    /// <param name="password">The plain text password</param>
    /// <returns>The hashed password</returns>
    string HashPassword(string password);

    /// <summary>
    /// Verifies a password against its hash
    /// </summary>
    /// <param name="password">The plain text password</param>
    /// <param name="hash">The password hash</param>
    /// <returns>True if the password matches the hash, false otherwise</returns>
    bool VerifyPassword(string password, string hash);
}