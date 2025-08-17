using CleanArchTemplate.Application.Features.Roles.Commands.CreateRole;
using CleanArchTemplate.Application.Features.Roles.Commands.UpdateRole;
using CleanArchTemplate.Application.Features.Roles.Commands.DeleteRole;
using CleanArchTemplate.Application.Features.Roles.Queries.GetAllRoles;
using CleanArchTemplate.Application.Features.Roles.Queries.GetRoleById;
using CleanArchTemplate.Application.Common.Models;
using CleanArchTemplate.Shared.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CleanArchTemplate.API.Controllers;

/// <summary>
/// Roles controller for role management operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
[SwaggerTag("Role management operations for creating, updating, and managing roles and permissions")]
public class RolesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<RolesController> _logger;

    public RolesController(IMediator mediator, ILogger<RolesController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new role
    /// </summary>
    /// <param name="command">Role creation details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created role details</returns>
    [HttpPost]
    [SwaggerOperation(
        Summary = "Create a new role",
        Description = "Creates a new role with the specified name and description. Role names must be unique.",
        OperationId = "CreateRole",
        Tags = new[] { "Roles" }
    )]
    [SwaggerResponse(201, "Role created successfully", typeof(RoleDto))]
    [SwaggerResponse(400, "Invalid request data", typeof(object))]
    [SwaggerResponse(409, "Role with name already exists", typeof(object))]
    [SwaggerResponse(422, "Validation errors", typeof(object))]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating role: {RoleName}", command.Name);

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Role created successfully with ID: {RoleId}", result.Data!.Id);
            return CreatedAtAction(nameof(GetRoleById), new { id = result.Data.Id }, result.Data);
        }

        _logger.LogWarning("Role creation failed for name: {RoleName}. Error: {Error}", command.Name, result.Message);
        return result.StatusCode switch
        {
            400 => BadRequest(new { error = result.Message, details = result.Errors }),
            409 => Conflict(new { error = result.Message }),
            422 => UnprocessableEntity(new { error = result.Message, details = result.Errors }),
            _ => BadRequest(new { error = result.Message })
        };
    }

    /// <summary>
    /// Gets all roles with pagination
    /// </summary>
    /// <param name="request">Pagination parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of roles</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<RoleSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllRoles([FromQuery] PaginationRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving roles with pagination: Page {PageNumber}, Size {PageSize}", request.PageNumber, request.PageSize);

        var query = new GetAllRolesQuery(request);
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Gets a role by ID
    /// </summary>
    /// <param name="id">Role ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Role details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoleById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving role by ID: {RoleId}", id);

        var query = new GetRoleByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Data);
        }

        _logger.LogWarning("Role not found with ID: {RoleId}", id);
        return result.StatusCode switch
        {
            404 => NotFound(new { error = result.Message }),
            _ => BadRequest(new { error = result.Message })
        };
    }

    /// <summary>
    /// Updates an existing role
    /// </summary>
    /// <param name="id">Role ID</param>
    /// <param name="command">Role update details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated role details</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateRoleCommand command, CancellationToken cancellationToken)
    {
        if (id != command.RoleId)
        {
            return BadRequest(new { error = "ID in URL does not match ID in request body" });
        }

        _logger.LogInformation("Updating role: {RoleId}", id);

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Role updated successfully: {RoleId}", id);
            return Ok(result.Data);
        }

        _logger.LogWarning("Role update failed for ID: {RoleId}. Error: {Error}", id, result.Message);
        return result.StatusCode switch
        {
            404 => NotFound(new { error = result.Message }),
            409 => Conflict(new { error = result.Message }),
            422 => UnprocessableEntity(new { error = result.Message, details = result.Errors }),
            _ => BadRequest(new { error = result.Message })
        };
    }

    /// <summary>
    /// Deletes a role (deactivates)
    /// </summary>
    /// <param name="id">Role ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRole(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting role: {RoleId}", id);

        var command = new DeleteRoleCommand(id);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Role deleted successfully: {RoleId}", id);
            return NoContent();
        }

        _logger.LogWarning("Role deletion failed for ID: {RoleId}. Error: {Error}", id, result.Message);
        return result.StatusCode switch
        {
            404 => NotFound(new { error = result.Message }),
            _ => BadRequest(new { error = result.Message })
        };
    }
}