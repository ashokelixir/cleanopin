namespace CleanArchTemplate.Application.Common.Models;

/// <summary>
/// Represents a tenant usage metric record
/// </summary>
public class TenantUsageMetric
{
    /// <summary>
    /// The unique identifier for the metric record
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The tenant ID
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// The metric name
    /// </summary>
    public string MetricName { get; set; } = string.Empty;

    /// <summary>
    /// The metric value
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    /// Optional tags for the metric
    /// </summary>
    public Dictionary<string, string> Tags { get; set; } = new();

    /// <summary>
    /// The timestamp when the metric was recorded
    /// </summary>
    public DateTime RecordedAt { get; set; }

    /// <summary>
    /// The user who recorded the metric (if applicable)
    /// </summary>
    public Guid? RecordedBy { get; set; }

    /// <summary>
    /// Additional metadata for the metric
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}