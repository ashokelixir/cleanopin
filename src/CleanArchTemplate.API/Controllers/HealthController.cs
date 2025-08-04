using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CleanArchTemplate.Infrastructure.Data.Contexts;
using CleanArchTemplate.Infrastructure.Services;
using CleanArchTemplate.API.Models;
using System.Diagnostics;
using Asp.Versioning;

namespace CleanArchTemplate.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[ApiVersion("1.0")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<HealthController> _logger;
    private readonly ResilientHealthCheckService _resilientHealthCheckService;

    public HealthController(
        ApplicationDbContext dbContext, 
        ILogger<HealthController> logger,
        ResilientHealthCheckService resilientHealthCheckService)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _resilientHealthCheckService = resilientHealthCheckService ?? throw new ArgumentNullException(nameof(resilientHealthCheckService));
    }

    [HttpGet]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetHealth()
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var healthResponse = new HealthResponse
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Version = GetType().Assembly.GetName().Version?.ToString() ?? "Unknown",
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
            };

            var checks = new List<HealthCheck>();
            var dbCheck = await CheckDatabaseHealthAsync();
            checks.Add(dbCheck);

            healthResponse.Checks = checks;
            healthResponse.Duration = stopwatch.ElapsedMilliseconds;

            var isHealthy = checks.All(c => c.Status == "Healthy");
            healthResponse.Status = isHealthy ? "Healthy" : "Unhealthy";

            return isHealthy ? Ok(healthResponse) : StatusCode(503, healthResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed with exception");
            return StatusCode(500, new { Status = "Error", Message = ex.Message, Timestamp = DateTime.UtcNow });
        }
    }

    [HttpGet("database")]
    [ProducesResponseType(typeof(HealthCheck), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HealthCheck), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetDatabaseHealth()
    {
        var dbCheck = await CheckDatabaseHealthAsync();
        return dbCheck.Status == "Healthy" ? Ok(dbCheck) : StatusCode(503, dbCheck);
    }

    private async Task<HealthCheck> CheckDatabaseHealthAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var check = new HealthCheck
        {
            Name = "Database",
            Status = "Healthy"
        };

        try
        {
            await _dbContext.Database.OpenConnectionAsync();
            await _dbContext.Database.CloseConnectionAsync();
            var userCount = await _dbContext.Users.CountAsync();
            
            check.Data = new Dictionary<string, object>
            {
                ["UserCount"] = userCount
            };
        }
        catch (Exception ex)
        {
            check.Status = "Unhealthy";
            check.Error = ex.Message;
        }
        finally
        {
            check.Duration = stopwatch.ElapsedMilliseconds;
        }

        return check;
    }

    [HttpGet("resilient")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetResilientHealth()
    {
        try
        {
            var systemHealth = await _resilientHealthCheckService.CheckSystemHealthAsync();
            
            var response = new
            {
                Status = systemHealth.IsHealthy ? "Healthy" : "Unhealthy",
                Timestamp = systemHealth.Timestamp,
                Checks = systemHealth.Checks.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new
                    {
                        Status = kvp.Value.IsHealthy ? "Healthy" : "Unhealthy",
                        Message = kvp.Value.Message,
                        ResponseTimeMs = kvp.Value.ResponseTime.TotalMilliseconds,
                        Details = kvp.Value.Details
                    })
            };

            return systemHealth.IsHealthy ? Ok(response) : StatusCode(503, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Resilient health check failed");
            return StatusCode(503, new { Status = "Unhealthy", Error = ex.Message });
        }
    }

    [HttpGet("critical")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetCriticalOperationsHealth()
    {
        try
        {
            var criticalHealth = await _resilientHealthCheckService.CheckCriticalOperationsAsync();
            
            var response = new
            {
                Status = criticalHealth.IsHealthy ? "Healthy" : "Unhealthy",
                Message = criticalHealth.Message,
                ResponseTimeMs = criticalHealth.ResponseTime.TotalMilliseconds,
                Details = criticalHealth.Details
            };

            return criticalHealth.IsHealthy ? Ok(response) : StatusCode(503, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical operations health check failed");
            return StatusCode(503, new { Status = "Unhealthy", Error = ex.Message });
        }
    }

    [HttpGet("ping")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult Ping()
    {
        return Ok(new 
        { 
            Status = "OK", 
            Message = "Health controller is working", 
            Timestamp = DateTime.UtcNow,
            Version = GetType().Assembly.GetName().Version?.ToString() ?? "Unknown"
        });
    }
}