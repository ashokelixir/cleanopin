using CleanArchTemplate.API.Attributes;
using CleanArchTemplate.Application.Common.Models;
using CleanArchTemplate.Application.Features.Permissions.Commands.AssignPermissionToUser;
using CleanArchTemplate.Application.Features.Permissions.Commands.BulkAssignPermissions;
using CleanArchTemplate.Application.Features.Permissions.Commands.RemovePermissionFromUser;
using CleanArchTemplate.Application.Features.Permissions.Queries.GetUserPermissions;
using CleanArchTemplate.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchTemplate.API.Controllers;

/// <summary>
/// Controller for managing user-specific permission overrides
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserPermissionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserPermissionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get effective permissions for a user (role-based + user overrides)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>User's effective permissions</returns>
    [HttpGet("{userId:guid}")]
    [RequirePermission("UserPermissions.Read")]
    [ProducesResponseType(typeof(UserPermissionMatrixDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserPermissionMatrixDto>> GetUserPermissions(Guid userId)
    {
        var query = new GetUserPermissionsQuery { UserId = userId };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get only user-specific permission overrides (not role-based permissions)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>User's permission overrides</returns>
    [HttpGet("{userId:guid}/overrides")]
    [RequirePermission("UserPermissions.Read")]
    [ProducesResponseType(typeof(IEnumerable<UserPermissionOverrideDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<UserPermissionOverrideDto>>> GetUserPermissionOverrides(Guid userId)
    {
        var query = new GetUserPermissionsQuery { UserId = userId, OverridesOnly = true };
        var result = await _mediator.Send(query);
        return Ok(result.UserOverrides);
    }

    /// <summary>
    /// Assign a permission directly to a user (grant override)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="request">Permission assignment details</param>
    /// <returns>Created user permission override</returns>
    [HttpPost("{userId:guid}/permissions")]
    [RequirePermission("UserPermissions.Create")]
    [ProducesResponseType(typeof(UserPermissionOverrideDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserPermissionOverrideDto>> AssignPermissionToUser(
        Guid userId, 
        [FromBody] AssignUserPermissionRequest request)
    {
        var command = new AssignPermissionToUserCommand
        {
            UserId = userId,
            PermissionId = request.PermissionId,
            State = request.State,
            Reason = request.Reason,
            ExpiresAt = request.ExpiresAt
        };

        await _mediator.Send(command);
        
        // Get the user permissions to find the created override
        var userPermissionsQuery = new GetUserPermissionsQuery { UserId = userId };
        var userPermissions = await _mediator.Send(userPermissionsQuery);
        var createdOverride = userPermissions.UserOverrides.FirstOrDefault(o => o.Permission.Id == request.PermissionId);
        
        return CreatedAtAction(nameof(GetUserPermissions), new { userId }, createdOverride);
    }

    /// <summary>
    /// Remove a user-specific permission override
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="permissionId">Permission ID</param>
    /// <returns>No content</returns>
    [HttpDelete("{userId:guid}/permissions/{permissionId:guid}")]
    [RequirePermission("UserPermissions.Delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemovePermissionFromUser(Guid userId, Guid permissionId)
    {
        var command = new RemovePermissionFromUserCommand
        {
            UserId = userId,
            PermissionId = permissionId
        };

        await _mediator.Send(command);
        return NoContent();
    }

    /// <summary>
    /// Bulk assign permissions to a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="request">Bulk assignment request</param>
    /// <returns>Bulk assignment result</returns>
    [HttpPost("{userId:guid}/permissions/bulk")]
    [RequirePermission("UserPermissions.Create")]
    [ProducesResponseType(typeof(BulkAssignPermissionsResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BulkAssignPermissionsResult>> BulkAssignPermissionsToUser(
        Guid userId, 
        [FromBody] BulkAssignUserPermissionsRequest request)
    {
        var command = new BulkAssignPermissionsCommand
        {
            UserId = userId,
            Permissions = request.PermissionIds.Select(id => new PermissionAssignmentDto
            {
                PermissionId = id,
                State = request.State,
                ExpiresAt = request.ExpiresAt
            }),
            Reason = request.Reason
        };

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Bulk remove user permission overrides
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="request">Bulk removal request</param>
    /// <returns>Bulk removal result</returns>
    [HttpDelete("{userId:guid}/permissions/bulk")]
    [RequirePermission("UserPermissions.Delete")]
    [ProducesResponseType(typeof(BulkAssignPermissionsResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BulkAssignPermissionsResult>> BulkRemovePermissionsFromUser(
        Guid userId, 
        [FromBody] BulkRemoveUserPermissionsRequest request)
    {
        // For removal, we'll use multiple individual remove commands
        // since the bulk command is designed for assignments
        var results = new List<string>();
        var successCount = 0;
        var failCount = 0;

        foreach (var permissionId in request.PermissionIds)
        {
            try
            {
                var removeCommand = new RemovePermissionFromUserCommand
                {
                    UserId = userId,
                    PermissionId = permissionId
                };
                await _mediator.Send(removeCommand);
                successCount++;
            }
            catch (Exception ex)
            {
                failCount++;
                results.Add($"Failed to remove permission {permissionId}: {ex.Message}");
            }
        }

        var result = new BulkAssignPermissionsResult
        {
            SuccessfulAssignments = successCount,
            FailedAssignments = failCount,
            Errors = results
        };

        return Ok(result);
    }

    /// <summary>
    /// Update a user permission override
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="permissionId">Permission ID</param>
    /// <param name="request">Update request</param>
    /// <returns>Updated user permission override</returns>
    [HttpPut("{userId:guid}/permissions/{permissionId:guid}")]
    [RequirePermission("UserPermissions.Update")]
    [ProducesResponseType(typeof(UserPermissionOverrideDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserPermissionOverrideDto>> UpdateUserPermission(
        Guid userId, 
        Guid permissionId, 
        [FromBody] UpdateUserPermissionRequest request)
    {
        // First remove the existing override
        var removeCommand = new RemovePermissionFromUserCommand
        {
            UserId = userId,
            PermissionId = permissionId
        };

        await _mediator.Send(removeCommand);

        // Then create the new override
        var assignCommand = new AssignPermissionToUserCommand
        {
            UserId = userId,
            PermissionId = permissionId,
            State = request.State,
            Reason = request.Reason,
            ExpiresAt = request.ExpiresAt
        };

        await _mediator.Send(assignCommand);
        
        // Get the updated user permissions
        var userPermissionsQuery = new GetUserPermissionsQuery { UserId = userId };
        var userPermissions = await _mediator.Send(userPermissionsQuery);
        var updatedOverride = userPermissions.UserOverrides.FirstOrDefault(o => o.Permission.Id == permissionId);
        
        return Ok(updatedOverride);
    }

    /// <summary>
    /// Check if a user has a specific permission
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="resource">Resource name</param>
    /// <param name="action">Action name</param>
    /// <returns>Permission check result</returns>
    [HttpGet("{userId:guid}/check/{resource}/{action}")]
    [RequirePermission("UserPermissions.Read")]
    [ProducesResponseType(typeof(PermissionCheckResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PermissionCheckResult>> CheckUserPermission(
        Guid userId, 
        string resource, 
        string action)
    {
        var query = new GetUserPermissionsQuery { UserId = userId };
        var userPermissions = await _mediator.Send(query);

        var permissionName = $"{resource}.{action}";
        var hasPermission = userPermissions.EffectivePermissions.Any(p => p.Name == permissionName);

        var result = new PermissionCheckResult
        {
            UserId = userId,
            Resource = resource,
            Action = action,
            PermissionName = permissionName,
            HasPermission = hasPermission,
            Source = DeterminePermissionSource(userPermissions, permissionName)
        };

        return Ok(result);
    }

    private static string DeterminePermissionSource(UserPermissionMatrixDto userPermissions, string permissionName)
    {
        var userOverride = userPermissions.UserOverrides.FirstOrDefault(o => o.Permission.Name == permissionName);
        if (userOverride != null)
        {
            return userOverride.State == PermissionState.Grant ? "User Override (Grant)" : "User Override (Deny)";
        }

        var rolePermission = userPermissions.RolePermissions.FirstOrDefault(p => p.Name == permissionName);
        if (rolePermission != null)
        {
            return "Role-based";
        }

        return "None";
    }
}

/// <summary>
/// Request model for assigning permission to user
/// </summary>
public class AssignUserPermissionRequest
{
    public Guid PermissionId { get; set; }
    public PermissionState State { get; set; } = PermissionState.Grant;
    public string? Reason { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// Request model for updating user permission
/// </summary>
public class UpdateUserPermissionRequest
{
    public PermissionState State { get; set; } = PermissionState.Grant;
    public string? Reason { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// Request model for bulk user permission assignment
/// </summary>
public class BulkAssignUserPermissionsRequest
{
    public IEnumerable<Guid> PermissionIds { get; set; } = new List<Guid>();
    public PermissionState State { get; set; } = PermissionState.Grant;
    public string? Reason { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// Request model for bulk user permission removal
/// </summary>
public class BulkRemoveUserPermissionsRequest
{
    public IEnumerable<Guid> PermissionIds { get; set; } = new List<Guid>();
}

/// <summary>
/// Result model for permission checks
/// </summary>
public class PermissionCheckResult
{
    public Guid UserId { get; set; }
    public string Resource { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string PermissionName { get; set; } = string.Empty;
    public bool HasPermission { get; set; }
    public string Source { get; set; } = string.Empty;
}