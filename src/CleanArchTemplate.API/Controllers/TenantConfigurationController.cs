using CleanArchTemplate.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchTemplate.API.Controllers;

/// <summary>
/// Controller for tenant configuration management
/// </summary>
[ApiController]
[Route("api/tenant/configuration")]
[Authorize]
public class TenantConfigurationController : ControllerBase
{
    private readonly ITenantConfigurationService _configurationService;
    private readonly ILogger<TenantConfigurationController> _logger;

    public TenantConfigurationController(
        ITenantConfigurationService configurationService,
        ILogger<TenantConfigurationController> logger)
    {
        _configurationService = configurationService;
        _logger = logger;
    }

    /// <summary>
    /// Gets a configuration value
    /// </summary>
    /// <param name="key">The configuration key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The configuration value</returns>
    [HttpGet("{key}")]
    public async Task<ActionResult<object>> GetConfiguration(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await _configurationService.GetConfigurationAsync<object?>(key, null, cancellationToken);
            if (value == null)
            {
                return NotFound($"Configuration '{key}' not found");
            }

            return Ok(new { Key = key, Value = value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configuration {Key}", key);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets all configuration values
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>All configuration values</returns>
    [HttpGet]
    public async Task<ActionResult<Dictionary<string, object>>> GetAllConfiguration(CancellationToken cancellationToken = default)
    {
        try
        {
            var configurations = await _configurationService.GetAllConfigurationAsync(cancellationToken);
            return Ok(configurations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all configurations");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Sets a configuration value
    /// </summary>
    /// <param name="key">The configuration key</param>
    /// <param name="request">The configuration value request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response</returns>
    [HttpPut("{key}")]
    [Authorize(Roles = "TenantAdmin,SystemAdmin")]
    public async Task<IActionResult> SetConfiguration(string key, [FromBody] SetConfigurationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            await _configurationService.SetConfigurationAsync(key, request.Value, cancellationToken);
            _logger.LogInformation("Configuration set: {Key}", key);
            return Ok(new { Message = "Configuration set successfully" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to set configuration {Key}: {Message}", key, ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting configuration {Key}", key);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Removes a configuration value
    /// </summary>
    /// <param name="key">The configuration key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response</returns>
    [HttpDelete("{key}")]
    [Authorize(Roles = "TenantAdmin,SystemAdmin")]
    public async Task<IActionResult> RemoveConfiguration(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _configurationService.RemoveConfigurationAsync(key, cancellationToken);
            _logger.LogInformation("Configuration removed: {Key}", key);
            return Ok(new { Message = "Configuration removed successfully" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to remove configuration {Key}: {Message}", key, ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing configuration {Key}", key);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets a configuration value for a specific tenant (system admin only)
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="key">The configuration key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The configuration value</returns>
    [HttpGet("tenant/{tenantId:guid}/{key}")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<object>> GetTenantConfiguration(Guid tenantId, string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await _configurationService.GetConfigurationAsync<object?>(tenantId, key, null, cancellationToken);
            if (value == null)
            {
                return NotFound($"Configuration '{key}' not found for tenant {tenantId}");
            }

            return Ok(new { TenantId = tenantId, Key = key, Value = value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configuration {Key} for tenant {TenantId}", key, tenantId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Sets a configuration value for a specific tenant (system admin only)
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="key">The configuration key</param>
    /// <param name="request">The configuration value request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response</returns>
    [HttpPut("tenant/{tenantId:guid}/{key}")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<IActionResult> SetTenantConfiguration(Guid tenantId, string key, [FromBody] SetConfigurationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            await _configurationService.SetConfigurationAsync(tenantId, key, request.Value, cancellationToken);
            _logger.LogInformation("Configuration set for tenant {TenantId}: {Key}", tenantId, key);
            return Ok(new { Message = "Configuration set successfully" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to set configuration {Key} for tenant {TenantId}: {Message}", key, tenantId, ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting configuration {Key} for tenant {TenantId}", key, tenantId);
            return StatusCode(500, "Internal server error");
        }
    }
}

/// <summary>
/// Request model for setting configuration
/// </summary>
public class SetConfigurationRequest
{
    /// <summary>
    /// The configuration value
    /// </summary>
    public object Value { get; set; } = null!;
}