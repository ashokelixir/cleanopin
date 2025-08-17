namespace CleanArchTemplate.Infrastructure.Models;

/// <summary>
/// Health check result model
/// </summary>
public class HealthCheckResult
{
    public bool IsHealthy { get; set; }
    public string Message { get; set; } = string.Empty;
    public TimeSpan ResponseTime { get; set; }
    public Dictionary<string, object>? Details { get; set; }
}

/// <summary>
/// System health result model
/// </summary>
public class SystemHealthResult
{
    public bool IsHealthy { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, HealthCheckResult> Checks { get; set; } = new();
}