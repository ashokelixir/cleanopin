using CleanArchTemplate.Domain.Common;
using CleanArchTemplate.Domain.Events;
using CleanArchTemplate.Domain.ValueObjects;

namespace CleanArchTemplate.Domain.Entities;

/// <summary>
/// Represents a user in the system
/// </summary>
public class User : BaseAuditableEntity
{
    private readonly List<UserRole> _userRoles = new();
    private readonly List<RefreshToken> _refreshTokens = new();

    /// <summary>
    /// The user's email address
    /// </summary>
    public Email Email { get; private set; }

    /// <summary>
    /// The user's first name
    /// </summary>
    public string FirstName { get; private set; } = string.Empty;

    /// <summary>
    /// The user's last name
    /// </summary>
    public string LastName { get; private set; } = string.Empty;

    /// <summary>
    /// The user's hashed password
    /// </summary>
    public string PasswordHash { get; private set; } = string.Empty;

    /// <summary>
    /// Indicates whether the user's email has been verified
    /// </summary>
    public bool IsEmailVerified { get; private set; }

    /// <summary>
    /// The date and time of the user's last login
    /// </summary>
    public DateTime? LastLoginAt { get; private set; }

    /// <summary>
    /// Indicates whether the user account is active
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Email verification token
    /// </summary>
    public string? EmailVerificationToken { get; private set; }

    /// <summary>
    /// Email verification token expiry date
    /// </summary>
    public DateTime? EmailVerificationTokenExpiry { get; private set; }

    /// <summary>
    /// Password reset token
    /// </summary>
    public string? PasswordResetToken { get; private set; }

    /// <summary>
    /// Password reset token expiry date
    /// </summary>
    public DateTime? PasswordResetTokenExpiry { get; private set; }

    /// <summary>
    /// Navigation property for user roles
    /// </summary>
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    /// <summary>
    /// Navigation property for refresh tokens
    /// </summary>
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    /// <summary>
    /// Gets the user's full name
    /// </summary>
    public string FullName => $"{FirstName} {LastName}".Trim();

    // Private constructor for EF Core
    private User() 
    {
        Email = null!; // Will be set by EF Core
    }

    /// <summary>
    /// Creates a new user
    /// </summary>
    /// <param name="email">The user's email address</param>
    /// <param name="firstName">The user's first name</param>
    /// <param name="lastName">The user's last name</param>
    /// <param name="passwordHash">The user's hashed password</param>
    /// <returns>A new user instance</returns>
    public static User Create(Email email, string firstName, string lastName, string passwordHash)
    {
        return new User(email, firstName, lastName, passwordHash);
    }

    /// <summary>
    /// Creates a new user
    /// </summary>
    /// <param name="email">The user's email address</param>
    /// <param name="firstName">The user's first name</param>
    /// <param name="lastName">The user's last name</param>
    /// <param name="passwordHash">The user's hashed password</param>
    private User(Email email, string firstName, string lastName, string passwordHash)
    {
        Email = email ?? throw new ArgumentNullException(nameof(email));
        FirstName = firstName?.Trim() ?? throw new ArgumentNullException(nameof(firstName));
        LastName = lastName?.Trim() ?? throw new ArgumentNullException(nameof(lastName));
        PasswordHash = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash));

        if (string.IsNullOrWhiteSpace(FirstName))
            throw new ArgumentException("First name cannot be empty.", nameof(firstName));

        if (string.IsNullOrWhiteSpace(LastName))
            throw new ArgumentException("Last name cannot be empty.", nameof(lastName));

        if (string.IsNullOrWhiteSpace(PasswordHash))
            throw new ArgumentException("Password hash cannot be empty.", nameof(passwordHash));

        GenerateEmailVerificationToken();
        AddDomainEvent(new UserCreatedEvent(Id, Email.Value, FullName));
    }

    /// <summary>
    /// Updates the user's profile information
    /// </summary>
    /// <param name="firstName">The new first name</param>
    /// <param name="lastName">The new last name</param>
    public void UpdateProfile(string firstName, string lastName)
    {
        var oldFullName = FullName;
        
        FirstName = firstName?.Trim() ?? throw new ArgumentNullException(nameof(firstName));
        LastName = lastName?.Trim() ?? throw new ArgumentNullException(nameof(lastName));

        if (string.IsNullOrWhiteSpace(FirstName))
            throw new ArgumentException("First name cannot be empty.", nameof(firstName));

        if (string.IsNullOrWhiteSpace(LastName))
            throw new ArgumentException("Last name cannot be empty.", nameof(lastName));

        AddDomainEvent(new UserProfileUpdatedEvent(Id, oldFullName, FullName));
    }

    /// <summary>
    /// Updates the user's email address
    /// </summary>
    /// <param name="newEmail">The new email address</param>
    public void UpdateEmail(Email newEmail)
    {
        var oldEmail = Email.Value;
        Email = newEmail ?? throw new ArgumentNullException(nameof(newEmail));
        IsEmailVerified = false;
        GenerateEmailVerificationToken();
        
        AddDomainEvent(new UserEmailUpdatedEvent(Id, oldEmail, newEmail.Value));
    }

    /// <summary>
    /// Updates the user's password hash
    /// </summary>
    /// <param name="newPasswordHash">The new password hash</param>
    public void UpdatePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash ?? throw new ArgumentNullException(nameof(newPasswordHash));
        
        if (string.IsNullOrWhiteSpace(PasswordHash))
            throw new ArgumentException("Password hash cannot be empty.", nameof(newPasswordHash));

        // Invalidate all refresh tokens when password changes
        foreach (var token in _refreshTokens.Where(t => t.IsActive))
        {
            token.Revoke();
        }

        AddDomainEvent(new UserPasswordUpdatedEvent(Id));
    }

    /// <summary>
    /// Verifies the user's email
    /// </summary>
    /// <param name="token">The verification token</param>
    public void VerifyEmail(string token)
    {
        if (IsEmailVerified)
            throw new InvalidOperationException("Email is already verified.");

        if (string.IsNullOrWhiteSpace(EmailVerificationToken))
            throw new InvalidOperationException("No verification token exists.");

        if (EmailVerificationTokenExpiry < DateTime.UtcNow)
            throw new InvalidOperationException("Verification token has expired.");

        if (!EmailVerificationToken.Equals(token, StringComparison.Ordinal))
            throw new InvalidOperationException("Invalid verification token.");

        IsEmailVerified = true;
        EmailVerificationToken = null;
        EmailVerificationTokenExpiry = null;

        AddDomainEvent(new UserEmailVerifiedEvent(Id, Email.Value));
    }

    /// <summary>
    /// Generates a new email verification token
    /// </summary>
    public void GenerateEmailVerificationToken()
    {
        EmailVerificationToken = Guid.NewGuid().ToString("N");
        EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24);
    }

    /// <summary>
    /// Generates a password reset token
    /// </summary>
    public void GeneratePasswordResetToken()
    {
        PasswordResetToken = Guid.NewGuid().ToString("N");
        PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        
        AddDomainEvent(new UserPasswordResetRequestedEvent(Id, Email.Value));
    }

    /// <summary>
    /// Validates a password reset token
    /// </summary>
    /// <param name="token">The reset token to validate</param>
    /// <returns>True if the token is valid, false otherwise</returns>
    public bool ValidatePasswordResetToken(string token)
    {
        return !string.IsNullOrWhiteSpace(PasswordResetToken) &&
               PasswordResetTokenExpiry > DateTime.UtcNow &&
               PasswordResetToken.Equals(token, StringComparison.Ordinal);
    }

    /// <summary>
    /// Clears the password reset token
    /// </summary>
    public void ClearPasswordResetToken()
    {
        PasswordResetToken = null;
        PasswordResetTokenExpiry = null;
    }

    /// <summary>
    /// Records a successful login
    /// </summary>
    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        AddDomainEvent(new UserLoggedInEvent(Id, Email.Value));
    }

    /// <summary>
    /// Activates the user account
    /// </summary>
    public void Activate()
    {
        if (IsActive)
            return;

        IsActive = true;
        AddDomainEvent(new UserActivatedEvent(Id, Email.Value));
    }

    /// <summary>
    /// Deactivates the user account
    /// </summary>
    public void Deactivate()
    {
        if (!IsActive)
            return;

        IsActive = false;
        
        // Revoke all active refresh tokens
        foreach (var token in _refreshTokens.Where(t => t.IsActive))
        {
            token.Revoke();
        }

        AddDomainEvent(new UserDeactivatedEvent(Id, Email.Value));
    }

    /// <summary>
    /// Adds a role to the user
    /// </summary>
    /// <param name="role">The role to add</param>
    public void AddRole(Role role)
    {
        if (role == null)
            throw new ArgumentNullException(nameof(role));

        if (_userRoles.Any(ur => ur.RoleId == role.Id))
            return; // Role already assigned

        var userRole = new UserRole(Id, role.Id);
        _userRoles.Add(userRole);
        
        AddDomainEvent(new UserRoleAssignedEvent(Id, role.Id, role.Name));
    }

    /// <summary>
    /// Removes a role from the user
    /// </summary>
    /// <param name="roleId">The ID of the role to remove</param>
    public void RemoveRole(Guid roleId)
    {
        var userRole = _userRoles.FirstOrDefault(ur => ur.RoleId == roleId);
        if (userRole != null)
        {
            _userRoles.Remove(userRole);
            AddDomainEvent(new UserRoleRemovedEvent(Id, roleId));
        }
    }

    /// <summary>
    /// Adds a refresh token to the user
    /// </summary>
    /// <param name="refreshToken">The refresh token to add</param>
    public void AddRefreshToken(RefreshToken refreshToken)
    {
        if (refreshToken == null)
            throw new ArgumentNullException(nameof(refreshToken));

        _refreshTokens.Add(refreshToken);
    }

    /// <summary>
    /// Revokes all refresh tokens for the user
    /// </summary>
    public void RevokeAllRefreshTokens()
    {
        foreach (var token in _refreshTokens.Where(t => t.IsActive))
        {
            token.Revoke();
        }
    }

    /// <summary>
    /// Sets email verification status for testing purposes
    /// </summary>
    /// <param name="isVerified">Whether the email should be verified</param>
    public void SetEmailVerificationForTesting(bool isVerified)
    {
        IsEmailVerified = isVerified;
        if (isVerified)
        {
            EmailVerificationToken = null;
            EmailVerificationTokenExpiry = null;
        }
    }
}