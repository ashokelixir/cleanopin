using CleanArchTemplate.Application.Common.Models;

namespace CleanArchTemplate.Application.Common.Messages;

/// <summary>
/// Message published when a user is created
/// </summary>
public class UserCreatedMessage : BaseMessage
{
    /// <summary>
    /// The ID of the created user
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The email of the created user
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The first name of the created user
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// The last name of the created user
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Whether the user's email is verified
    /// </summary>
    public bool IsEmailVerified { get; set; }

    /// <summary>
    /// The roles assigned to the user
    /// </summary>
    public List<string> Roles { get; set; } = new();
}

/// <summary>
/// Message published when a user is updated
/// </summary>
public class UserUpdatedMessage : BaseMessage
{
    /// <summary>
    /// The ID of the updated user
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The email of the updated user
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The first name of the updated user
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// The last name of the updated user
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Whether the user is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// The fields that were updated
    /// </summary>
    public List<string> UpdatedFields { get; set; } = new();
}

/// <summary>
/// Message published when a user is deleted
/// </summary>
public class UserDeletedMessage : BaseMessage
{
    /// <summary>
    /// The ID of the deleted user
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The email of the deleted user
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The first name of the deleted user
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// The last name of the deleted user
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// When the user was deleted
    /// </summary>
    public DateTime DeletedAt { get; set; }

    /// <summary>
    /// The reason for deletion
    /// </summary>
    public string? DeletionReason { get; set; }
}

/// <summary>
/// Message published when a user's email is verified
/// </summary>
public class UserEmailVerifiedMessage : BaseMessage
{
    /// <summary>
    /// The ID of the user whose email was verified
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The email that was verified
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// When the email was verified
    /// </summary>
    public DateTime VerifiedAt { get; set; }
}

/// <summary>
/// Message published when a user's password is changed
/// </summary>
public class UserPasswordChangedMessage : BaseMessage
{
    /// <summary>
    /// The ID of the user whose password was changed
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The email of the user
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// When the password was changed
    /// </summary>
    public DateTime ChangedAt { get; set; }
}

/// <summary>
/// Message published when a user's profile is updated
/// </summary>
public class UserProfileUpdatedMessage : BaseMessage
{
    /// <summary>
    /// The ID of the user whose profile was updated
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The old full name
    /// </summary>
    public string OldFullName { get; set; } = string.Empty;

    /// <summary>
    /// The new full name
    /// </summary>
    public string NewFullName { get; set; } = string.Empty;

    /// <summary>
    /// When the profile was updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Message published when a user's email is updated
/// </summary>
public class UserEmailUpdatedMessage : BaseMessage
{
    /// <summary>
    /// The ID of the user whose email was updated
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The old email address
    /// </summary>
    public string OldEmail { get; set; } = string.Empty;

    /// <summary>
    /// The new email address
    /// </summary>
    public string NewEmail { get; set; } = string.Empty;

    /// <summary>
    /// When the email was updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Message published when a user is activated
/// </summary>
public class UserActivatedMessage : BaseMessage
{
    /// <summary>
    /// The ID of the user who was activated
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The email of the user
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// When the user was activated
    /// </summary>
    public DateTime ActivatedAt { get; set; }
}

/// <summary>
/// Message published when a user is deactivated
/// </summary>
public class UserDeactivatedMessage : BaseMessage
{
    /// <summary>
    /// The ID of the user who was deactivated
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The email of the user
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// When the user was deactivated
    /// </summary>
    public DateTime DeactivatedAt { get; set; }
}