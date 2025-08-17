namespace CleanArchTemplate.Application.Common.Models;

/// <summary>
/// DTO representing a comparison between two permission sets
/// </summary>
public class PermissionMatrixComparisonDto
{
    /// <summary>
    /// First entity being compared (role or user)
    /// </summary>
    public PermissionEntityDto FirstEntity { get; set; } = new();

    /// <summary>
    /// Second entity being compared (role or user)
    /// </summary>
    public PermissionEntityDto SecondEntity { get; set; } = new();

    /// <summary>
    /// Permissions that both entities have
    /// </summary>
    public IEnumerable<PermissionDto> CommonPermissions { get; set; } = new List<PermissionDto>();

    /// <summary>
    /// Permissions that only the first entity has
    /// </summary>
    public IEnumerable<PermissionDto> FirstEntityOnlyPermissions { get; set; } = new List<PermissionDto>();

    /// <summary>
    /// Permissions that only the second entity has
    /// </summary>
    public IEnumerable<PermissionDto> SecondEntityOnlyPermissions { get; set; } = new List<PermissionDto>();

    /// <summary>
    /// Comparison statistics
    /// </summary>
    public PermissionComparisonStatisticsDto Statistics { get; set; } = new();

    /// <summary>
    /// Comparison timestamp
    /// </summary>
    public DateTime ComparedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// DTO representing an entity (role or user) in permission comparison
/// </summary>
public class PermissionEntityDto
{
    /// <summary>
    /// Entity ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Entity name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Entity type (Role or User)
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Total permissions count
    /// </summary>
    public int PermissionCount { get; set; }
}

/// <summary>
/// DTO representing permission comparison statistics
/// </summary>
public class PermissionComparisonStatisticsDto
{
    /// <summary>
    /// Number of common permissions
    /// </summary>
    public int CommonPermissionsCount { get; set; }

    /// <summary>
    /// Number of permissions unique to first entity
    /// </summary>
    public int FirstEntityUniqueCount { get; set; }

    /// <summary>
    /// Number of permissions unique to second entity
    /// </summary>
    public int SecondEntityUniqueCount { get; set; }

    /// <summary>
    /// Similarity percentage (0-100)
    /// </summary>
    public double SimilarityPercentage { get; set; }

    /// <summary>
    /// Total permissions across both entities
    /// </summary>
    public int TotalUniquePermissions { get; set; }
}