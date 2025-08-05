using Microsoft.AspNetCore.Mvc;

namespace CleanArchTemplate.API.Controllers;

[ApiController]
[Route("")]
public class RootHealthController : ControllerBase
{
    private readonly ILogger<RootHealthController> _logger;

    public RootHealthController(ILogger<RootHealthController> logger)
    {
        _logger = logger;
    }

    [HttpGet("health")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult GetSimpleHealth()
    {
        try
        {
            _logger.LogDebug("Health check endpoint called");
            
            return Ok(new 
            { 
                Status = "OK", 
                Timestamp = DateTime.UtcNow,
                Service = "CleanArchTemplate API",
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return StatusCode(500, new 
            { 
                Status = "Error", 
                Timestamp = DateTime.UtcNow,
                Service = "CleanArchTemplate API",
                Error = ex.Message
            });
        }
    }
}