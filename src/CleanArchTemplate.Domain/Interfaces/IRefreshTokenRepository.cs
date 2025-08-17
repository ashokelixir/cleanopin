using CleanArchTemplate.Domain.Entities;

namespace CleanArchTemplate.Domain.Interfaces;

/// <summary>
/// Domain repository interface for RefreshToken entity
/// </summary>
public interface IRefreshTokenRepository
{
    /// <summary>
    /// Gets a refresh token by ID
    /// </summary>
    /// <param name="id">The refresh token ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The refresh token if found, null otherwise</returns>
    Task<RefreshToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a refresh token by token value
    /// </summary>
    /// <param name="token">The token value</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The refresh token if found, null otherwise</returns>
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active refresh tokens for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Active refresh tokens for the user</returns>
    Task<IEnumerable<RefreshToken>> GetActiveTokensByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new refresh token
    /// </summary>
    /// <param name="refreshToken">The refresh token to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing refresh token
    /// </summary>
    /// <param name="refreshToken">The refresh token to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a refresh token
    /// </summary>
    /// <param name="refreshToken">The refresh token to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task RemoveAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all refresh tokens for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task RevokeAllUserTokensAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes expired refresh tokens
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task RemoveExpiredTokensAsync(CancellationToken cancellationToken = default);
}