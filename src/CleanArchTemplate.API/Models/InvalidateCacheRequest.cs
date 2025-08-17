namespace CleanArchTemplate.API.Models;

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