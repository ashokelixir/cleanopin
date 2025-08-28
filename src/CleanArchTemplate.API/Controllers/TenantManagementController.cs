using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchTemplate.API.Controllers;

/// <summary>
/// Controller for tenant management operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TenantManagementController : ControllerBase
{
    private readonly ITenantService _tenantService;
    private readonly ITenantConfigurationService _configurationService;
    private readonly ITenantFeatureService _featureService;
    private readonly ITenantUsageService _usageService;
    private readonly ILogger<TenantManagementController> _logger;

    public TenantManagementController(
        ITenantService tenantService,
        ITenantConfigurationService configurationService,
        ITenantFeatureService featureService,
        ITenantUsageService usageService,
        ILogger<TenantManagementController> logger)
    {
        _tenantService = tenantService;
        _configurationService = configurationService;
        _featureService = featureService;
        _usageService = usageService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new tenant
    /// </summary>
    /// <param name="request">The tenant creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created tenant information</returns>
    [HttpPost]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<TenantInfo>> CreateTenant([FromBody] CreateTenantRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var tenant = await _tenantService.CreateTenantAsync(
                request.Name, 
                request.Identifier, 
                request.ConnectionString, 
                request.Configuration, 
                cancellationToken);

            _logger.LogInformation("Tenant created: {TenantId} ({Name})", tenant.Id, tenant.Name);
            return CreatedAtAction(nameof(GetTenant), new { id = tenant.Id }, tenant);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create tenant: {Message}", ex.Message);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Gets a tenant by ID
    /// </summary>
    /// <param name="id">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tenant information</returns>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "SystemAdmin,TenantAdmin")]
    public async Task<ActionResult<TenantInfo>> GetTenant(Guid id, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantService.GetTenantAsync(id, cancellationToken);
        if (tenant == null)
        {
            return NotFound($"Tenant with ID {id} not found");
        }

        return Ok(tenant);
    }

    /// <summary>
    /// Gets all tenants
    /// </summary>
    /// <param name="activeOnly">Whether to return only active tenants</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of tenants</returns>
    [HttpGet]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<IEnumerable<TenantInfo>>> GetAllTenants([FromQuery] bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        var tenants = await _tenantService.GetAllTenantsAsync(activeOnly, cancellationToken);
        return Ok(tenants);
    }

    /// <summary>
    /// Updates a tenant
    /// </summary>
    /// <param name="id">The tenant ID</param>
    /// <param name="request">The tenant update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated tenant information</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SystemAdmin,TenantAdmin")]
    public async Task<ActionResult<TenantInfo>> UpdateTenant(Guid id, [FromBody] UpdateTenantRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var tenant = await _tenantService.UpdateTenantAsync(id, request.Name, request.Configuration, cancellationToken);
            _logger.LogInformation("Tenant updated: {TenantId} ({Name})", tenant.Id, tenant.Name);
            return Ok(tenant);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to update tenant {TenantId}: {Message}", id, ex.Message);
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Activates a tenant
    /// </summary>
    /// <param name="id">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response</returns>
    [HttpPost("{id:guid}/activate")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<IActionResult> ActivateTenant(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _tenantService.ActivateTenantAsync(id, cancellationToken);
            _logger.LogInformation("Tenant activated: {TenantId}", id);
            return Ok(new { Message = "Tenant activated successfully" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to activate tenant {TenantId}: {Message}", id, ex.Message);
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Deactivates a tenant
    /// </summary>
    /// <param name="id">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response</returns>
    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<IActionResult> DeactivateTenant(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _tenantService.DeactivateTenantAsync(id, cancellationToken);
            _logger.LogInformation("Tenant deactivated: {TenantId}", id);
            return Ok(new { Message = "Tenant deactivated successfully" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to deactivate tenant {TenantId}: {Message}", id, ex.Message);
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Sets tenant subscription expiry
    /// </summary>
    /// <param name="id">The tenant ID</param>
    /// <param name="request">The subscription expiry request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response</returns>
    [HttpPost("{id:guid}/subscription")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<IActionResult> SetSubscriptionExpiry(Guid id, [FromBody] SetSubscriptionExpiryRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            await _tenantService.SetSubscriptionExpiryAsync(id, request.ExpiresAt, cancellationToken);
            _logger.LogInformation("Subscription expiry set for tenant {TenantId}: {ExpiresAt}", id, request.ExpiresAt);
            return Ok(new { Message = "Subscription expiry set successfully" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to set subscription expiry for tenant {TenantId}: {Message}", id, ex.Message);
            return NotFound(ex.Message);
        }
    }
}

/// <summary>
/// Request model for creating a tenant
/// </summary>
public class CreateTenantRequest
{
    /// <summary>
    /// The tenant name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The tenant identifier
    /// </summary>
    public string Identifier { get; set; } = string.Empty;

    /// <summary>
    /// Optional connection string
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Optional configuration
    /// </summary>
    public Dictionary<string, object>? Configuration { get; set; }
}

/// <summary>
/// Request model for updating a tenant
/// </summary>
public class UpdateTenantRequest
{
    /// <summary>
    /// The tenant name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional configuration
    /// </summary>
    public Dictionary<string, object>? Configuration { get; set; }
}

/// <summary>
/// Request model for setting subscription expiry
/// </summary>
public class SetSubscriptionExpiryRequest
{
    /// <summary>
    /// The expiry date
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}