using CleanArchTemplate.API.Attributes;
using CleanArchTemplate.Application.Common.Models;
using CleanArchTemplate.Application.Features.Permissions.Commands.CreatePermission;
using CleanArchTemplate.Application.Features.Permissions.Commands.UpdatePermission;
using CleanArchTemplate.Application.Features.Permissions.Queries.GetPermissions;
using CleanArchTemplate.Shared.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchTemplate.API.Controllers;

/// <summary>
/// Controller for managing permissions with CRUD operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PermissionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all permissions with optional filtering
    /// </summary>
    /// <param name="query">Query parameters for filtering and pagination</param>
    /// <returns>Paginated list of permissions</returns>
    [HttpGet]
    [RequirePermission("Permissions.Read")]
    [ProducesResponseType(typeof(PaginatedResult<PermissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedResult<PermissionDto>>> GetPermissions([FromQuery] GetPermissionsQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get a specific permission by ID
    /// </summary>
    /// <param name="id">Permission ID</param>
    /// <returns>Permission details</returns>
    [HttpGet("{id:guid}")]
    [RequirePermission("Permissions.Read")]
    [ProducesResponseType(typeof(PermissionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PermissionDto>> GetPermission(Guid id)
    {
        var query = new GetPermissionsQuery { Id = id };
        var result = await _mediator.Send(query);
        
        var permission = result.Items.FirstOrDefault();
        if (permission == null)
        {
            return NotFound($"Permission with ID {id} not found.");
        }

        return Ok(permission);
    }

    /// <summary>
    /// Create a new permission
    /// </summary>
    /// <param name="command">Permission creation details</param>
    /// <returns>Created permission</returns>
    [HttpPost]
    [RequirePermission("Permissions.Create")]
    [ProducesResponseType(typeof(PermissionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PermissionDto>> CreatePermission([FromBody] CreatePermissionCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetPermission), new { id = result }, result);
    }

    /// <summary>
    /// Update an existing permission
    /// </summary>
    /// <param name="id">Permission ID</param>
    /// <param name="command">Permission update details</param>
    /// <returns>Updated permission</returns>
    [HttpPut("{id:guid}")]
    [RequirePermission("Permissions.Update")]
    [ProducesResponseType(typeof(PermissionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PermissionDto>> UpdatePermission(Guid id, [FromBody] UpdatePermissionCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest("Permission ID in URL does not match the ID in the request body.");
        }

        await _mediator.Send(command);
        
        // Get the updated permission to return
        var updatedQuery = new GetPermissionsQuery { Id = id };
        var updatedResult = await _mediator.Send(updatedQuery);
        var updatedPermission = updatedResult.Items.FirstOrDefault();
        
        if (updatedPermission == null)
        {
            return NotFound($"Permission with ID {id} not found after update.");
        }
        
        return Ok(updatedPermission);
    }

    /// <summary>
    /// Delete a permission (soft delete)
    /// </summary>
    /// <param name="id">Permission ID</param>
    /// <returns>No content</returns>
    [HttpDelete("{id:guid}")]
    [RequirePermission("Permissions.Delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeletePermission(Guid id)
    {
        // For now, we'll implement this as deactivating the permission
        // since the domain model supports IsActive flag
        var command = new UpdatePermissionCommand
        {
            Id = id,
            IsActive = false
        };

        try
        {
            await _mediator.Send(command);
            return NoContent();
        }
        catch (ArgumentException)
        {
            return NotFound($"Permission with ID {id} not found.");
        }
    }

    /// <summary>
    /// Get permissions by resource
    /// </summary>
    /// <param name="resource">Resource name</param>
    /// <returns>List of permissions for the resource</returns>
    [HttpGet("by-resource/{resource}")]
    [RequirePermission("Permissions.Read")]
    [ProducesResponseType(typeof(IEnumerable<PermissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<PermissionDto>>> GetPermissionsByResource(string resource)
    {
        var query = new GetPermissionsQuery { Resource = resource };
        var result = await _mediator.Send(query);
        return Ok(result.Items);
    }

    /// <summary>
    /// Get permissions by category
    /// </summary>
    /// <param name="category">Category name</param>
    /// <returns>List of permissions in the category</returns>
    [HttpGet("by-category/{category}")]
    [RequirePermission("Permissions.Read")]
    [ProducesResponseType(typeof(IEnumerable<PermissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<PermissionDto>>> GetPermissionsByCategory(string category)
    {
        var query = new GetPermissionsQuery { Category = category };
        var result = await _mediator.Send(query);
        return Ok(result.Items);
    }
}