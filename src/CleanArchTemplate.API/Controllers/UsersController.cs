using CleanArchTemplate.Application.Features.Users.Commands.CreateUser;
using CleanArchTemplate.Application.Features.Users.Commands.UpdateUser;
using CleanArchTemplate.Application.Features.Users.Queries.GetAllUsers;
using CleanArchTemplate.Application.Features.Users.Queries.GetUserById;
using CleanArchTemplate.Application.Common.Models;
using CleanArchTemplate.Shared.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CleanArchTemplate.API.Controllers;

/// <summary>
/// Users controller for user management operations with resilience patterns
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
[SwaggerTag("User management operations with resilience patterns for creating, updating, and retrieving user information")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IMediator mediator, ILogger<UsersController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new user with resilience patterns
    /// </summary>
    /// <param name="command">User creation details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created user details</returns>
    [HttpPost]
    [SwaggerOperation(
        Summary = "Create a new user with resilience patterns",
        Description = "Creates a new user using resilience patterns including retry logic, circuit breaker, and timeout protection. Demonstrates enterprise-grade fault tolerance.",
        OperationId = "CreateUser",
        Tags = new[] { "Users" }
    )]
    [SwaggerResponse(201, "User created successfully", typeof(UserDto))]
    [SwaggerResponse(400, "Invalid request data", typeof(object))]
    [SwaggerResponse(409, "User with email already exists", typeof(object))]
    [SwaggerResponse(422, "Validation errors", typeof(object))]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating user with resilience patterns for email: {Email}", command.Email);

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation("User created successfully with ID: {UserId}", result.Data!.Id);
            return CreatedAtAction(nameof(GetUserById), new { id = result.Data.Id }, result.Data);
        }

        _logger.LogWarning("User creation failed for email: {Email}. Error: {Error}", command.Email, result.Message);
        return result.StatusCode switch
        {
            400 => BadRequest(new { error = result.Message, details = result.Errors }),
            409 => Conflict(new { error = result.Message }),
            422 => UnprocessableEntity(new { error = result.Message, details = result.Errors }),
            _ => BadRequest(new { error = result.Message })
        };
    }

    /// <summary>
    /// Gets all users with pagination
    /// </summary>
    /// <param name="request">Pagination parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of users</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<UserSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllUsers([FromQuery] PaginationRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving users with pagination: Page {PageNumber}, Size {PageSize}", request.PageNumber, request.PageSize);

        var query = new GetAllUsersQuery(request);
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Gets a user by ID
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving user by ID: {UserId}", id);

        var query = new GetUserByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Data);
        }

        _logger.LogWarning("User not found with ID: {UserId}", id);
        return result.StatusCode switch
        {
            404 => NotFound(new { error = result.Message }),
            _ => BadRequest(new { error = result.Message })
        };
    }

    /// <summary>
    /// Updates an existing user
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="command">User update details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated user details</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserCommand command, CancellationToken cancellationToken)
    {
        if (id != command.UserId)
        {
            return BadRequest(new { error = "ID in URL does not match ID in request body" });
        }

        _logger.LogInformation("Updating user: {UserId}", id);

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation("User updated successfully: {UserId}", id);
            return Ok(result.Data);
        }

        _logger.LogWarning("User update failed for ID: {UserId}. Error: {Error}", id, result.Message);
        return result.StatusCode switch
        {
            404 => NotFound(new { error = result.Message }),
            422 => UnprocessableEntity(new { error = result.Message, details = result.Errors }),
            _ => BadRequest(new { error = result.Message })
        };
    }
}