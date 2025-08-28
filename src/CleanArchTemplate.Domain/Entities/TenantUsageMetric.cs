using CleanArchTemplate.Domain.Common;
using CleanArchTemplate.Domain.Interfaces;

namespace CleanArchTemplate.Domain.Entities;

/// <summary>
/// Represents a tenant usage metric record
/// </summary>
public class TenantUsageMetric : BaseEntity, ITenantEntity
{
    /// <summary>
    /// The tenant ID this metric belongs to
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// The metric name
    /// </summary>
    public string MetricName { get; private set; } = string.Empty;

    /// <summary>
    /// The metric value
    /// </summary>
    public double Value { get; private set; }

    /// <summary>
    /// Optional tags for the metric (stored as JSON)
    /// </summary>
    public string Tags { get; private set; } = "{}";

    /// <summary>
    /// The timestamp when the metric was recorded
    /// </summary>
    public DateTime RecordedAt { get; private set; }

    /// <summary>
    /// The user who recorded the metric (if applicable)
    /// </summary>
    public Guid? RecordedBy { get; private set; }

    /// <summary>
    /// Additional metadata for the metric (stored as JSON)
    /// </summary>
    public string Metadata { get; private set; } = "{}";

    /// <summary>
    /// Navigation property to the tenant
    /// </summary>
    public Tenant Tenant { get; private set; } = null!;

    // Private constructor for EF Core
    private TenantUsageMetric() { }

    /// <summary>
    /// Creates a new tenant usage metric
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="metricName">The metric name</param>
    /// <param name="value">The metric value</param>
    /// <param name="tags">Optional tags</param>
    /// <param name="recordedBy">The user who recorded the metric</param>
    /// <param name="metadata">Additional metadata</param>
    /// <returns>A new tenant usage metric instance</returns>
    public static TenantUsageMetric Create(Guid tenantId, string metricName, double value, 
        string? tags = null, Guid? recordedBy = null, string? metadata = null)
    {
        return new TenantUsageMetric(tenantId, metricName, value, tags, recordedBy, metadata);
    }

    /// <summary>
    /// Creates a new tenant usage metric
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="metricName">The metric name</param>
    /// <param name="value">The metric value</param>
    /// <param name="tags">Optional tags</param>
    /// <param name="recordedBy">The user who recorded the metric</param>
    /// <param name="metadata">Additional metadata</param>
    private TenantUsageMetric(Guid tenantId, string metricName, double value, 
        string? tags = null, Guid? recordedBy = null, string? metadata = null)
    {
        TenantId = tenantId;
        MetricName = metricName?.Trim() ?? throw new ArgumentNullException(nameof(metricName));
        Value = value;
        Tags = tags?.Trim() ?? "{}";
        RecordedAt = DateTime.UtcNow;
        RecordedBy = recordedBy;
        Metadata = metadata?.Trim() ?? "{}";

        if (string.IsNullOrWhiteSpace(MetricName))
            throw new ArgumentException("Metric name cannot be empty.", nameof(metricName));

        ValidateMetricName(MetricName);
    }

    /// <summary>
    /// Updates the metric value
    /// </summary>
    /// <param name="value">The new value</param>
    /// <param name="tags">Optional new tags</param>
    /// <param name="metadata">Optional new metadata</param>
    public void UpdateValue(double value, string? tags = null, string? metadata = null)
    {
        Value = value;
        
        if (tags != null)
        {
            Tags = tags.Trim();
        }

        if (metadata != null)
        {
            Metadata = metadata.Trim();
        }
    }

    /// <summary>
    /// Validates the metric name format
    /// </summary>
    /// <param name="metricName">The metric name to validate</param>
    private static void ValidateMetricName(string metricName)
    {
        if (metricName.Length > 100)
            throw new ArgumentException("Metric name cannot exceed 100 characters.", nameof(metricName));

        if (!System.Text.RegularExpressions.Regex.IsMatch(metricName, @"^[a-zA-Z][a-zA-Z0-9._-]*$"))
            throw new ArgumentException("Metric name must start with a letter and can only contain letters, numbers, dots, underscores, and hyphens.", nameof(metricName));
    }
}