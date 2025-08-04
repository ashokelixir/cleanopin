using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.ValueObjects;

namespace CleanArchTemplate.Application.Common.Interfaces;

/// <summary>
/// Repository interface for User entity
/// </summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>
    /// Gets a user by email address
    /// </summary>
    /// <param name="email">The email address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The user if found, null otherwise</returns>
    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by email verification token
    /// </summary>
    /// <param name="token">The verification token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The user if found, null otherwise</returns>
    Task<User?> GetByEmailVerificationTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by password reset token
    /// </summary>
    /// <param name="token">The password reset token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The user if found, null otherwise</returns>
    Task<User?> GetByPasswordResetTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets users with their roles
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Users with their roles</returns>
    Task<IEnumerable<User>> GetUsersWithRolesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user with their roles by ID
    /// </summary>
    /// <param name="id">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The user with roles if found, null otherwise</returns>
    Task<User?> GetUserWithRolesByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an email address is already in use
    /// </summary>
    /// <param name="email">The email address to check</param>
    /// <param name="excludeUserId">User ID to exclude from the check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if email exists, false otherwise</returns>
    Task<bool> IsEmailExistsAsync(Email email, Guid? excludeUserId = null, CancellationToken cancellationToken = default);
}