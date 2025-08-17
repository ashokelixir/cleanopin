using Microsoft.AspNetCore.Authorization;

namespace CleanArchTemplate.API.Authorization;

/// <summary>
/// Authorization requirement for a specific permission
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// The required permission name
    /// </summary>
    public string Permission { get; }

    /// <summary>
    /// Initializes a new instance of the PermissionRequirement
    /// </summary>
    /// <param name="permission">The required permission name</param>
    public PermissionRequirement(string permission)
    {
        Permission = permission ?? throw new ArgumentNullException(nameof(permission));
    }
}