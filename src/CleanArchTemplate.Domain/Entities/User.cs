using CleanArchTemplate.Domain.Common;
using CleanArchTemplate.Domain.Enums;
using CleanArchTemplate.Domain.Events;
using CleanArchTemplate.Domain.ValueObjects;

namespace CleanArchTemplate.Domain.Entities;

/// <summary>
/// Represents a user in the system
/// </summary>
public class User : BaseAuditableEntity, ITenantEntity
{
    private readonly List<UserRole> _userRoles = new();
    private readonly List<RefreshToken> _refreshTokens = new();
    private readonly List<UserPermission> _userPermissions = new();

    /// <summary>
    /// The ID of the tenant this user belongs to
    /// </summary>
    public Guid TenantId { get; set; }

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
    /// Navigation property for user-specific permission overrides
    /// </summary>
    public IReadOnlyCollection<UserPermission> UserPermissions => _userPermissions.AsReadOnly();

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
    /// Adds a user-specific permission override
    /// </summary>
    /// <param name="userPermission">The user permission to add</param>
    public void AddPermission(UserPermission userPermission)
    {
        if (userPermission == null)
            throw new ArgumentNullException(nameof(userPermission));

        if (userPermission.UserId != Id)
            throw new ArgumentException("User permission must belong to this user.", nameof(userPermission));

        // Remove existing permission for the same permission ID if it exists
        var existing = _userPermissions.FirstOrDefault(up => up.PermissionId == userPermission.PermissionId);
        if (existing != null)
        {
            _userPermissions.Remove(existing);
        }

        _userPermissions.Add(userPermission);
    }

    /// <summary>
    /// Removes a user-specific permission override
    /// </summary>
    /// <param name="permissionId">The ID of the permission to remove</param>
    public void RemovePermission(Guid permissionId)
    {
        var userPermission = _userPermissions.FirstOrDefault(up => up.PermissionId == permissionId);
        if (userPermission != null)
        {
            _userPermissions.Remove(userPermission);
            AddDomainEvent(new UserPermissionRemovedEvent(Id, permissionId, "Unknown", 
                userPermission.State, userPermission.Reason));
        }
    }

    /// <summary>
    /// Gets a user-specific permission override for a given permission
    /// </summary>
    /// <param name="permissionId">The permission ID</param>
    /// <returns>The user permission override if it exists</returns>
    public UserPermission? GetUserPermission(Guid permissionId)
    {
        return _userPermissions.FirstOrDefault(up => up.PermissionId == permissionId && up.IsActive());
    }

    /// <summary>
    /// Checks if the user has a specific permission override
    /// </summary>
    /// <param name="permissionId">The permission ID</param>
    /// <returns>True if the user has an active permission override</returns>
    public bool HasUserPermission(Guid permissionId)
    {
        return _userPermissions.Any(up => up.PermissionId == permissionId && up.IsActive());
    }

    /// <summary>
    /// Gets all active user permission overrides
    /// </summary>
    /// <returns>Collection of active user permissions</returns>
    public IEnumerable<UserPermission> GetActiveUserPermissions()
    {
        return _userPermissions.Where(up => up.IsActive());
    }

    /// <summary>
    /// Removes expired user permission overrides
    /// </summary>
    public void RemoveExpiredPermissions()
    {
        var expiredPermissions = _userPermissions.Where(up => up.IsExpired()).ToList();
        foreach (var expired in expiredPermissions)
        {
            _userPermissions.Remove(expired);
            AddDomainEvent(new UserPermissionRemovedEvent(Id, expired.PermissionId, "Unknown", 
                expired.State, "Expired"));
        }
    }

    /// <summary>
    /// Checks if the user has effective permission considering roles and user-specific overrides
    /// </summary>
    /// <param name="permission">The permission to check</param>
    /// <param name="userRoles">The user's roles with their permissions</param>
    /// <returns>True if the user has the permission</returns>
    public bool HasEffectivePermission(Permission permission, IEnumerable<Role> userRoles)
    {
        if (permission == null)
            throw new ArgumentNullException(nameof(permission));

        if (!permission.IsActive)
            return false;

        // Check for user-specific override first (highest priority)
        var userOverride = GetUserPermission(permission.Id);
        if (userOverride != null)
        {
            return userOverride.State == PermissionState.Grant;
        }

        // Check role-based permissions
        return userRoles?.Any(role => role.IsActive && role.HasPermission(permission.Id)) ?? false;
    }

    /// <summary>
    /// Gets all effective permissions for the user considering roles and user-specific overrides
    /// </summary>
    /// <param name="availablePermissions">All available permissions in the system</param>
    /// <param name="userRoles">The user's roles with their permissions</param>
    /// <returns>Collection of effective permission names</returns>
    public IEnumerable<string> GetEffectivePermissions(IEnumerable<Permission> availablePermissions, IEnumerable<Role> userRoles)
    {
        if (availablePermissions == null)
            throw new ArgumentNullException(nameof(availablePermissions));

        var permissions = availablePermissions.ToList();
        var roles = userRoles?.ToList() ?? new List<Role>();
        var effectivePermissions = new HashSet<string>();

        // Get permissions from roles
        foreach (var role in roles.Where(r => r.IsActive))
        {
            foreach (var rolePermissionId in role.GetPermissionIds())
            {
                var permission = permissions.FirstOrDefault(p => p.Id == rolePermissionId);
                if (permission != null && permission.IsActive)
                {
                    effectivePermissions.Add(permission.Name);
                }
            }
        }

        // Apply user-specific permission overrides
        foreach (var userPermission in GetActiveUserPermissions())
        {
            var permission = permissions.FirstOrDefault(p => p.Id == userPermission.PermissionId);
            if (permission == null || !permission.IsActive)
                continue;

            if (userPermission.State == PermissionState.Grant)
            {
                effectivePermissions.Add(permission.Name);
            }
            else if (userPermission.State == PermissionState.Deny)
            {
                effectivePermissions.Remove(permission.Name);
            }
        }

        return effectivePermissions.OrderBy(p => p);
    }

    /// <summary>
    /// Checks if the user has any of the specified permissions
    /// </summary>
    /// <param name="permissionNames">The permission names to check</param>
    /// <param name="availablePermissions">All available permissions in the system</param>
    /// <param name="userRoles">The user's roles with their permissions</param>
    /// <returns>True if the user has at least one of the permissions</returns>
    public bool HasAnyPermission(IEnumerable<string> permissionNames, IEnumerable<Permission> availablePermissions, IEnumerable<Role> userRoles)
    {
        if (permissionNames == null)
            throw new ArgumentNullException(nameof(permissionNames));

        var permissionList = permissionNames.ToList();
        if (!permissionList.Any())
            return false;

        var permissions = availablePermissions?.ToList() ?? new List<Permission>();

        // Check each permission until we find one the user has
        foreach (var permissionName in permissionList)
        {
            var permission = permissions.FirstOrDefault(p => p.Name == permissionName?.Trim());
            if (permission != null && HasEffectivePermission(permission, userRoles))
                return true;
        }

        return false;
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