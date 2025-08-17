using CleanArchTemplate.Domain.Enums;

namespace CleanArchTemplate.Application.Common.Models;

/// <summary>
/// DTO representing the complete role-permission matrix
/// </summary>
public class PermissionMatrixDto
{
    /// <summary>
    /// List of roles in the matrix
    /// </summary>
    public IEnumerable<RoleDto> Roles { get; set; } = new List<RoleDto>();

    /// <summary>
    /// List of permissions in the matrix
    /// </summary>
    public IEnumerable<PermissionDto> Permissions { get; set; } = new List<PermissionDto>();

    /// <summary>
    /// Role-permission assignments
    /// </summary>
    public IEnumerable<RolePermissionAssignmentDto> Assignments { get; set; } = new List<RolePermissionAssignmentDto>();

    /// <summary>
    /// Matrix metadata
    /// </summary>
    public PermissionMatrixMetadataDto Metadata { get; set; } = new();
}

/// <summary>
/// DTO representing a role-permission assignment
/// </summary>
public class RolePermissionAssignmentDto
{
    /// <summary>
    /// Role ID
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// Permission ID
    /// </summary>
    public Guid PermissionId { get; set; }

    /// <summary>
    /// Whether the permission is assigned to the role
    /// </summary>
    public bool IsAssigned { get; set; }

    /// <summary>
    /// Assignment timestamp
    /// </summary>
    public DateTime? AssignedAt { get; set; }

    /// <summary>
    /// User who made the assignment
    /// </summary>
    public string? AssignedBy { get; set; }
}

/// <summary>
/// DTO representing user permission matrix with effective permissions
/// </summary>
public class UserPermissionMatrixDto
{
    /// <summary>
    /// User information
    /// </summary>
    public UserDto User { get; set; } = new();

    /// <summary>
    /// Permissions inherited from roles
    /// </summary>
    public IEnumerable<PermissionDto> RolePermissions { get; set; } = new List<PermissionDto>();

    /// <summary>
    /// User-specific permission overrides
    /// </summary>
    public IEnumerable<UserPermissionOverrideDto> UserOverrides { get; set; } = new List<UserPermissionOverrideDto>();

    /// <summary>
    /// Final effective permissions after applying overrides
    /// </summary>
    public IEnumerable<PermissionDto> EffectivePermissions { get; set; } = new List<PermissionDto>();

    /// <summary>
    /// Matrix metadata
    /// </summary>
    public PermissionMatrixMetadataDto Metadata { get; set; } = new();
}

/// <summary>
/// DTO representing a user permission override
/// </summary>
public class UserPermissionOverrideDto : BaseAuditableDto
{
    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Permission information
    /// </summary>
    public PermissionDto Permission { get; set; } = new();

    /// <summary>
    /// Permission state (Grant/Deny)
    /// </summary>
    public PermissionState State { get; set; }

    /// <summary>
    /// Reason for the override
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Expiration date for the override
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Whether the override is currently active
    /// </summary>
    public bool IsActive => !ExpiresAt.HasValue || ExpiresAt.Value > DateTime.UtcNow;
}

/// <summary>
/// DTO representing permission matrix metadata
/// </summary>
public class PermissionMatrixMetadataDto
{
    /// <summary>
    /// Total number of roles
    /// </summary>
    public int TotalRoles { get; set; }

    /// <summary>
    /// Total number of permissions
    /// </summary>
    public int TotalPermissions { get; set; }

    /// <summary>
    /// Total number of assignments
    /// </summary>
    public int TotalAssignments { get; set; }

    /// <summary>
    /// Matrix generation timestamp
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Categories included in the matrix
    /// </summary>
    public IEnumerable<string> Categories { get; set; } = new List<string>();

    /// <summary>
    /// Resources included in the matrix
    /// </summary>
    public IEnumerable<string> Resources { get; set; } = new List<string>();
}