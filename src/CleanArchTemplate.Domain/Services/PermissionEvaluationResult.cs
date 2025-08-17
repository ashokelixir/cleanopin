using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.Enums;

namespace CleanArchTemplate.Domain.Services;

/// <summary>
/// Represents the result of a permission evaluation with detailed information about how the decision was made
/// </summary>
public class PermissionEvaluationResult
{
    /// <summary>
    /// Whether the user has the permission
    /// </summary>
    public bool HasPermission { get; }

    /// <summary>
    /// The source of the permission decision
    /// </summary>
    public PermissionSource Source { get; }

    /// <summary>
    /// The user-specific permission override if applicable
    /// </summary>
    public UserPermission? UserOverride { get; }

    /// <summary>
    /// The roles that grant this permission
    /// </summary>
    public IReadOnlyCollection<string> GrantingRoles { get; }

    /// <summary>
    /// The reason for the permission decision
    /// </summary>
    public string Reason { get; }

    /// <summary>
    /// Whether the permission was inherited from a parent permission
    /// </summary>
    public bool IsInherited { get; }

    /// <summary>
    /// The parent permission name if inherited
    /// </summary>
    public string? ParentPermissionName { get; }

    /// <summary>
    /// Creates a new permission evaluation result
    /// </summary>
    /// <param name="hasPermission">Whether the user has the permission</param>
    /// <param name="source">The source of the permission decision</param>
    /// <param name="reason">The reason for the decision</param>
    /// <param name="userOverride">The user-specific permission override if applicable</param>
    /// <param name="grantingRoles">The roles that grant this permission</param>
    /// <param name="isInherited">Whether the permission was inherited</param>
    /// <param name="parentPermissionName">The parent permission name if inherited</param>
    public PermissionEvaluationResult(
        bool hasPermission,
        PermissionSource source,
        string reason,
        UserPermission? userOverride = null,
        IEnumerable<string>? grantingRoles = null,
        bool isInherited = false,
        string? parentPermissionName = null)
    {
        HasPermission = hasPermission;
        Source = source;
        Reason = reason ?? throw new ArgumentNullException(nameof(reason));
        UserOverride = userOverride;
        GrantingRoles = grantingRoles?.ToList().AsReadOnly() ?? new List<string>().AsReadOnly();
        IsInherited = isInherited;
        ParentPermissionName = parentPermissionName;
    }

    /// <summary>
    /// Creates a result for a user override decision
    /// </summary>
    /// <param name="userOverride">The user permission override</param>
    /// <param name="reason">The reason for the decision</param>
    /// <returns>Permission evaluation result</returns>
    public static PermissionEvaluationResult FromUserOverride(UserPermission userOverride, string reason)
    {
        return new PermissionEvaluationResult(
            hasPermission: userOverride.State == PermissionState.Grant,
            source: PermissionSource.UserOverride,
            reason: reason,
            userOverride: userOverride);
    }

    /// <summary>
    /// Creates a result for a role-based permission decision
    /// </summary>
    /// <param name="hasPermission">Whether the user has the permission</param>
    /// <param name="grantingRoles">The roles that grant this permission</param>
    /// <param name="reason">The reason for the decision</param>
    /// <returns>Permission evaluation result</returns>
    public static PermissionEvaluationResult FromRoles(bool hasPermission, IEnumerable<string> grantingRoles, string reason)
    {
        return new PermissionEvaluationResult(
            hasPermission: hasPermission,
            source: PermissionSource.Role,
            reason: reason,
            grantingRoles: grantingRoles);
    }

    /// <summary>
    /// Creates a result for an inherited permission decision
    /// </summary>
    /// <param name="hasPermission">Whether the user has the permission</param>
    /// <param name="parentPermissionName">The parent permission name</param>
    /// <param name="grantingRoles">The roles that grant the parent permission</param>
    /// <param name="reason">The reason for the decision</param>
    /// <returns>Permission evaluation result</returns>
    public static PermissionEvaluationResult FromInheritance(bool hasPermission, string parentPermissionName, IEnumerable<string> grantingRoles, string reason)
    {
        return new PermissionEvaluationResult(
            hasPermission: hasPermission,
            source: PermissionSource.Inheritance,
            reason: reason,
            grantingRoles: grantingRoles,
            isInherited: true,
            parentPermissionName: parentPermissionName);
    }

    /// <summary>
    /// Creates a result for a denied permission
    /// </summary>
    /// <param name="reason">The reason for the denial</param>
    /// <returns>Permission evaluation result</returns>
    public static PermissionEvaluationResult Denied(string reason)
    {
        return new PermissionEvaluationResult(
            hasPermission: false,
            source: PermissionSource.None,
            reason: reason);
    }
}

/// <summary>
/// Represents the source of a permission decision
/// </summary>
public enum PermissionSource
{
    /// <summary>
    /// No permission source (permission denied)
    /// </summary>
    None = 0,

    /// <summary>
    /// Permission granted/denied through user-specific override
    /// </summary>
    UserOverride = 1,

    /// <summary>
    /// Permission granted through role assignment
    /// </summary>
    Role = 2,

    /// <summary>
    /// Permission granted through inheritance from parent permission
    /// </summary>
    Inheritance = 3
}