using CleanArchTemplate.Domain.Entities;

namespace CleanArchTemplate.Application.Common.Interfaces;

/// <summary>
/// Service interface for JWT token operations
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates an access token for the specified user
    /// </summary>
    /// <param name="user">The user to generate the token for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The access token</returns>
    Task<string> GenerateAccessTokenAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a refresh token
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The refresh token</returns>
    Task<string> GenerateRefreshTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates an access token
    /// </summary>
    /// <param name="token">The token to validate</param>
    /// <returns>True if the token is valid, false otherwise</returns>
    bool ValidateAccessToken(string token);

    /// <summary>
    /// Gets the user ID from an access token
    /// </summary>
    /// <param name="token">The access token</param>
    /// <returns>The user ID if the token is valid, null otherwise</returns>
    Guid? GetUserIdFromToken(string token);
}