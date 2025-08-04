namespace CleanArchTemplate.API.Models;

/// <summary>
/// Health response model
/// </summary>
public class HealthResponse
{
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public long Duration { get; set; }
    public IEnumerable<HealthCheck> Checks { get; set; } = new List<HealthCheck>();
}

/// <summary>
/// Detailed health response model
/// </summary>
public class DetailedHealthResponse : HealthResponse
{
    public string MachineName { get; set; } = string.Empty;
    public int ProcessId { get; set; }
    public long WorkingSet { get; set; }
    public int ProcessorCount { get; set; }
}

/// <summary>
/// Individual health check model
/// </summary>
public class HealthCheck
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public long Duration { get; set; }
    public string? Error { get; set; }
    public Dictionary<string, object>? Data { get; set; }
}