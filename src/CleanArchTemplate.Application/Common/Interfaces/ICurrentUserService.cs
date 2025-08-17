namespace CleanArchTemplate.Application.Common.Interfaces;

/// <summary>
/// Service interface for accessing current user context
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the current user's identifier
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// Gets the current user's email
    /// </summary>
    string? UserEmail { get; }

    /// <summary>
    /// Gets the current user's name
    /// </summary>
    string? UserName { get; }

    /// <summary>
    /// Checks if the current user is authenticated
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Gets the current user's identifier for audit purposes
    /// </summary>
    /// <returns>User identifier or "system" if not authenticated</returns>
    string GetAuditIdentifier();
}