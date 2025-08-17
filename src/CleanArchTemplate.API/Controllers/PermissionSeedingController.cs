using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.API.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CleanArchTemplate.API.Controllers;

/// <summary>
/// Controller for managing permission seeding operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[RequirePermission("System.Admin")]
public class PermissionSeedingController : ControllerBase
{
    private readonly IPermissionSeedingService _permissionSeedingService;
    private readonly ILogger<PermissionSeedingController> _logger;

    public PermissionSeedingController(
        IPermissionSeedingService permissionSeedingService,
        ILogger<PermissionSeedingController> logger)
    {
        _permissionSeedingService = permissionSeedingService ?? throw new ArgumentNullException(nameof(permissionSeedingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Seeds default permissions for the application
    /// </summary>
    /// <returns>Success result</returns>
    [HttpPost("default-permissions")]
    [SwaggerOperation(
        Summary = "Seed default permissions",
        Description = "Seeds all default permissions required by the application"
    )]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SeedDefaultPermissions(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting default permission seeding via API");
            await _permissionSeedingService.SeedDefaultPermissionsAsync(cancellationToken);
            _logger.LogInformation("Default permission seeding completed successfully");
            
            return Ok(new { Message = "Default permissions seeded successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while seeding default permissions");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { Message = "An error occurred while seeding default permissions" });
        }
    }

    /// <summary>
    /// Seeds environment-specific permissions
    /// </summary>
    /// <param name="environment">Environment name (Development, Staging, Production)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpPost("environment-permissions/{environment}")]
    [SwaggerOperation(
        Summary = "Seed environment-specific permissions",
        Description = "Seeds permissions specific to the given environment"
    )]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SeedEnvironmentPermissions(
        string environment, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(environment))
        {
            return BadRequest(new { Message = "Environment name is required" });
        }

        try
        {
            _logger.LogInformation("Starting environment permission seeding for {Environment} via API", environment);
            await _permissionSeedingService.SeedEnvironmentPermissionsAsync(environment, cancellationToken);
            _logger.LogInformation("Environment permission seeding completed successfully for {Environment}", environment);
            
            return Ok(new { Message = $"Environment permissions for '{environment}' seeded successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while seeding environment permissions for {Environment}", environment);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { Message = $"An error occurred while seeding environment permissions for '{environment}'" });
        }
    }

    /// <summary>
    /// Seeds default role-permission assignments
    /// </summary>
    /// <returns>Success result</returns>
    [HttpPost("role-permissions")]
    [SwaggerOperation(
        Summary = "Seed default role-permission assignments",
        Description = "Seeds default permission assignments for all roles"
    )]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SeedRolePermissions(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting role-permission assignment seeding via API");
            await _permissionSeedingService.SeedDefaultRolePermissionsAsync(cancellationToken);
            _logger.LogInformation("Role-permission assignment seeding completed successfully");
            
            return Ok(new { Message = "Role-permission assignments seeded successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while seeding role-permission assignments");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { Message = "An error occurred while seeding role-permission assignments" });
        }
    }

    /// <summary>
    /// Validates permission integrity
    /// </summary>
    /// <returns>Validation result</returns>
    [HttpGet("validate")]
    [SwaggerOperation(
        Summary = "Validate permission integrity",
        Description = "Validates that all required permissions exist in the system"
    )]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ValidatePermissionIntegrity(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting permission integrity validation via API");
            var isValid = await _permissionSeedingService.ValidatePermissionIntegrityAsync(cancellationToken);
            _logger.LogInformation("Permission integrity validation completed: {IsValid}", isValid);
            
            return Ok(new { IsValid = isValid, Message = isValid ? "All required permissions exist" : "Some required permissions are missing" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while validating permission integrity");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { Message = "An error occurred while validating permission integrity" });
        }
    }

    /// <summary>
    /// Gets all default permissions that should exist in the system
    /// </summary>
    /// <returns>List of default permissions</returns>
    [HttpGet("default-permissions")]
    [SwaggerOperation(
        Summary = "Get default permissions",
        Description = "Gets all default permissions that should exist in the system"
    )]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDefaultPermissions(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving default permissions via API");
            var permissions = await _permissionSeedingService.GetDefaultPermissionsAsync(cancellationToken);
            var permissionList = permissions.Select(p => new
            {
                p.Resource,
                p.Action,
                p.Description,
                p.Category,
                Key = $"{p.Resource}.{p.Action}"
            }).ToList();
            
            _logger.LogInformation("Retrieved {Count} default permissions", permissionList.Count);
            
            return Ok(new { Permissions = permissionList, Count = permissionList.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving default permissions");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { Message = "An error occurred while retrieving default permissions" });
        }
    }

    /// <summary>
    /// Cleans up orphaned permissions that are no longer used
    /// </summary>
    /// <returns>Cleanup result</returns>
    [HttpDelete("orphaned-permissions")]
    [SwaggerOperation(
        Summary = "Clean up orphaned permissions",
        Description = "Removes permissions that are not assigned to any role or user"
    )]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CleanupOrphanedPermissions(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting orphaned permission cleanup via API");
            await _permissionSeedingService.CleanupOrphanedPermissionsAsync(cancellationToken);
            _logger.LogInformation("Orphaned permission cleanup completed successfully");
            
            return Ok(new { Message = "Orphaned permissions cleaned up successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while cleaning up orphaned permissions");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { Message = "An error occurred while cleaning up orphaned permissions" });
        }
    }

    /// <summary>
    /// Performs a complete permission system reset and re-seeding
    /// </summary>
    /// <returns>Reset result</returns>
    [HttpPost("reset")]
    [SwaggerOperation(
        Summary = "Reset and re-seed permission system",
        Description = "Performs a complete reset of the permission system and re-seeds all default data"
    )]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ResetPermissionSystem(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting complete permission system reset via API");
            
            // Seed default permissions
            await _permissionSeedingService.SeedDefaultPermissionsAsync(cancellationToken);
            
            // Seed role-permission assignments
            await _permissionSeedingService.SeedDefaultRolePermissionsAsync(cancellationToken);
            
            // Validate integrity
            var isValid = await _permissionSeedingService.ValidatePermissionIntegrityAsync(cancellationToken);
            
            _logger.LogInformation("Permission system reset completed successfully. Validation: {IsValid}", isValid);
            
            return Ok(new 
            { 
                Message = "Permission system reset completed successfully",
                ValidationPassed = isValid
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while resetting permission system");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { Message = "An error occurred while resetting the permission system" });
        }
    }
}