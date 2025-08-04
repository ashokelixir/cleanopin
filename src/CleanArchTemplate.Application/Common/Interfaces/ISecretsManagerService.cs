namespace CleanArchTemplate.Application.Common.Interfaces;

/// <summary>
/// Interface for AWS Secrets Manager service
/// </summary>
public interface ISecretsManagerService
{
    /// <summary>
    /// Retrieves a secret value by name
    /// </summary>
    /// <param name="secretName">The name of the secret</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The secret value as a string</returns>
    Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a secret value and deserializes it as JSON
    /// </summary>
    /// <typeparam name="T">The type to deserialize to</typeparam>
    /// <param name="secretName">The name of the secret</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The deserialized secret value</returns>
    Task<T?> GetSecretAsync<T>(string secretName, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Retrieves multiple secrets in a single call
    /// </summary>
    /// <param name="secretNames">The names of the secrets to retrieve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A dictionary of secret names and their values</returns>
    Task<Dictionary<string, string>> GetSecretsAsync(IEnumerable<string> secretNames, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates the cache for a specific secret
    /// </summary>
    /// <param name="secretName">The name of the secret to invalidate</param>
    void InvalidateCache(string secretName);

    /// <summary>
    /// Clears all cached secrets
    /// </summary>
    void ClearCache();
}