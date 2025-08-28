namespace CleanArchTemplate.Application.Common.Models;

/// <summary>
/// Information about a tenant
/// </summary>
public class TenantInfo
{
    /// <summary>
    /// The tenant's unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The tenant's display name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The tenant's unique identifier (used for subdomain, etc.)
    /// </summary>
    public string Identifier { get; set; } = string.Empty;

    /// <summary>
    /// The tenant's database connection string (if using separate databases)
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Configuration specific to this tenant
    /// </summary>
    public Dictionary<string, object> Configuration { get; set; } = new();

    /// <summary>
    /// Indicates whether the tenant is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// The date and time when the tenant's subscription expires
    /// </summary>
    public DateTime? SubscriptionExpiresAt { get; set; }

    /// <summary>
    /// Checks if the tenant's subscription is expired
    /// </summary>
    /// <returns>True if the subscription is expired</returns>
    public bool IsSubscriptionExpired()
    {
        return SubscriptionExpiresAt.HasValue && SubscriptionExpiresAt.Value < DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the tenant is available for use
    /// </summary>
    /// <returns>True if the tenant is active and subscription is not expired</returns>
    public bool IsAvailable()
    {
        return IsActive && !IsSubscriptionExpired();
    }
}