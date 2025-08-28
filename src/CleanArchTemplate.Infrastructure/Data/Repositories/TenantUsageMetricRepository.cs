using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.Interfaces;
using CleanArchTemplate.Infrastructure.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace CleanArchTemplate.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for tenant usage metric operations
/// </summary>
public class TenantUsageMetricRepository : BaseRepository<TenantUsageMetric>, ITenantUsageMetricRepository
{
    public TenantUsageMetricRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Gets usage metrics for a tenant and metric name within a date range
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="metricName">The metric name</param>
    /// <param name="startDate">The start date</param>
    /// <param name="endDate">The end date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of usage metrics</returns>
    public async Task<IEnumerable<TenantUsageMetric>> GetByTenantAndMetricAsync(Guid tenantId, string metricName, 
        DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.Set<TenantUsageMetric>()
            .Where(m => m.TenantId == tenantId && 
                       m.MetricName == metricName && 
                       m.RecordedAt >= startDate && 
                       m.RecordedAt <= endDate)
            .OrderBy(m => m.RecordedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets all usage metrics for a tenant within a date range
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="startDate">The start date</param>
    /// <param name="endDate">The end date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of usage metrics</returns>
    public async Task<IEnumerable<TenantUsageMetric>> GetByTenantAsync(Guid tenantId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.Set<TenantUsageMetric>()
            .Where(m => m.TenantId == tenantId && 
                       m.RecordedAt >= startDate && 
                       m.RecordedAt <= endDate)
            .OrderBy(m => m.RecordedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets usage statistics for a tenant and metric name within a date range
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="metricName">The metric name</param>
    /// <param name="startDate">The start date</param>
    /// <param name="endDate">The end date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Usage statistics</returns>
    public async Task<(long Count, double Sum, double Average, double Min, double Max, DateTime? FirstRecorded, DateTime? LastRecorded)> 
        GetStatisticsAsync(Guid tenantId, string metricName, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var metrics = await _context.Set<TenantUsageMetric>()
            .Where(m => m.TenantId == tenantId && 
                       m.MetricName == metricName && 
                       m.RecordedAt >= startDate && 
                       m.RecordedAt <= endDate)
            .ToListAsync(cancellationToken);

        if (!metrics.Any())
        {
            return (0, 0, 0, 0, 0, null, null);
        }

        var count = metrics.Count;
        var sum = metrics.Sum(m => m.Value);
        var average = sum / count;
        var min = metrics.Min(m => m.Value);
        var max = metrics.Max(m => m.Value);
        var firstRecorded = metrics.Min(m => m.RecordedAt);
        var lastRecorded = metrics.Max(m => m.RecordedAt);

        return (count, sum, average, min, max, firstRecorded, lastRecorded);
    }

    /// <summary>
    /// Gets distinct metric names for a tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of metric names</returns>
    public async Task<IEnumerable<string>> GetMetricNamesAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<TenantUsageMetric>()
            .Where(m => m.TenantId == tenantId)
            .Select(m => m.MetricName)
            .Distinct()
            .OrderBy(name => name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets daily aggregated metrics for a tenant and metric name within a date range
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="metricName">The metric name</param>
    /// <param name="startDate">The start date</param>
    /// <param name="endDate">The end date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of date to aggregated values</returns>
    public async Task<Dictionary<DateTime, (long Count, double Sum, double Average)>> GetDailyAggregatesAsync(
        Guid tenantId, string metricName, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var metrics = await _context.Set<TenantUsageMetric>()
            .Where(m => m.TenantId == tenantId && 
                       m.MetricName == metricName && 
                       m.RecordedAt >= startDate && 
                       m.RecordedAt <= endDate)
            .ToListAsync(cancellationToken);

        return metrics
            .GroupBy(m => m.RecordedAt.Date)
            .ToDictionary(
                g => g.Key,
                g => ((long)g.Count(), g.Sum(m => m.Value), g.Average(m => m.Value))
            );
    }

    /// <summary>
    /// Removes old usage metrics for a tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="olderThan">Remove metrics older than this date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of metrics removed</returns>
    public async Task<int> RemoveOldMetricsAsync(Guid tenantId, DateTime olderThan, CancellationToken cancellationToken = default)
    {
        var metricsToRemove = await _context.Set<TenantUsageMetric>()
            .Where(m => m.TenantId == tenantId && m.RecordedAt < olderThan)
            .ToListAsync(cancellationToken);

        if (metricsToRemove.Any())
        {
            _context.Set<TenantUsageMetric>().RemoveRange(metricsToRemove);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return metricsToRemove.Count;
    }

    /// <summary>
    /// Gets the latest metric value for a tenant and metric name
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="metricName">The metric name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The latest metric if found</returns>
    public async Task<TenantUsageMetric?> GetLatestMetricAsync(Guid tenantId, string metricName, CancellationToken cancellationToken = default)
    {
        return await _context.Set<TenantUsageMetric>()
            .Where(m => m.TenantId == tenantId && m.MetricName == metricName)
            .OrderByDescending(m => m.RecordedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
}