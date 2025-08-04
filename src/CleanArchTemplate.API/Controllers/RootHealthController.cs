using Microsoft.AspNetCore.Mvc;

namespace CleanArchTemplate.API.Controllers;

[ApiController]
[Route("")]
public class RootHealthController : ControllerBase
{
    [HttpGet("health")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult GetSimpleHealth()
    {
        return Ok(new 
        { 
            Status = "OK", 
            Timestamp = DateTime.UtcNow,
            Service = "CleanArchTemplate API"
        });
    }
}