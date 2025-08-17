namespace CleanArchTemplate.Application.Common.Models;

/// <summary>
/// DTO representing aggregated permission matrix statistics
/// </summary>
public class PermissionMatrixStatisticsDto
{
    /// <summary>
    /// Overall system statistics
    /// </summary>
    public SystemPermissionStatisticsDto SystemStatistics { get; set; } = new();

    /// <summary>
    /// Role-based statistics
    /// </summary>
    public IEnumerable<RolePermissionStatisticsDto> RoleStatistics { get; set; } = new List<RolePermissionStatisticsDto>();

    /// <summary>
    /// Permission usage statistics
    /// </summary>
    public IEnumerable<PermissionUsageStatisticsDto> PermissionUsage { get; set; } = new List<PermissionUsageStatisticsDto>();

    /// <summary>
    /// Category-based statistics
    /// </summary>
    public IEnumerable<CategoryStatisticsDto> CategoryStatistics { get; set; } = new List<CategoryStatisticsDto>();

    /// <summary>
    /// User override statistics
    /// </summary>
    public UserOverrideStatisticsDto UserOverrideStatistics { get; set; } = new();

    /// <summary>
    /// Statistics generation timestamp
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// DTO representing system-wide permission statistics
/// </summary>
public class SystemPermissionStatisticsDto
{
    /// <summary>
    /// Total number of permissions
    /// </summary>
    public int TotalPermissions { get; set; }

    /// <summary>
    /// Total number of roles
    /// </summary>
    public int TotalRoles { get; set; }

    /// <summary>
    /// Total number of users
    /// </summary>
    public int TotalUsers { get; set; }

    /// <summary>
    /// Total number of role-permission assignments
    /// </summary>
    public int TotalRolePermissionAssignments { get; set; }

    /// <summary>
    /// Total number of user permission overrides
    /// </summary>
    public int TotalUserPermissionOverrides { get; set; }

    /// <summary>
    /// Number of active permissions
    /// </summary>
    public int ActivePermissions { get; set; }

    /// <summary>
    /// Number of inactive permissions
    /// </summary>
    public int InactivePermissions { get; set; }

    /// <summary>
    /// Average permissions per role
    /// </summary>
    public double AveragePermissionsPerRole { get; set; }

    /// <summary>
    /// Average effective permissions per user
    /// </summary>
    public double AverageEffectivePermissionsPerUser { get; set; }
}

/// <summary>
/// DTO representing role-specific permission statistics
/// </summary>
public class RolePermissionStatisticsDto
{
    /// <summary>
    /// Role information
    /// </summary>
    public RoleDto Role { get; set; } = new();

    /// <summary>
    /// Number of permissions assigned to this role
    /// </summary>
    public int PermissionCount { get; set; }

    /// <summary>
    /// Number of users with this role
    /// </summary>
    public int UserCount { get; set; }

    /// <summary>
    /// Permissions by category
    /// </summary>
    public Dictionary<string, int> PermissionsByCategory { get; set; } = new();

    /// <summary>
    /// Most recent permission assignment date
    /// </summary>
    public DateTime? LastPermissionAssigned { get; set; }
}

/// <summary>
/// DTO representing permission usage statistics
/// </summary>
public class PermissionUsageStatisticsDto
{
    /// <summary>
    /// Permission information
    /// </summary>
    public PermissionDto Permission { get; set; } = new();

    /// <summary>
    /// Number of roles that have this permission
    /// </summary>
    public int RoleCount { get; set; }

    /// <summary>
    /// Number of users with this permission (through roles)
    /// </summary>
    public int UserCount { get; set; }

    /// <summary>
    /// Number of user overrides for this permission
    /// </summary>
    public int UserOverrideCount { get; set; }

    /// <summary>
    /// Usage percentage across all roles
    /// </summary>
    public double RoleUsagePercentage { get; set; }

    /// <summary>
    /// Usage percentage across all users
    /// </summary>
    public double UserUsagePercentage { get; set; }
}

/// <summary>
/// DTO representing category-based statistics
/// </summary>
public class CategoryStatisticsDto
{
    /// <summary>
    /// Category name
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Number of permissions in this category
    /// </summary>
    public int PermissionCount { get; set; }

    /// <summary>
    /// Number of role assignments for permissions in this category
    /// </summary>
    public int RoleAssignmentCount { get; set; }

    /// <summary>
    /// Number of user overrides for permissions in this category
    /// </summary>
    public int UserOverrideCount { get; set; }

    /// <summary>
    /// Average usage across roles for this category
    /// </summary>
    public double AverageRoleUsage { get; set; }
}

/// <summary>
/// DTO representing user override statistics
/// </summary>
public class UserOverrideStatisticsDto
{
    /// <summary>
    /// Total number of user permission overrides
    /// </summary>
    public int TotalOverrides { get; set; }

    /// <summary>
    /// Number of grant overrides
    /// </summary>
    public int GrantOverrides { get; set; }

    /// <summary>
    /// Number of deny overrides
    /// </summary>
    public int DenyOverrides { get; set; }

    /// <summary>
    /// Number of active overrides
    /// </summary>
    public int ActiveOverrides { get; set; }

    /// <summary>
    /// Number of expired overrides
    /// </summary>
    public int ExpiredOverrides { get; set; }

    /// <summary>
    /// Number of users with at least one override
    /// </summary>
    public int UsersWithOverrides { get; set; }

    /// <summary>
    /// Average overrides per user (for users with overrides)
    /// </summary>
    public double AverageOverridesPerUser { get; set; }

    /// <summary>
    /// Most common override reasons
    /// </summary>
    public Dictionary<string, int> CommonOverrideReasons { get; set; } = new();
}