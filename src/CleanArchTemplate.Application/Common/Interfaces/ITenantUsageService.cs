using CleanArchTemplate.Application.Common.Models;

namespace CleanArchTemplate.Application.Common.Interfaces;

/// <summary>
/// Interface for tenant usage metrics and analytics
/// </summary>
public interface ITenantUsageService
{
    /// <summary>
    /// Records a usage metric for the current tenant
    /// </summary>
    /// <param name="metricName">The metric name</param>
    /// <param name="value">The metric value</param>
    /// <param name="tags">Optional tags for the metric</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RecordUsageAsync(string metricName, double value, Dictionary<string, string>? tags = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a usage metric for a specific tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="metricName">The metric name</param>
    /// <param name="value">The metric value</param>
    /// <param name="tags">Optional tags for the metric</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RecordUsageAsync(Guid tenantId, string metricName, double value, Dictionary<string, string>? tags = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets usage statistics for the current tenant
    /// </summary>
    /// <param name="metricName">The metric name</param>
    /// <param name="startDate">The start date for the statistics</param>
    /// <param name="endDate">The end date for the statistics</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Usage statistics</returns>
    Task<TenantUsageStatistics> GetUsageStatisticsAsync(string metricName, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets usage statistics for a specific tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="metricName">The metric name</param>
    /// <param name="startDate">The start date for the statistics</param>
    /// <param name="endDate">The end date for the statistics</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Usage statistics</returns>
    Task<TenantUsageStatistics> GetUsageStatisticsAsync(Guid tenantId, string metricName, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all usage metrics for the current tenant
    /// </summary>
    /// <param name="startDate">The start date for the metrics</param>
    /// <param name="endDate">The end date for the metrics</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of usage metrics</returns>
    Task<IEnumerable<TenantUsageMetric>> GetAllUsageMetricsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all usage metrics for a specific tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="startDate">The start date for the metrics</param>
    /// <param name="endDate">The end date for the metrics</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of usage metrics</returns>
    Task<IEnumerable<TenantUsageMetric>> GetAllUsageMetricsAsync(Guid tenantId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tenant analytics summary
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="startDate">The start date for the analytics</param>
    /// <param name="endDate">The end date for the analytics</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Analytics summary</returns>
    Task<TenantAnalyticsSummary> GetAnalyticsSummaryAsync(Guid tenantId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}