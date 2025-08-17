using Microsoft.AspNetCore.Authorization;

namespace CleanArchTemplate.API.Authorization;

/// <summary>
/// Authorization requirement for any one of the specified permissions
/// </summary>
public class AnyPermissionRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// The permissions (user needs any one of these)
    /// </summary>
    public IReadOnlyList<string> Permissions { get; }

    /// <summary>
    /// Initializes a new instance of the AnyPermissionRequirement
    /// </summary>
    /// <param name="permissions">The permissions (user needs any one of these)</param>
    public AnyPermissionRequirement(params string[] permissions)
    {
        if (permissions == null || permissions.Length == 0)
        {
            throw new ArgumentException("At least one permission must be specified", nameof(permissions));
        }

        Permissions = permissions.ToList().AsReadOnly();
    }
}