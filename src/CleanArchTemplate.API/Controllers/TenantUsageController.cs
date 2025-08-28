using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchTemplate.API.Controllers;

/// <summary>
/// Controller for tenant usage metrics and analytics
/// </summary>
[ApiController]
[Route("api/tenant/usage")]
[Authorize]
public class TenantUsageController : ControllerBase
{
    private readonly ITenantUsageService _usageService;
    private readonly ILogger<TenantUsageController> _logger;

    public TenantUsageController(
        ITenantUsageService usageService,
        ILogger<TenantUsageController> logger)
    {
        _usageService = usageService;
        _logger = logger;
    }

    /// <summary>
    /// Records a usage metric
    /// </summary>
    /// <param name="request">The usage metric request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response</returns>
    [HttpPost("metrics")]
    public async Task<IActionResult> RecordUsage([FromBody] RecordUsageRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            await _usageService.RecordUsageAsync(request.MetricName, request.Value, request.Tags, cancellationToken);
            _logger.LogDebug("Usage metric recorded: {MetricName} = {Value}", request.MetricName, request.Value);
            return Ok(new { Message = "Usage metric recorded successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording usage metric {MetricName}", request.MetricName);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets usage statistics for a metric
    /// </summary>
    /// <param name="metricName">The metric name</param>
    /// <param name="startDate">The start date</param>
    /// <param name="endDate">The end date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Usage statistics</returns>
    [HttpGet("statistics/{metricName}")]
    public async Task<ActionResult<TenantUsageStatistics>> GetUsageStatistics(
        string metricName, 
        [FromQuery] DateTime startDate, 
        [FromQuery] DateTime endDate, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var statistics = await _usageService.GetUsageStatisticsAsync(metricName, startDate, endDate, cancellationToken);
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting usage statistics for metric {MetricName}", metricName);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets all usage metrics for a date range
    /// </summary>
    /// <param name="startDate">The start date</param>
    /// <param name="endDate">The end date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of usage metrics</returns>
    [HttpGet("metrics")]
    public async Task<ActionResult<IEnumerable<TenantUsageMetric>>> GetAllUsageMetrics(
        [FromQuery] DateTime startDate, 
        [FromQuery] DateTime endDate, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metrics = await _usageService.GetAllUsageMetricsAsync(startDate, endDate, cancellationToken);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all usage metrics");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets analytics summary for a tenant (system admin only)
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="startDate">The start date</param>
    /// <param name="endDate">The end date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Analytics summary</returns>
    [HttpGet("analytics/{tenantId:guid}")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<TenantAnalyticsSummary>> GetAnalyticsSummary(
        Guid tenantId,
        [FromQuery] DateTime startDate, 
        [FromQuery] DateTime endDate, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var summary = await _usageService.GetAnalyticsSummaryAsync(tenantId, startDate, endDate, cancellationToken);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting analytics summary for tenant {TenantId}", tenantId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Records a usage metric for a specific tenant (system admin only)
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="request">The usage metric request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response</returns>
    [HttpPost("tenant/{tenantId:guid}/metrics")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<IActionResult> RecordUsageForTenant(Guid tenantId, [FromBody] RecordUsageRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            await _usageService.RecordUsageAsync(tenantId, request.MetricName, request.Value, request.Tags, cancellationToken);
            _logger.LogDebug("Usage metric recorded for tenant {TenantId}: {MetricName} = {Value}", tenantId, request.MetricName, request.Value);
            return Ok(new { Message = "Usage metric recorded successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording usage metric {MetricName} for tenant {TenantId}", request.MetricName, tenantId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets usage statistics for a metric and tenant (system admin only)
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="metricName">The metric name</param>
    /// <param name="startDate">The start date</param>
    /// <param name="endDate">The end date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Usage statistics</returns>
    [HttpGet("tenant/{tenantId:guid}/statistics/{metricName}")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<TenantUsageStatistics>> GetUsageStatisticsForTenant(
        Guid tenantId,
        string metricName, 
        [FromQuery] DateTime startDate, 
        [FromQuery] DateTime endDate, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var statistics = await _usageService.GetUsageStatisticsAsync(tenantId, metricName, startDate, endDate, cancellationToken);
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting usage statistics for metric {MetricName} and tenant {TenantId}", metricName, tenantId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets all usage metrics for a tenant and date range (system admin only)
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="startDate">The start date</param>
    /// <param name="endDate">The end date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of usage metrics</returns>
    [HttpGet("tenant/{tenantId:guid}/metrics")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<IEnumerable<TenantUsageMetric>>> GetAllUsageMetricsForTenant(
        Guid tenantId,
        [FromQuery] DateTime startDate, 
        [FromQuery] DateTime endDate, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metrics = await _usageService.GetAllUsageMetricsAsync(tenantId, startDate, endDate, cancellationToken);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all usage metrics for tenant {TenantId}", tenantId);
            return StatusCode(500, "Internal server error");
        }
    }
}

/// <summary>
/// Request model for recording usage
/// </summary>
public class RecordUsageRequest
{
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
    public Dictionary<string, string>? Tags { get; set; }
}