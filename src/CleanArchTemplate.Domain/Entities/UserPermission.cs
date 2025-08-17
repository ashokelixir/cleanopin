using CleanArchTemplate.Domain.Common;
using CleanArchTemplate.Domain.Enums;
using CleanArchTemplate.Domain.Events;

namespace CleanArchTemplate.Domain.Entities;

/// <summary>
/// Represents a user-specific permission override that takes precedence over role-based permissions
/// </summary>
public class UserPermission : BaseAuditableEntity
{
    /// <summary>
    /// The user ID this permission override applies to
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// The permission ID this override applies to
    /// </summary>
    public Guid PermissionId { get; private set; }

    /// <summary>
    /// The permission state (Grant or Deny)
    /// </summary>
    public PermissionState State { get; private set; }

    /// <summary>
    /// Optional reason for the permission override
    /// </summary>
    public string? Reason { get; private set; }

    /// <summary>
    /// Optional expiration date for the permission override
    /// </summary>
    public DateTime? ExpiresAt { get; private set; }

    /// <summary>
    /// Navigation property to the user
    /// </summary>
    public User User { get; private set; } = null!;

    /// <summary>
    /// Navigation property to the permission
    /// </summary>
    public Permission Permission { get; private set; } = null!;

    // Private constructor for EF Core
    private UserPermission() { }

    /// <summary>
    /// Creates a new user permission override
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="permissionId">The permission ID</param>
    /// <param name="state">The permission state (Grant or Deny)</param>
    /// <param name="reason">Optional reason for the override</param>
    /// <param name="expiresAt">Optional expiration date</param>
    public UserPermission(Guid userId, Guid permissionId, PermissionState state, string? reason = null, DateTime? expiresAt = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));

        if (permissionId == Guid.Empty)
            throw new ArgumentException("Permission ID cannot be empty.", nameof(permissionId));

        if (!Enum.IsDefined(typeof(PermissionState), state))
            throw new ArgumentException("Invalid permission state.", nameof(state));

        if (expiresAt.HasValue && expiresAt.Value <= DateTime.UtcNow)
            throw new ArgumentException("Expiration date must be in the future.", nameof(expiresAt));

        if (!string.IsNullOrEmpty(reason) && reason.Length > 500)
            throw new ArgumentException("Reason cannot exceed 500 characters.", nameof(reason));

        UserId = userId;
        PermissionId = permissionId;
        State = state;
        Reason = reason?.Trim();
        ExpiresAt = expiresAt;

        AddDomainEvent(new UserPermissionCreatedEvent(Id, UserId, PermissionId, State, Reason, ExpiresAt));
    }

    /// <summary>
    /// Creates a new user permission override
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="permissionId">The permission ID</param>
    /// <param name="state">The permission state (Grant or Deny)</param>
    /// <param name="reason">Optional reason for the override</param>
    /// <param name="expiresAt">Optional expiration date</param>
    /// <returns>A new UserPermission instance</returns>
    public static UserPermission Create(Guid userId, Guid permissionId, PermissionState state, string? reason = null, DateTime? expiresAt = null)
    {
        return new UserPermission(userId, permissionId, state, reason, expiresAt);
    }

    /// <summary>
    /// Updates the permission state
    /// </summary>
    /// <param name="state">The new permission state</param>
    /// <param name="reason">Optional reason for the change</param>
    public void UpdateState(PermissionState state, string? reason = null)
    {
        if (!Enum.IsDefined(typeof(PermissionState), state))
            throw new ArgumentException("Invalid permission state.", nameof(state));

        if (!string.IsNullOrEmpty(reason) && reason.Length > 500)
            throw new ArgumentException("Reason cannot exceed 500 characters.", nameof(reason));

        var oldState = State;
        var oldReason = Reason;

        State = state;
        Reason = reason?.Trim();

        AddDomainEvent(new UserPermissionUpdatedEvent(Id, UserId, PermissionId, oldState, State, oldReason, Reason));
    }

    /// <summary>
    /// Sets the expiration date for the permission override
    /// </summary>
    /// <param name="expiresAt">The expiration date (null for no expiration)</param>
    public void SetExpiration(DateTime? expiresAt)
    {
        if (expiresAt.HasValue && expiresAt.Value <= DateTime.UtcNow)
            throw new ArgumentException("Expiration date must be in the future.", nameof(expiresAt));

        var oldExpiresAt = ExpiresAt;
        ExpiresAt = expiresAt;

        AddDomainEvent(new UserPermissionExpirationChangedEvent(Id, UserId, PermissionId, oldExpiresAt, ExpiresAt));
    }

    /// <summary>
    /// Checks if the permission override is currently active (not expired)
    /// </summary>
    /// <returns>True if the permission is active</returns>
    public bool IsActive()
    {
        return !ExpiresAt.HasValue || ExpiresAt.Value > DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the permission override is expired
    /// </summary>
    /// <returns>True if the permission is expired</returns>
    public bool IsExpired()
    {
        return ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;
    }

    /// <summary>
    /// Extends the expiration date by the specified duration
    /// </summary>
    /// <param name="duration">The duration to extend</param>
    public void ExtendExpiration(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
            throw new ArgumentException("Duration must be positive.", nameof(duration));

        var newExpiresAt = (ExpiresAt ?? DateTime.UtcNow).Add(duration);
        SetExpiration(newExpiresAt);
    }
}