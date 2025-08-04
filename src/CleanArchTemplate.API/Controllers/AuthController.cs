using CleanArchTemplate.Application.Features.Authentication.Commands.Login;
using CleanArchTemplate.Application.Features.Authentication.Commands.Logout;
using CleanArchTemplate.Application.Features.Authentication.Commands.RefreshToken;
using CleanArchTemplate.Application.Features.Authentication.Commands.Register;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CleanArchTemplate.API.Controllers;

/// <summary>
/// Authentication controller for user login, registration, and token management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[SwaggerTag("Authentication and authorization endpoints including login, registration, and token management")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IMediator mediator, ILogger<AuthController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registers a new user account
    /// </summary>
    /// <param name="command">Registration details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Registration response with user details</returns>
    [HttpPost("register")]
    [SwaggerOperation(
        Summary = "Register a new user account",
        Description = "Creates a new user account with email verification. Requires valid email, password, and personal information.",
        OperationId = "RegisterUser",
        Tags = new[] { "Authentication" }
    )]
    [SwaggerResponse(201, "User registered successfully", typeof(RegisterResponse))]
    [SwaggerResponse(400, "Invalid request data", typeof(object))]
    [SwaggerResponse(422, "Validation errors", typeof(object))]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("=== REGISTER ENDPOINT HIT === User registration attempt for email: {Email}", command?.Email ?? "NULL");
        
        if (command == null)
        {
            _logger.LogError("RegisterCommand is null");
            return BadRequest(new { error = "Invalid request data" });
        }

        try
        {
            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("User registered successfully with ID: {UserId}", result.Data!.UserId);
                return CreatedAtAction(nameof(Register), new { id = result.Data.UserId }, result.Data);
            }

            _logger.LogWarning("User registration failed for email: {Email}. Error: {Error}", command.Email, result.Message);
            return result.StatusCode switch
            {
                400 => BadRequest(new { error = result.Message }),
                422 => UnprocessableEntity(new { error = result.Message }),
                _ => BadRequest(new { error = result.Message })
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred during user registration for email: {Email}", command.Email);
            return StatusCode(500, new { error = "An internal server error occurred" });
        }
    }

    /// <summary>
    /// Authenticates a user and returns access and refresh tokens
    /// </summary>
    /// <param name="command">Login credentials</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication tokens and user information</returns>
    [HttpPost("login")]
    [SwaggerOperation(
        Summary = "Authenticate user and get tokens",
        Description = "Validates user credentials and returns JWT access token and refresh token for API access.",
        OperationId = "LoginUser",
        Tags = new[] { "Authentication" }
    )]
    [SwaggerResponse(200, "Login successful", typeof(LoginResponse))]
    [SwaggerResponse(401, "Invalid credentials", typeof(object))]
    [SwaggerResponse(403, "Account locked or disabled", typeof(object))]
    [SwaggerResponse(422, "Validation errors", typeof(object))]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Login attempt for email: {Email}", command.Email);

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation("User logged in successfully: {Email}", command.Email);
            return Ok(result.Data);
        }

        _logger.LogWarning("Login failed for email: {Email}. Error: {Error}", command.Email, result.Message);
        return result.StatusCode switch
        {
            401 => Unauthorized(new { error = result.Message }),
            403 => Forbid(),
            422 => UnprocessableEntity(new { error = result.Message }),
            _ => Unauthorized(new { error = result.Message })
        };
    }

    /// <summary>
    /// Refreshes an access token using a valid refresh token
    /// </summary>
    /// <param name="command">Refresh token details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New access and refresh tokens</returns>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Token refresh attempt");

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Token refreshed successfully");
            return Ok(result.Data);
        }

        _logger.LogWarning("Token refresh failed. Error: {Error}", result.Message);
        return result.StatusCode switch
        {
            401 => Unauthorized(new { error = result.Message }),
            422 => UnprocessableEntity(new { error = result.Message }),
            _ => Unauthorized(new { error = result.Message })
        };
    }

    /// <summary>
    /// Logs out a user by revoking their refresh token
    /// </summary>
    /// <param name="command">Logout details with refresh token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response</returns>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Logout([FromBody] LogoutCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Logout attempt for user: {UserId}", User.Identity?.Name);

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation("User logged out successfully: {UserId}", User.Identity?.Name);
            return Ok(new { message = "Logged out successfully" });
        }

        _logger.LogWarning("Logout failed for user: {UserId}. Error: {Error}", User.Identity?.Name, result.Message);
        return result.StatusCode switch
        {
            422 => UnprocessableEntity(new { error = result.Message }),
            _ => BadRequest(new { error = result.Message })
        };
    }
}
