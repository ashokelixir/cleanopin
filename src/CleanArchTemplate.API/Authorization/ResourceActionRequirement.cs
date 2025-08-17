using Microsoft.AspNetCore.Authorization;

namespace CleanArchTemplate.API.Authorization;

/// <summary>
/// Authorization requirement for a specific resource and action
/// </summary>
public class ResourceActionRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// The required resource name
    /// </summary>
    public string Resource { get; }

    /// <summary>
    /// The required action name
    /// </summary>
    public string Action { get; }

    /// <summary>
    /// The computed permission name (Resource.Action)
    /// </summary>
    public string Permission => $"{Resource}.{Action}";

    /// <summary>
    /// Initializes a new instance of the ResourceActionRequirement
    /// </summary>
    /// <param name="resource">The required resource name</param>
    /// <param name="action">The required action name</param>
    public ResourceActionRequirement(string resource, string action)
    {
        Resource = resource ?? throw new ArgumentNullException(nameof(resource));
        Action = action ?? throw new ArgumentNullException(nameof(action));
    }
}