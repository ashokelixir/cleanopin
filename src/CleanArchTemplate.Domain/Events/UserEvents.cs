using CleanArchTemplate.Domain.Common;

namespace CleanArchTemplate.Domain.Events;

/// <summary>
/// Domain event raised when a user is created
/// </summary>
public sealed class UserCreatedEvent : BaseDomainEvent
{
    public Guid UserId { get; }
    public string Email { get; }
    public string FullName { get; }

    public UserCreatedEvent(Guid userId, string email, string fullName)
    {
        UserId = userId;
        Email = email;
        FullName = fullName;
    }
}

/// <summary>
/// Domain event raised when a user's profile is updated
/// </summary>
public sealed class UserProfileUpdatedEvent : BaseDomainEvent
{
    public Guid UserId { get; }
    public string OldFullName { get; }
    public string NewFullName { get; }

    public UserProfileUpdatedEvent(Guid userId, string oldFullName, string newFullName)
    {
        UserId = userId;
        OldFullName = oldFullName;
        NewFullName = newFullName;
    }
}

/// <summary>
/// Domain event raised when a user's email is updated
/// </summary>
public sealed class UserEmailUpdatedEvent : BaseDomainEvent
{
    public Guid UserId { get; }
    public string OldEmail { get; }
    public string NewEmail { get; }

    public UserEmailUpdatedEvent(Guid userId, string oldEmail, string newEmail)
    {
        UserId = userId;
        OldEmail = oldEmail;
        NewEmail = newEmail;
    }
}

/// <summary>
/// Domain event raised when a user's password is updated
/// </summary>
public sealed class UserPasswordUpdatedEvent : BaseDomainEvent
{
    public Guid UserId { get; }

    public UserPasswordUpdatedEvent(Guid userId)
    {
        UserId = userId;
    }
}

/// <summary>
/// Domain event raised when a user's email is verified
/// </summary>
public sealed class UserEmailVerifiedEvent : BaseDomainEvent
{
    public Guid UserId { get; }
    public string Email { get; }

    public UserEmailVerifiedEvent(Guid userId, string email)
    {
        UserId = userId;
        Email = email;
    }
}

/// <summary>
/// Domain event raised when a password reset is requested
/// </summary>
public sealed class UserPasswordResetRequestedEvent : BaseDomainEvent
{
    public Guid UserId { get; }
    public string Email { get; }

    public UserPasswordResetRequestedEvent(Guid userId, string email)
    {
        UserId = userId;
        Email = email;
    }
}

/// <summary>
/// Domain event raised when a user logs in
/// </summary>
public sealed class UserLoggedInEvent : BaseDomainEvent
{
    public Guid UserId { get; }
    public string Email { get; }

    public UserLoggedInEvent(Guid userId, string email)
    {
        UserId = userId;
        Email = email;
    }
}

/// <summary>
/// Domain event raised when a user is activated
/// </summary>
public sealed class UserActivatedEvent : BaseDomainEvent
{
    public Guid UserId { get; }
    public string Email { get; }

    public UserActivatedEvent(Guid userId, string email)
    {
        UserId = userId;
        Email = email;
    }
}

/// <summary>
/// Domain event raised when a user is deactivated
/// </summary>
public sealed class UserDeactivatedEvent : BaseDomainEvent
{
    public Guid UserId { get; }
    public string Email { get; }

    public UserDeactivatedEvent(Guid userId, string email)
    {
        UserId = userId;
        Email = email;
    }
}

/// <summary>
/// Domain event raised when a role is assigned to a user
/// </summary>
public sealed class UserRoleAssignedEvent : BaseDomainEvent
{
    public Guid UserId { get; }
    public Guid RoleId { get; }
    public string RoleName { get; }

    public UserRoleAssignedEvent(Guid userId, Guid roleId, string roleName)
    {
        UserId = userId;
        RoleId = roleId;
        RoleName = roleName;
    }
}

/// <summary>
/// Domain event raised when a role is removed from a user
/// </summary>
public sealed class UserRoleRemovedEvent : BaseDomainEvent
{
    public Guid UserId { get; }
    public Guid RoleId { get; }

    public UserRoleRemovedEvent(Guid userId, Guid roleId)
    {
        UserId = userId;
        RoleId = roleId;
    }
}