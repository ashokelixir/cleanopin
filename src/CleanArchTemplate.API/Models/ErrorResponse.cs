using System.Text.Json.Serialization;

namespace CleanArchTemplate.API.Models;

/// <summary>
/// Standardized error response model following RFC 7807 Problem Details for HTTP APIs
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// The HTTP status code
    /// </summary>
    [JsonPropertyName("status")]
    public int StatusCode { get; set; }

    /// <summary>
    /// A short, human-readable summary of the problem type
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// A human-readable explanation specific to this occurrence of the problem
    /// </summary>
    [JsonPropertyName("detail")]
    public string Detail { get; set; } = string.Empty;

    /// <summary>
    /// A URI reference that identifies the specific occurrence of the problem
    /// </summary>
    [JsonPropertyName("instance")]
    public string Instance { get; set; } = string.Empty;

    /// <summary>
    /// A URI reference that identifies the problem type
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Validation errors (if applicable)
    /// </summary>
    [JsonPropertyName("errors")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Errors { get; set; }

    /// <summary>
    /// Additional properties for extending the error response
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object>? Extensions { get; set; }

    /// <summary>
    /// Timestamp when the error occurred
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}