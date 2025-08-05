namespace CleanArchTemplate.Shared.Models;

/// <summary>
/// Configuration settings for AWS Secrets Manager
/// </summary>
public class SecretsManagerSettings
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "SecretsManager";

    /// <summary>
    /// AWS region for Secrets Manager
    /// </summary>
    public string Region { get; set; } = "ap-south-1";

    /// <summary>
    /// Environment-specific secret name prefix
    /// </summary>
    public string Environment { get; set; } = "development";

    /// <summary>
    /// Project name for secret naming convention
    /// </summary>
    public string ProjectName { get; set; } = "cleanarch-template";

    /// <summary>
    /// Cache duration for secrets in minutes
    /// </summary>
    public int CacheDurationMinutes { get; set; } = 15;

    /// <summary>
    /// Maximum number of retry attempts for failed requests
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Base delay for exponential backoff in milliseconds
    /// </summary>
    public int BaseDelayMs { get; set; } = 1000;

    /// <summary>
    /// Whether to enable automatic secret rotation detection
    /// </summary>
    public bool EnableRotationDetection { get; set; } = true;

    /// <summary>
    /// List of secret names to preload on startup
    /// </summary>
    public List<string> PreloadSecrets { get; set; } = new();

    /// <summary>
    /// Whether to use local development mode (skip AWS calls)
    /// </summary>
    public bool UseLocalDevelopment { get; set; } = false;
}

/// <summary>
/// Model for secret metadata
/// </summary>
public class SecretMetadata
{
    /// <summary>
    /// Secret name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Secret ARN
    /// </summary>
    public string Arn { get; set; } = string.Empty;

    /// <summary>
    /// Version ID
    /// </summary>
    public string VersionId { get; set; } = string.Empty;

    /// <summary>
    /// Creation date
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Last accessed date
    /// </summary>
    public DateTime LastAccessedDate { get; set; }

    /// <summary>
    /// Whether the secret is scheduled for deletion
    /// </summary>
    public bool IsScheduledForDeletion { get; set; }

    /// <summary>
    /// Next rotation date (if rotation is enabled)
    /// </summary>
    public DateTime? NextRotationDate { get; set; }
}
