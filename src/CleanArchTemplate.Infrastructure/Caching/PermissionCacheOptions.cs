namespace CleanArchTemplate.Infrastructure.Caching;

/// <summary>
/// Configuration options for permission caching
/// </summary>
public class PermissionCacheOptions
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "PermissionCache";

    /// <summary>
    /// Default cache expiry time for permissions
    /// </summary>
    public TimeSpan DefaultExpiry { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// L1 (in-memory) cache expiry time
    /// </summary>
    public TimeSpan L1CacheExpiry { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Cache expiry time for permission evaluations
    /// </summary>
    public TimeSpan EvaluationCacheExpiry { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Cache expiry time for warm-up operations
    /// </summary>
    public TimeSpan WarmUpCacheExpiry { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// List of common permissions to pre-cache during warm-up
    /// </summary>
    public List<string> CommonPermissions { get; set; } = new()
    {
        "Users.Read",
        "Users.Create",
        "Users.Update",
        "Users.Delete",
        "Roles.Read",
        "Roles.Create",
        "Roles.Update",
        "Roles.Delete",
        "Permissions.Read",
        "Permissions.Create",
        "Permissions.Update",
        "Permissions.Delete"
    };

    /// <summary>
    /// Whether to enable hierarchical cache invalidation
    /// </summary>
    public bool EnableHierarchicalInvalidation { get; set; } = true;

    /// <summary>
    /// Whether to enable cache warm-up on user login
    /// </summary>
    public bool EnableWarmUpOnLogin { get; set; } = true;

    /// <summary>
    /// Maximum number of concurrent warm-up operations
    /// </summary>
    public int MaxConcurrentWarmUps { get; set; } = 10;

    /// <summary>
    /// Whether to use distributed cache (Redis)
    /// </summary>
    public bool UseDistributedCache { get; set; } = true;

    /// <summary>
    /// Whether to use in-memory cache as L1 cache
    /// </summary>
    public bool UseMemoryCache { get; set; } = true;
}