namespace CleanArchTemplate.Application.Common.Models;

/// <summary>
/// DTO representing an authorization result
/// </summary>
public class AuthorizationResult
{
    /// <summary>
    /// Whether authorization was successful
    /// </summary>
    public bool IsAuthorized { get; set; }

    /// <summary>
    /// Reason for authorization failure (if applicable)
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// Required permission that was checked
    /// </summary>
    public string? RequiredPermission { get; set; }

    /// <summary>
    /// User's actual permissions
    /// </summary>
    public IEnumerable<string> UserPermissions { get; set; } = new List<string>();

    /// <summary>
    /// Additional context about the authorization
    /// </summary>
    public Dictionary<string, object> Context { get; set; } = new();

    /// <summary>
    /// Authorization timestamp
    /// </summary>
    public DateTime AuthorizedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a successful authorization result
    /// </summary>
    /// <param name="requiredPermission">The permission that was checked</param>
    /// <param name="userPermissions">The user's permissions</param>
    /// <returns>Successful authorization result</returns>
    public static AuthorizationResult Success(string requiredPermission, IEnumerable<string> userPermissions)
    {
        return new AuthorizationResult
        {
            IsAuthorized = true,
            RequiredPermission = requiredPermission,
            UserPermissions = userPermissions
        };
    }

    /// <summary>
    /// Creates a failed authorization result
    /// </summary>
    /// <param name="requiredPermission">The permission that was checked</param>
    /// <param name="failureReason">Reason for failure</param>
    /// <param name="userPermissions">The user's permissions</param>
    /// <returns>Failed authorization result</returns>
    public static AuthorizationResult Failure(string requiredPermission, string failureReason, IEnumerable<string> userPermissions)
    {
        return new AuthorizationResult
        {
            IsAuthorized = false,
            RequiredPermission = requiredPermission,
            FailureReason = failureReason,
            UserPermissions = userPermissions
        };
    }
}

/// <summary>
/// DTO representing effective permissions for a user
/// </summary>
public class EffectivePermissionsDto
{
    /// <summary>
    /// User information
    /// </summary>
    public UserDto User { get; set; } = new();

    /// <summary>
    /// Permissions from roles
    /// </summary>
    public IEnumerable<PermissionSourceDto> RolePermissions { get; set; } = new List<PermissionSourceDto>();

    /// <summary>
    /// User-specific permission overrides
    /// </summary>
    public IEnumerable<UserPermissionOverrideDto> UserOverrides { get; set; } = new List<UserPermissionOverrideDto>();

    /// <summary>
    /// Final effective permissions after applying all rules
    /// </summary>
    public IEnumerable<string> EffectivePermissions { get; set; } = new List<string>();

    /// <summary>
    /// Permissions that were denied by user overrides
    /// </summary>
    public IEnumerable<string> DeniedPermissions { get; set; } = new List<string>();

    /// <summary>
    /// Permissions that were granted by user overrides
    /// </summary>
    public IEnumerable<string> GrantedPermissions { get; set; } = new List<string>();

    /// <summary>
    /// Calculation timestamp
    /// </summary>
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// DTO representing a permission source (role or direct assignment)
/// </summary>
public class PermissionSourceDto
{
    /// <summary>
    /// Permission name
    /// </summary>
    public string Permission { get; set; } = string.Empty;

    /// <summary>
    /// Source type (Role, DirectAssignment)
    /// </summary>
    public string SourceType { get; set; } = string.Empty;

    /// <summary>
    /// Source ID (role ID, etc.)
    /// </summary>
    public Guid SourceId { get; set; }

    /// <summary>
    /// Source name (role name, etc.)
    /// </summary>
    public string SourceName { get; set; } = string.Empty;

    /// <summary>
    /// Whether this permission is active
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// DTO representing authorization context for a user
/// </summary>
public class AuthorizationContextDto
{
    /// <summary>
    /// User information
    /// </summary>
    public UserDto User { get; set; } = new();

    /// <summary>
    /// User's roles
    /// </summary>
    public IEnumerable<RoleDto> Roles { get; set; } = new List<RoleDto>();

    /// <summary>
    /// User's effective permissions
    /// </summary>
    public IEnumerable<string> Permissions { get; set; } = new List<string>();

    /// <summary>
    /// User's permission overrides
    /// </summary>
    public IEnumerable<UserPermissionOverrideDto> PermissionOverrides { get; set; } = new List<UserPermissionOverrideDto>();

    /// <summary>
    /// Session information
    /// </summary>
    public AuthorizationSessionDto Session { get; set; } = new();

    /// <summary>
    /// Context generation timestamp
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// DTO representing authorization session information
/// </summary>
public class AuthorizationSessionDto
{
    /// <summary>
    /// Session ID
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Session start time
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Session expiry time
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Whether the session is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// IP address of the session
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent of the session
    /// </summary>
    public string? UserAgent { get; set; }
}

/// <summary>
/// DTO representing authorization requirements for a resource and action
/// </summary>
public class AuthorizationRequirementsDto
{
    /// <summary>
    /// Resource name
    /// </summary>
    public string Resource { get; set; } = string.Empty;

    /// <summary>
    /// Action name
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Required permissions
    /// </summary>
    public IEnumerable<string> RequiredPermissions { get; set; } = new List<string>();

    /// <summary>
    /// Alternative permission sets (user needs any one of these sets)
    /// </summary>
    public IEnumerable<IEnumerable<string>> AlternativePermissionSets { get; set; } = new List<IEnumerable<string>>();

    /// <summary>
    /// Whether the resource requires ownership check
    /// </summary>
    public bool RequiresOwnership { get; set; }

    /// <summary>
    /// Additional requirements or constraints
    /// </summary>
    public Dictionary<string, object> AdditionalRequirements { get; set; } = new();

    /// <summary>
    /// Description of the authorization requirements
    /// </summary>
    public string Description { get; set; } = string.Empty;
}