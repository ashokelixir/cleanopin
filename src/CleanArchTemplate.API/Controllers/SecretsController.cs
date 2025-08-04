using CleanArchTemplate.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchTemplate.API.Controllers;

/// <summary>
/// Controller for demonstrating AWS Secrets Manager integration
/// This controller should be removed in production or secured appropriately
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "Administrator")]
public class SecretsController : ControllerBase
{
    private readonly ISecretsManagerService _secretsService;
    private readonly ILogger<SecretsController> _logger;

    public SecretsController(
        ISecretsManagerService secretsService,
        ILogger<SecretsController> logger)
    {
        _secretsService = secretsService;
        _logger = logger;
    }

    /// <summary>
    /// Gets a list of available secret operations (for demonstration)
    /// </summary>
    /// <returns>List of available operations</returns>
    [HttpGet("operations")]
    public IActionResult GetAvailableOperations()
    {
        var operations = new[]
        {
            new { Operation = "GET /api/v1/secrets/test/{secretName}", Description = "Test secret retrieval" },
            new { Operation = "POST /api/v1/secrets/cache/invalidate", Description = "Invalidate secret cache" },
            new { Operation = "POST /api/v1/secrets/cache/clear", Description = "Clear all cached secrets" },
            new { Operation = "GET /api/v1/secrets/health", Description = "Check Secrets Manager health" }
        };

        return Ok(operations);
    }

    /// <summary>
    /// Tests secret retrieval (for demonstration purposes)
    /// WARNING: This endpoint should be removed in production
    /// </summary>
    /// <param name="secretName">The name of the secret to test</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status without exposing the actual secret</returns>
    [HttpGet("test/{secretName}")]
    public async Task<IActionResult> TestSecretRetrieval(
        string secretName, 
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Testing secret retrieval for {SecretName}", secretName);

            var secret = await _secretsService.GetSecretAsync(secretName, cancellationToken);
            
            // Never return the actual secret value in the response
            return Ok(new
            {
                SecretName = secretName,
                Retrieved = true,
                Length = secret.Length,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve secret {SecretName}", secretName);
            
            return BadRequest(new
            {
                SecretName = secretName,
                Retrieved = false,
                Error = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Invalidates cache for a specific secret
    /// </summary>
    /// <param name="request">The cache invalidation request</param>
    /// <returns>Success status</returns>
    [HttpPost("cache/invalidate")]
    public IActionResult InvalidateSecretCache([FromBody] InvalidateCacheRequest request)
    {
        try
        {
            _secretsService.InvalidateCache(request.SecretName);
            
            _logger.LogInformation("Invalidated cache for secret {SecretName}", request.SecretName);
            
            return Ok(new
            {
                SecretName = request.SecretName,
                Action = "Cache Invalidated",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to invalidate cache for secret {SecretName}", request.SecretName);
            return BadRequest(new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Clears all cached secrets
    /// </summary>
    /// <returns>Success status</returns>
    [HttpPost("cache/clear")]
    public IActionResult ClearSecretCache()
    {
        try
        {
            _secretsService.ClearCache();
            
            _logger.LogInformation("Cleared all secrets from cache");
            
            return Ok(new
            {
                Action = "All Cache Cleared",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear secret cache");
            return BadRequest(new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Checks the health of the Secrets Manager service
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health status</returns>
    [HttpGet("health")]
    public async Task<IActionResult> CheckSecretsManagerHealth(CancellationToken cancellationToken)
    {
        try
        {
            // Try to retrieve a test secret or use a known secret
            // In a real implementation, you might have a dedicated health check secret
            var testSecrets = new[] { "database-credentials", "jwt-settings" };
            var healthResults = new List<object>();

            foreach (var secretName in testSecrets)
            {
                try
                {
                    await _secretsService.GetSecretAsync(secretName, cancellationToken);
                    healthResults.Add(new
                    {
                        SecretName = secretName,
                        Status = "Healthy",
                        Timestamp = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    healthResults.Add(new
                    {
                        SecretName = secretName,
                        Status = "Unhealthy",
                        Error = ex.Message,
                        Timestamp = DateTime.UtcNow
                    });
                }
            }

            var overallHealth = healthResults.All(r => r.GetType().GetProperty("Status")?.GetValue(r)?.ToString() == "Healthy");

            return Ok(new
            {
                OverallStatus = overallHealth ? "Healthy" : "Unhealthy",
                SecretChecks = healthResults,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check Secrets Manager health");
            
            return StatusCode(503, new
            {
                OverallStatus = "Unhealthy",
                Error = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}

/// <summary>
/// Request model for cache invalidation
/// </summary>
public class InvalidateCacheRequest
{
    /// <summary>
    /// The name of the secret to invalidate from cache
    /// </summary>
    public string SecretName { get; set; } = string.Empty;
}