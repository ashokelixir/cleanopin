using CleanArchTemplate.Domain.Common;

namespace CleanArchTemplate.Domain.Entities;

/// <summary>
/// Represents a refresh token for JWT authentication
/// </summary>
public class RefreshToken : BaseAuditableEntity
{
    /// <summary>
    /// The user ID this token belongs to
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// The token value
    /// </summary>
    public string Token { get; private set; } = string.Empty;

    /// <summary>
    /// The date and time when the token expires
    /// </summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>
    /// Indicates whether the token has been revoked
    /// </summary>
    public bool IsRevoked { get; private set; }

    /// <summary>
    /// The date and time when the token was revoked
    /// </summary>
    public DateTime? RevokedAt { get; private set; }

    /// <summary>
    /// The reason for token revocation
    /// </summary>
    public string? RevocationReason { get; private set; }

    /// <summary>
    /// The IP address from which the token was created
    /// </summary>
    public string? CreatedByIp { get; private set; }

    /// <summary>
    /// The IP address from which the token was revoked
    /// </summary>
    public string? RevokedByIp { get; private set; }

    /// <summary>
    /// Navigation property to the user
    /// </summary>
    public User User { get; private set; } = null!;

    /// <summary>
    /// Indicates whether the token is active (not expired and not revoked)
    /// </summary>
    public bool IsActive => !IsRevoked && ExpiresAt > DateTime.UtcNow;

    /// <summary>
    /// Indicates whether the token has expired
    /// </summary>
    public bool IsExpired => ExpiresAt <= DateTime.UtcNow;

    // Private constructor for EF Core
    private RefreshToken() { }

    /// <summary>
    /// Creates a new refresh token
    /// </summary>
    /// <param name="token">The token value</param>
    /// <param name="userId">The user ID</param>
    /// <param name="expiresAt">The expiration date</param>
    /// <param name="createdByIp">The IP address from which the token was created</param>
    /// <returns>A new refresh token instance</returns>
    public static RefreshToken Create(string token, Guid userId, DateTime expiresAt, string? createdByIp = null)
    {
        return new RefreshToken(userId, token, expiresAt, createdByIp);
    }

    /// <summary>
    /// Creates a new refresh token
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="token">The token value</param>
    /// <param name="expiresAt">The expiration date</param>
    /// <param name="createdByIp">The IP address from which the token was created</param>
    private RefreshToken(Guid userId, string token, DateTime expiresAt, string? createdByIp = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));

        Token = token?.Trim() ?? throw new ArgumentNullException(nameof(token));

        if (string.IsNullOrWhiteSpace(Token))
            throw new ArgumentException("Token cannot be empty.", nameof(token));

        if (expiresAt <= DateTime.UtcNow)
            throw new ArgumentException("Expiration date must be in the future.", nameof(expiresAt));

        UserId = userId;
        ExpiresAt = expiresAt;
        CreatedByIp = createdByIp?.Trim();
    }

    /// <summary>
    /// Revokes the refresh token
    /// </summary>
    /// <param name="reason">The reason for revocation</param>
    /// <param name="revokedByIp">The IP address from which the token was revoked</param>
    public void Revoke(string? reason = null, string? revokedByIp = null)
    {
        if (IsRevoked)
            return;

        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
        RevocationReason = reason?.Trim();
        RevokedByIp = revokedByIp?.Trim();
    }

    /// <summary>
    /// Checks if the token can be used for refresh
    /// </summary>
    /// <returns>True if the token is valid for refresh, false otherwise</returns>
    public bool CanBeUsedForRefresh()
    {
        return IsActive;
    }
}