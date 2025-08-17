namespace CleanArchTemplate.Application.Common.Models;

/// <summary>
/// DTO representing permission matrix validation results
/// </summary>
public class PermissionMatrixValidationResultDto
{
    /// <summary>
    /// Whether the matrix is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Validation errors found
    /// </summary>
    public IEnumerable<PermissionMatrixValidationErrorDto> Errors { get; set; } = new List<PermissionMatrixValidationErrorDto>();

    /// <summary>
    /// Validation warnings found
    /// </summary>
    public IEnumerable<PermissionMatrixValidationWarningDto> Warnings { get; set; } = new List<PermissionMatrixValidationWarningDto>();

    /// <summary>
    /// Validation summary
    /// </summary>
    public PermissionMatrixValidationSummaryDto Summary { get; set; } = new();

    /// <summary>
    /// Validation timestamp
    /// </summary>
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// DTO representing a validation error
/// </summary>
public class PermissionMatrixValidationErrorDto
{
    /// <summary>
    /// Error code
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Error message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Error severity
    /// </summary>
    public ValidationSeverity Severity { get; set; }

    /// <summary>
    /// Entity type that has the error (Role, Permission, User)
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Entity ID that has the error
    /// </summary>
    public Guid? EntityId { get; set; }

    /// <summary>
    /// Entity name that has the error
    /// </summary>
    public string? EntityName { get; set; }

    /// <summary>
    /// Additional context about the error
    /// </summary>
    public Dictionary<string, object> Context { get; set; } = new();
}

/// <summary>
/// DTO representing a validation warning
/// </summary>
public class PermissionMatrixValidationWarningDto
{
    /// <summary>
    /// Warning code
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Warning message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Entity type that has the warning (Role, Permission, User)
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Entity ID that has the warning
    /// </summary>
    public Guid? EntityId { get; set; }

    /// <summary>
    /// Entity name that has the warning
    /// </summary>
    public string? EntityName { get; set; }

    /// <summary>
    /// Suggested action to resolve the warning
    /// </summary>
    public string? SuggestedAction { get; set; }

    /// <summary>
    /// Additional context about the warning
    /// </summary>
    public Dictionary<string, object> Context { get; set; } = new();
}

/// <summary>
/// DTO representing validation summary
/// </summary>
public class PermissionMatrixValidationSummaryDto
{
    /// <summary>
    /// Total number of errors
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// Total number of warnings
    /// </summary>
    public int WarningCount { get; set; }

    /// <summary>
    /// Number of roles validated
    /// </summary>
    public int RolesValidated { get; set; }

    /// <summary>
    /// Number of permissions validated
    /// </summary>
    public int PermissionsValidated { get; set; }

    /// <summary>
    /// Number of assignments validated
    /// </summary>
    public int AssignmentsValidated { get; set; }

    /// <summary>
    /// Number of user overrides validated
    /// </summary>
    public int UserOverridesValidated { get; set; }

    /// <summary>
    /// Validation checks performed
    /// </summary>
    public IEnumerable<string> ChecksPerformed { get; set; } = new List<string>();

    /// <summary>
    /// Validation duration in milliseconds
    /// </summary>
    public long ValidationDurationMs { get; set; }
}

/// <summary>
/// DTO representing permission matrix change history
/// </summary>
public class PermissionMatrixChangeDto : BaseAuditableDto
{
    /// <summary>
    /// Type of change (RolePermissionAssigned, UserPermissionOverridden, etc.)
    /// </summary>
    public string ChangeType { get; set; } = string.Empty;

    /// <summary>
    /// Entity type affected (Role, User, Permission)
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Entity ID affected
    /// </summary>
    public Guid EntityId { get; set; }

    /// <summary>
    /// Entity name affected
    /// </summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>
    /// Permission involved in the change
    /// </summary>
    public PermissionDto? Permission { get; set; }

    /// <summary>
    /// Role involved in the change (if applicable)
    /// </summary>
    public RoleDto? Role { get; set; }

    /// <summary>
    /// User involved in the change (if applicable)
    /// </summary>
    public UserDto? User { get; set; }

    /// <summary>
    /// Previous value before the change
    /// </summary>
    public string? OldValue { get; set; }

    /// <summary>
    /// New value after the change
    /// </summary>
    public string? NewValue { get; set; }

    /// <summary>
    /// Reason for the change
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Additional metadata about the change
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Enumeration for validation severity levels
/// </summary>
public enum ValidationSeverity
{
    /// <summary>
    /// Low severity - informational
    /// </summary>
    Low = 1,

    /// <summary>
    /// Medium severity - should be addressed
    /// </summary>
    Medium = 2,

    /// <summary>
    /// High severity - must be addressed
    /// </summary>
    High = 3,

    /// <summary>
    /// Critical severity - system integrity at risk
    /// </summary>
    Critical = 4
}