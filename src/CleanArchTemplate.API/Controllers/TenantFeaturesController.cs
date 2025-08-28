using CleanArchTemplate.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchTemplate.API.Controllers;

/// <summary>
/// Controller for tenant feature flag management
/// </summary>
[ApiController]
[Route("api/tenant/features")]
[Authorize]
public class TenantFeaturesController : ControllerBase
{
    private readonly ITenantFeatureService _featureService;
    private readonly ILogger<TenantFeaturesController> _logger;

    public TenantFeaturesController(
        ITenantFeatureService featureService,
        ILogger<TenantFeaturesController> logger)
    {
        _featureService = featureService;
        _logger = logger;
    }

    /// <summary>
    /// Checks if a feature is enabled
    /// </summary>
    /// <param name="featureName">The feature name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Feature status</returns>
    [HttpGet("{featureName}/enabled")]
    public async Task<ActionResult<bool>> IsFeatureEnabled(string featureName, CancellationToken cancellationToken = default)
    {
        try
        {
            var isEnabled = await _featureService.IsFeatureEnabledAsync(featureName, cancellationToken);
            return Ok(new { FeatureName = featureName, Enabled = isEnabled });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if feature {FeatureName} is enabled", featureName);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets feature configuration
    /// </summary>
    /// <param name="featureName">The feature name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Feature configuration</returns>
    [HttpGet("{featureName}/configuration")]
    public async Task<ActionResult<object>> GetFeatureConfiguration(string featureName, CancellationToken cancellationToken = default)
    {
        try
        {
            var configuration = await _featureService.GetFeatureConfigurationAsync<object?>(featureName, null, cancellationToken);
            return Ok(new { FeatureName = featureName, Configuration = configuration });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting feature configuration for {FeatureName}", featureName);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets all enabled features
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>All enabled features</returns>
    [HttpGet("enabled")]
    public async Task<ActionResult<Dictionary<string, object?>>> GetAllEnabledFeatures(CancellationToken cancellationToken = default)
    {
        try
        {
            var features = await _featureService.GetAllEnabledFeaturesAsync(cancellationToken);
            return Ok(features);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all enabled features");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Enables a feature
    /// </summary>
    /// <param name="featureName">The feature name</param>
    /// <param name="request">The feature enable request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response</returns>
    [HttpPost("{featureName}/enable")]
    [Authorize(Roles = "TenantAdmin,SystemAdmin")]
    public async Task<IActionResult> EnableFeature(string featureName, [FromBody] EnableFeatureRequest? request = null, CancellationToken cancellationToken = default)
    {
        try
        {
            await _featureService.EnableFeatureAsync(featureName, request?.Configuration, cancellationToken);
            _logger.LogInformation("Feature enabled: {FeatureName}", featureName);
            return Ok(new { Message = "Feature enabled successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling feature {FeatureName}", featureName);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Disables a feature
    /// </summary>
    /// <param name="featureName">The feature name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response</returns>
    [HttpPost("{featureName}/disable")]
    [Authorize(Roles = "TenantAdmin,SystemAdmin")]
    public async Task<IActionResult> DisableFeature(string featureName, CancellationToken cancellationToken = default)
    {
        try
        {
            await _featureService.DisableFeatureAsync(featureName, cancellationToken);
            _logger.LogInformation("Feature disabled: {FeatureName}", featureName);
            return Ok(new { Message = "Feature disabled successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling feature {FeatureName}", featureName);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Checks if a feature is enabled for a specific tenant (system admin only)
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="featureName">The feature name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Feature status</returns>
    [HttpGet("tenant/{tenantId:guid}/{featureName}/enabled")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<bool>> IsFeatureEnabledForTenant(Guid tenantId, string featureName, CancellationToken cancellationToken = default)
    {
        try
        {
            var isEnabled = await _featureService.IsFeatureEnabledAsync(tenantId, featureName, cancellationToken);
            return Ok(new { TenantId = tenantId, FeatureName = featureName, Enabled = isEnabled });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if feature {FeatureName} is enabled for tenant {TenantId}", featureName, tenantId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Enables a feature for a specific tenant (system admin only)
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="featureName">The feature name</param>
    /// <param name="request">The feature enable request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response</returns>
    [HttpPost("tenant/{tenantId:guid}/{featureName}/enable")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<IActionResult> EnableFeatureForTenant(Guid tenantId, string featureName, [FromBody] EnableFeatureRequest? request = null, CancellationToken cancellationToken = default)
    {
        try
        {
            await _featureService.EnableFeatureAsync(tenantId, featureName, request?.Configuration, cancellationToken);
            _logger.LogInformation("Feature enabled for tenant {TenantId}: {FeatureName}", tenantId, featureName);
            return Ok(new { Message = "Feature enabled successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling feature {FeatureName} for tenant {TenantId}", featureName, tenantId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Disables a feature for a specific tenant (system admin only)
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="featureName">The feature name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response</returns>
    [HttpPost("tenant/{tenantId:guid}/{featureName}/disable")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<IActionResult> DisableFeatureForTenant(Guid tenantId, string featureName, CancellationToken cancellationToken = default)
    {
        try
        {
            await _featureService.DisableFeatureAsync(tenantId, featureName, cancellationToken);
            _logger.LogInformation("Feature disabled for tenant {TenantId}: {FeatureName}", tenantId, featureName);
            return Ok(new { Message = "Feature disabled successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling feature {FeatureName} for tenant {TenantId}", featureName, tenantId);
            return StatusCode(500, "Internal server error");
        }
    }
}

/// <summary>
/// Request model for enabling a feature
/// </summary>
public class EnableFeatureRequest
{
    /// <summary>
    /// Optional feature configuration
    /// </summary>
    public object? Configuration { get; set; }
}