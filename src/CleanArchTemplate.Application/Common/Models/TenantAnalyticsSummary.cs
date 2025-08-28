namespace CleanArchTemplate.Application.Common.Models;

/// <summary>
/// Analytics summary for a tenant
/// </summary>
public class TenantAnalyticsSummary
{
    /// <summary>
    /// The tenant ID
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// The tenant name
    /// </summary>
    public string TenantName { get; set; } = string.Empty;

    /// <summary>
    /// The start date of the analytics period
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// The end date of the analytics period
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Total number of active users in the period
    /// </summary>
    public int ActiveUsers { get; set; }

    /// <summary>
    /// Total number of API requests in the period
    /// </summary>
    public long TotalApiRequests { get; set; }

    /// <summary>
    /// Total data storage used (in bytes)
    /// </summary>
    public long StorageUsed { get; set; }

    /// <summary>
    /// Total bandwidth used (in bytes)
    /// </summary>
    public long BandwidthUsed { get; set; }

    /// <summary>
    /// Number of features enabled
    /// </summary>
    public int EnabledFeatures { get; set; }

    /// <summary>
    /// Average response time for API requests (in milliseconds)
    /// </summary>
    public double AverageResponseTime { get; set; }

    /// <summary>
    /// Error rate percentage
    /// </summary>
    public double ErrorRate { get; set; }

    /// <summary>
    /// Most used features
    /// </summary>
    public Dictionary<string, long> FeatureUsage { get; set; } = new();

    /// <summary>
    /// Daily usage breakdown
    /// </summary>
    public Dictionary<DateTime, TenantDailyUsage> DailyUsage { get; set; } = new();

    /// <summary>
    /// Top metrics for the period
    /// </summary>
    public Dictionary<string, TenantUsageStatistics> TopMetrics { get; set; } = new();
}

/// <summary>
/// Daily usage statistics for a tenant
/// </summary>
public class TenantDailyUsage
{
    /// <summary>
    /// The date
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Number of active users on this date
    /// </summary>
    public int ActiveUsers { get; set; }

    /// <summary>
    /// Number of API requests on this date
    /// </summary>
    public long ApiRequests { get; set; }

    /// <summary>
    /// Storage used on this date (in bytes)
    /// </summary>
    public long StorageUsed { get; set; }

    /// <summary>
    /// Bandwidth used on this date (in bytes)
    /// </summary>
    public long BandwidthUsed { get; set; }

    /// <summary>
    /// Average response time on this date (in milliseconds)
    /// </summary>
    public double AverageResponseTime { get; set; }

    /// <summary>
    /// Error count on this date
    /// </summary>
    public long ErrorCount { get; set; }
}