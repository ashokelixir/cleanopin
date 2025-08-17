using Microsoft.AspNetCore.Mvc;
using CleanArchTemplate.API.Attributes;

namespace CleanArchTemplate.IntegrationTests.API.Authorization;

/// <summary>
/// Test controller for authorization attribute testing
/// </summary>
[ApiController]
[Route("api/test/authorization")]
public class TestAuthorizationController : ControllerBase
{
    /// <summary>
    /// Test endpoint requiring a specific permission
    /// </summary>
    [HttpGet("require-permission")]
    [RequirePermission("Users.Read")]
    public IActionResult TestRequirePermission()
    {
        return Ok(new { message = "Access granted with RequirePermission attribute" });
    }

    /// <summary>
    /// Test endpoint requiring a resource-action permission
    /// </summary>
    [HttpGet("require-resource-action")]
    [RequireResourceAction("Users", "Create")]
    public IActionResult TestRequireResourceAction()
    {
        return Ok(new { message = "Access granted with RequireResourceAction attribute" });
    }

    /// <summary>
    /// Test endpoint requiring any of multiple permissions
    /// </summary>
    [HttpGet("require-any-permission")]
    [RequireAnyPermission("Users.Create", "Users.Update", "Users.Delete")]
    public IActionResult TestRequireAnyPermission()
    {
        return Ok(new { message = "Access granted with RequireAnyPermission attribute" });
    }

    /// <summary>
    /// Test endpoint with multiple authorization attributes
    /// </summary>
    [HttpGet("multiple-attributes")]
    [RequirePermission("Users.Read")]
    [RequireResourceAction("Roles", "Read")]
    public IActionResult TestMultipleAttributes()
    {
        return Ok(new { message = "Access granted with multiple authorization attributes" });
    }

    /// <summary>
    /// Test endpoint without any authorization
    /// </summary>
    [HttpGet("no-authorization")]
    public IActionResult TestNoAuthorization()
    {
        return Ok(new { message = "No authorization required" });
    }

    /// <summary>
    /// Test endpoint with invalid permission
    /// </summary>
    [HttpGet("invalid-permission")]
    [RequirePermission("NonExistent.Permission")]
    public IActionResult TestInvalidPermission()
    {
        return Ok(new { message = "This should not be accessible" });
    }
}