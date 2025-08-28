using CleanArchTemplate.Domain.Entities;

namespace CleanArchTemplate.Domain.Interfaces;

/// <summary>
/// Repository interface for tenant usage metric operations
/// </summary>
public interface ITenantUsageMetricRepository : IRepository<TenantUsageMetric>
{
    /// <summary>
    /// Gets usage metrics for a tenant and metric name within a date range
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="metricName">The metric name</param>
    /// <param name="startDate">The start date</param>
    /// <param name="endDate">The end date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of usage metrics</returns>
    Task<IEnumerable<TenantUsageMetric>> GetByTenantAndMetricAsync(Guid tenantId, string metricName, 
        DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all usage metrics for a tenant within a date range
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="startDate">The start date</param>
    /// <param name="endDate">The end date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of usage metrics</returns>
    Task<IEnumerable<TenantUsageMetric>> GetByTenantAsync(Guid tenantId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets usage statistics for a tenant and metric name within a date range
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="metricName">The metric name</param>
    /// <param name="startDate">The start date</param>
    /// <param name="endDate">The end date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Usage statistics</returns>
    Task<(long Count, double Sum, double Average, double Min, double Max, DateTime? FirstRecorded, DateTime? LastRecorded)> 
        GetStatisticsAsync(Guid tenantId, string metricName, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets distinct metric names for a tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of metric names</returns>
    Task<IEnumerable<string>> GetMetricNamesAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets daily aggregated metrics for a tenant and metric name within a date range
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="metricName">The metric name</param>
    /// <param name="startDate">The start date</param>
    /// <param name="endDate">The end date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of date to aggregated values</returns>
    Task<Dictionary<DateTime, (long Count, double Sum, double Average)>> GetDailyAggregatesAsync(
        Guid tenantId, string metricName, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes old usage metrics for a tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="olderThan">Remove metrics older than this date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of metrics removed</returns>
    Task<int> RemoveOldMetricsAsync(Guid tenantId, DateTime olderThan, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest metric value for a tenant and metric name
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="metricName">The metric name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The latest metric if found</returns>
    Task<TenantUsageMetric?> GetLatestMetricAsync(Guid tenantId, string metricName, CancellationToken cancellationToken = default);
}