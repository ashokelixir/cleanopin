using AutoMapper;
using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Application.Common.Models;
using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CleanArchTemplate.Infrastructure.MultiTenancy;

/// <summary>
/// Service for tenant usage metrics and analytics
/// </summary>
public class TenantUsageService : ITenantUsageService
{
    private readonly ITenantUsageMetricRepository _usageMetricRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly ILogger<TenantUsageService> _logger;

    public TenantUsageService(
        ITenantUsageMetricRepository usageMetricRepository,
        ITenantContext tenantContext,
        ICurrentUserService currentUserService,
        IMapper mapper,
        ILogger<TenantUsageService> logger)
    {
        _usageMetricRepository = usageMetricRepository;
        _tenantContext = tenantContext;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Records a usage metric for the current tenant
    /// </summary>
    /// <param name="metricName">The metric name</param>
    /// <param name="value">The metric value</param>
    /// <param name="tags">Optional tags for the metric</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task RecordUsageAsync(string metricName, double value, Dictionary<string, string>? tags = null, CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        await RecordUsageAsync(tenantId, metricName, value, tags, cancellationToken);
    }

    /// <summary>
    /// Records a usage metric for a specific tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="metricName">The metric name</param>
    /// <param name="value">The metric value</param>
    /// <param name="tags">Optional tags for the metric</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task RecordUsageAsync(Guid tenantId, string metricName, double value, Dictionary<string, string>? tags = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var tagsJson = tags != null ? JsonSerializer.Serialize(tags) : "{}";
            var recordedBy = Guid.TryParse(_currentUserService.UserId, out var userId) ? userId : (Guid?)null;
            
            var usageMetric = Domain.Entities.TenantUsageMetric.Create(tenantId, metricName, value, tagsJson, recordedBy, null);
            await _usageMetricRepository.AddAsync(usageMetric, cancellationToken);

            _logger.LogDebug("Recorded usage metric {MetricName} with value {Value} for tenant {TenantId}", metricName, value, tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording usage metric {MetricName} for tenant {TenantId}", metricName, tenantId);
            throw;
        }
    }

    /// <summary>
    /// Gets usage statistics for the current tenant
    /// </summary>
    /// <param name="metricName">The metric name</param>
    /// <param name="startDate">The start date for the statistics</param>
    /// <param name="endDate">The end date for the statistics</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Usage statistics</returns>
    public async Task<TenantUsageStatistics> GetUsageStatisticsAsync(string metricName, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        return await GetUsageStatisticsAsync(tenantId, metricName, startDate, endDate, cancellationToken);
    }

    /// <summary>
    /// Gets usage statistics for a specific tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="metricName">The metric name</param>
    /// <param name="startDate">The start date for the statistics</param>
    /// <param name="endDate">The end date for the statistics</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Usage statistics</returns>
    public async Task<TenantUsageStatistics> GetUsageStatisticsAsync(Guid tenantId, string metricName, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var (count, sum, average, min, max, firstRecorded, lastRecorded) = 
                await _usageMetricRepository.GetStatisticsAsync(tenantId, metricName, startDate, endDate, cancellationToken);

            var statistics = new TenantUsageStatistics
            {
                TenantId = tenantId,
                MetricName = metricName,
                StartDate = startDate,
                EndDate = endDate,
                Count = count,
                Sum = sum,
                Average = average,
                Minimum = min,
                Maximum = max,
                FirstRecordedAt = firstRecorded,
                LastRecordedAt = lastRecorded
            };

            // Get first and last values
            if (firstRecorded.HasValue)
            {
                var firstMetric = await _usageMetricRepository.GetByTenantAndMetricAsync(tenantId, metricName, firstRecorded.Value, firstRecorded.Value.AddSeconds(1), cancellationToken);
                statistics.FirstValue = firstMetric.FirstOrDefault()?.Value;
            }

            if (lastRecorded.HasValue)
            {
                var lastMetric = await _usageMetricRepository.GetByTenantAndMetricAsync(tenantId, metricName, lastRecorded.Value.AddSeconds(-1), lastRecorded.Value, cancellationToken);
                statistics.LastValue = lastMetric.LastOrDefault()?.Value;
            }

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting usage statistics for metric {MetricName} and tenant {TenantId}", metricName, tenantId);
            throw;
        }
    }

    /// <summary>
    /// Gets all usage metrics for the current tenant
    /// </summary>
    /// <param name="startDate">The start date for the metrics</param>
    /// <param name="endDate">The end date for the metrics</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of usage metrics</returns>
    public async Task<IEnumerable<Application.Common.Models.TenantUsageMetric>> GetAllUsageMetricsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        return await GetAllUsageMetricsAsync(tenantId, startDate, endDate, cancellationToken);
    }

    /// <summary>
    /// Gets all usage metrics for a specific tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="startDate">The start date for the metrics</param>
    /// <param name="endDate">The end date for the metrics</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of usage metrics</returns>
    public async Task<IEnumerable<Application.Common.Models.TenantUsageMetric>> GetAllUsageMetricsAsync(Guid tenantId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var metrics = await _usageMetricRepository.GetByTenantAsync(tenantId, startDate, endDate, cancellationToken);
            return _mapper.Map<IEnumerable<Application.Common.Models.TenantUsageMetric>>(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all usage metrics for tenant {TenantId}", tenantId);
            throw;
        }
    }

    /// <summary>
    /// Gets tenant analytics summary
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="startDate">The start date for the analytics</param>
    /// <param name="endDate">The end date for the analytics</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Analytics summary</returns>
    public async Task<TenantAnalyticsSummary> GetAnalyticsSummaryAsync(Guid tenantId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var summary = new TenantAnalyticsSummary
            {
                TenantId = tenantId,
                StartDate = startDate,
                EndDate = endDate
            };

            // Get all metrics for the period
            var allMetrics = await _usageMetricRepository.GetByTenantAsync(tenantId, startDate, endDate, cancellationToken);
            var metricsByName = allMetrics.GroupBy(m => m.MetricName).ToDictionary(g => g.Key, g => g.ToList());

            // Calculate basic statistics
            if (metricsByName.ContainsKey("active_users"))
            {
                var activeUserMetrics = metricsByName["active_users"];
                summary.ActiveUsers = (int)(activeUserMetrics.LastOrDefault()?.Value ?? 0.0);
            }

            if (metricsByName.ContainsKey("api_requests"))
            {
                var apiRequestMetrics = metricsByName["api_requests"];
                summary.TotalApiRequests = (long)apiRequestMetrics.Sum(m => m.Value);
            }

            if (metricsByName.ContainsKey("storage_used"))
            {
                var storageMetrics = metricsByName["storage_used"];
                summary.StorageUsed = (long)(storageMetrics.LastOrDefault()?.Value ?? 0.0);
            }

            if (metricsByName.ContainsKey("bandwidth_used"))
            {
                var bandwidthMetrics = metricsByName["bandwidth_used"];
                summary.BandwidthUsed = (long)bandwidthMetrics.Sum(m => m.Value);
            }

            if (metricsByName.ContainsKey("response_time"))
            {
                var responseTimeMetrics = metricsByName["response_time"];
                summary.AverageResponseTime = responseTimeMetrics.Any() ? responseTimeMetrics.Average(m => m.Value) : 0;
            }

            if (metricsByName.ContainsKey("error_count"))
            {
                var errorMetrics = metricsByName["error_count"];
                var totalRequests = summary.TotalApiRequests;
                var totalErrors = (long)errorMetrics.Sum(m => m.Value);
                summary.ErrorRate = totalRequests > 0 ? (double)totalErrors / totalRequests * 100 : 0;
            }

            // Calculate feature usage
            var featureMetrics = metricsByName.Where(kvp => kvp.Key.StartsWith("feature_")).ToDictionary(kvp => kvp.Key, kvp => (long)kvp.Value.Sum(m => m.Value));
            summary.FeatureUsage = featureMetrics;
            summary.EnabledFeatures = featureMetrics.Count;

            // Calculate daily usage
            var dailyUsage = new Dictionary<DateTime, TenantDailyUsage>();
            var currentDate = startDate.Date;
            
            while (currentDate <= endDate.Date)
            {
                var dayMetrics = allMetrics.Where(m => m.RecordedAt.Date == currentDate).ToList();
                
                var dailyStats = new TenantDailyUsage
                {
                    Date = currentDate,
                    ActiveUsers = (int)(dayMetrics.Where(m => m.MetricName == "active_users").LastOrDefault()?.Value ?? 0.0),
                    ApiRequests = (long)dayMetrics.Where(m => m.MetricName == "api_requests").Sum(m => m.Value),
                    StorageUsed = (long)(dayMetrics.Where(m => m.MetricName == "storage_used").LastOrDefault()?.Value ?? 0.0),
                    BandwidthUsed = (long)dayMetrics.Where(m => m.MetricName == "bandwidth_used").Sum(m => m.Value),
                    AverageResponseTime = dayMetrics.Where(m => m.MetricName == "response_time").Any() 
                        ? dayMetrics.Where(m => m.MetricName == "response_time").Average(m => m.Value) : 0,
                    ErrorCount = (long)dayMetrics.Where(m => m.MetricName == "error_count").Sum(m => m.Value)
                };

                dailyUsage[currentDate] = dailyStats;
                currentDate = currentDate.AddDays(1);
            }

            summary.DailyUsage = dailyUsage;

            // Calculate top metrics
            var topMetrics = new Dictionary<string, TenantUsageStatistics>();
            foreach (var metricGroup in metricsByName.Take(10)) // Top 10 metrics
            {
                var stats = await GetUsageStatisticsAsync(tenantId, metricGroup.Key, startDate, endDate, cancellationToken);
                topMetrics[metricGroup.Key] = stats;
            }

            summary.TopMetrics = topMetrics;

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting analytics summary for tenant {TenantId}", tenantId);
            throw;
        }
    }

    /// <summary>
    /// Gets the current tenant ID from the tenant context
    /// </summary>
    /// <returns>The current tenant ID</returns>
    private Guid GetCurrentTenantId()
    {
        if (_tenantContext.CurrentTenant == null)
        {
            throw new InvalidOperationException("No tenant context available");
        }

        return _tenantContext.CurrentTenant.Id;
    }
}