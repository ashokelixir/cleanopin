namespace CleanArchTemplate.Application.Common.Models;

/// <summary>
/// Statistics for tenant usage metrics
/// </summary>
public class TenantUsageStatistics
{
    /// <summary>
    /// The tenant ID
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// The metric name
    /// </summary>
    public string MetricName { get; set; } = string.Empty;

    /// <summary>
    /// The start date of the statistics period
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// The end date of the statistics period
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// The total count of metric records
    /// </summary>
    public long Count { get; set; }

    /// <summary>
    /// The sum of all metric values
    /// </summary>
    public double Sum { get; set; }

    /// <summary>
    /// The average of all metric values
    /// </summary>
    public double Average { get; set; }

    /// <summary>
    /// The minimum metric value
    /// </summary>
    public double Minimum { get; set; }

    /// <summary>
    /// The maximum metric value
    /// </summary>
    public double Maximum { get; set; }

    /// <summary>
    /// The first recorded value in the period
    /// </summary>
    public double? FirstValue { get; set; }

    /// <summary>
    /// The last recorded value in the period
    /// </summary>
    public double? LastValue { get; set; }

    /// <summary>
    /// The timestamp of the first record
    /// </summary>
    public DateTime? FirstRecordedAt { get; set; }

    /// <summary>
    /// The timestamp of the last record
    /// </summary>
    public DateTime? LastRecordedAt { get; set; }
}