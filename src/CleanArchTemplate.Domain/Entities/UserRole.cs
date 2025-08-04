using CleanArchTemplate.Domain.Common;

namespace CleanArchTemplate.Domain.Entities;

/// <summary>
/// Represents the many-to-many relationship between users and roles
/// </summary>
public class UserRole : BaseAuditableEntity
{
    /// <summary>
    /// The ID of the user
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// The ID of the role
    /// </summary>
    public Guid RoleId { get; private set; }

    /// <summary>
    /// Navigation property to the user
    /// </summary>
    public User User { get; private set; } = null!;

    /// <summary>
    /// Navigation property to the role
    /// </summary>
    public Role Role { get; private set; } = null!;

    // Private constructor for EF Core
    private UserRole() { }

    /// <summary>
    /// Creates a new user role relationship
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="roleId">The role ID</param>
    public UserRole(Guid userId, Guid roleId)
    {
        UserId = userId;
        RoleId = roleId;
    }
}
