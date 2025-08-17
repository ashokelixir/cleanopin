using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.ValueObjects;

namespace CleanArchTemplate.Domain.Interfaces;

/// <summary>
/// Domain repository interface for User entity
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Gets a user by ID
    /// </summary>
    /// <param name="id">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The user if found, null otherwise</returns>
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all users
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>All users</returns>
    Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by email
    /// </summary>
    /// <param name="email">The user's email</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The user if found, null otherwise</returns>
    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by email verification token
    /// </summary>
    /// <param name="token">The email verification token</param>
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
    /// Gets a user with their roles by ID
    /// </summary>
    /// <param name="id">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The user with roles if found, null otherwise</returns>
    Task<User?> GetUserWithRolesByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user with their roles and permissions
    /// </summary>
    /// <param name="id">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The user with roles and permissions if found, null otherwise</returns>
    Task<User?> GetUserWithRolesAndPermissionsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user with their user-specific permission overrides
    /// </summary>
    /// <param name="id">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The user with permission overrides if found, null otherwise</returns>
    Task<User?> GetUserWithPermissionOverridesAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all users with their roles
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Users with their roles</returns>
    Task<IEnumerable<User>> GetUsersWithRolesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all users with a specific role
    /// </summary>
    /// <param name="roleId">The role ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Users with the specified role</returns>
    Task<IEnumerable<User>> GetUsersByRoleAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an email is already in use
    /// </summary>
    /// <param name="email">The email to check</param>
    /// <param name="excludeUserId">User ID to exclude from the check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if email exists, false otherwise</returns>
    Task<bool> IsEmailExistsAsync(Email email, Guid? excludeUserId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new user
    /// </summary>
    /// <param name="user">The user to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task AddAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing user
    /// </summary>
    /// <param name="user">The user to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a user
    /// </summary>
    /// <param name="user">The user to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task RemoveAsync(User user, CancellationToken cancellationToken = default);
}