using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System.Text.Json;

namespace CleanArchTemplate.Infrastructure.Configuration;

/// <summary>
/// Configuration provider that loads configuration from AWS Secrets Manager
/// </summary>
public class SecretsManagerConfigurationProvider : ConfigurationProvider
{
    private readonly SecretsManagerConfigurationSource _source;
    private readonly ILogger<SecretsManagerConfigurationProvider> _logger;
    private readonly IAmazonSecretsManager _secretsManagerClient;

    public SecretsManagerConfigurationProvider(
        SecretsManagerConfigurationSource source,
        ILogger<SecretsManagerConfigurationProvider> logger)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _secretsManagerClient = new AmazonSecretsManagerClient(_source.Region);
    }

    public override void Load()
    {
        try
        {
            LoadAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configuration from AWS Secrets Manager");
            throw;
        }
    }

    private async Task LoadAsync()
    {
        try
        {
            // If specific secret names are provided, load those
            if (_source.SecretNames.Any())
            {
                foreach (var secretName in _source.SecretNames)
                {
                    await LoadSecretAsync(secretName);
                }
            }
            else
            {
                // Fallback to default behavior for backward compatibility
                await LoadDatabaseCredentialsAsync();
                await LoadJwtSettingsAsync();
                await LoadApplicationSecretsAsync();
            }
        }
        catch (Exception ex)
        {
            if (!_source.Optional)
            {
                _logger.LogError(ex, "Error loading secrets from AWS Secrets Manager");
                throw;
            }
            else
            {
                _logger.LogWarning(ex, "Failed to load optional secrets from AWS Secrets Manager");
            }
        }
    }

    private async Task LoadDatabaseCredentialsAsync()
    {
        try
        {
            var secretName = $"{_source.Environment}/database/credentials";
            var response = await _secretsManagerClient.GetSecretValueAsync(new GetSecretValueRequest
            {
                SecretId = secretName
            });

            if (!string.IsNullOrEmpty(response.SecretString))
            {
                var credentials = JsonSerializer.Deserialize<Dictionary<string, object>>(response.SecretString);
                if (credentials != null)
                {
                    foreach (var kvp in credentials)
                    {
                        Data[$"ConnectionStrings:Database:{kvp.Key}"] = kvp.Value?.ToString();
                    }
                }
            }
        }
        catch (ResourceNotFoundException)
        {
            _logger.LogWarning("Database credentials secret not found in AWS Secrets Manager");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load database credentials from AWS Secrets Manager");
        }
    }

    private async Task LoadJwtSettingsAsync()
    {
        try
        {
            var secretName = $"{_source.Environment}/jwt/settings";
            var response = await _secretsManagerClient.GetSecretValueAsync(new GetSecretValueRequest
            {
                SecretId = secretName
            });

            if (!string.IsNullOrEmpty(response.SecretString))
            {
                var jwtSettings = JsonSerializer.Deserialize<Dictionary<string, object>>(response.SecretString);
                if (jwtSettings != null)
                {
                    foreach (var kvp in jwtSettings)
                    {
                        Data[$"Jwt:{kvp.Key}"] = kvp.Value?.ToString();
                    }
                }
            }
        }
        catch (ResourceNotFoundException)
        {
            _logger.LogWarning("JWT settings secret not found in AWS Secrets Manager");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load JWT settings from AWS Secrets Manager");
        }
    }

    private async Task LoadApplicationSecretsAsync()
    {
        try
        {
            var secretName = $"{_source.Environment}/application/secrets";
            var response = await _secretsManagerClient.GetSecretValueAsync(new GetSecretValueRequest
            {
                SecretId = secretName
            });

            if (!string.IsNullOrEmpty(response.SecretString))
            {
                var appSecrets = JsonSerializer.Deserialize<Dictionary<string, object>>(response.SecretString);
                if (appSecrets != null)
                {
                    foreach (var kvp in appSecrets)
                    {
                        Data[$"AppSecrets:{kvp.Key}"] = kvp.Value?.ToString();
                    }
                }
            }
        }
        catch (ResourceNotFoundException)
        {
            _logger.LogWarning("Application secrets not found in AWS Secrets Manager");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load application secrets from AWS Secrets Manager");
        }
    }

    private async Task LoadSecretAsync(string secretName)
    {
        try
        {
            var fullSecretName = string.IsNullOrEmpty(_source.Environment) 
                ? secretName 
                : $"{_source.Environment}/{secretName}";

            var response = await _secretsManagerClient.GetSecretValueAsync(new GetSecretValueRequest
            {
                SecretId = fullSecretName
            });

            if (!string.IsNullOrEmpty(response.SecretString))
            {
                // Try to parse as JSON first
                try
                {
                    var secretData = JsonSerializer.Deserialize<Dictionary<string, object>>(response.SecretString);
                    if (secretData != null)
                    {
                        foreach (var kvp in secretData)
                        {
                            var key = string.IsNullOrEmpty(_source.KeyPrefix) 
                                ? kvp.Key 
                                : $"{_source.KeyPrefix}:{kvp.Key}";
                            Data[key] = kvp.Value?.ToString();
                        }
                    }
                }
                catch (JsonException)
                {
                    // If not JSON, treat as plain text
                    var key = string.IsNullOrEmpty(_source.KeyPrefix) 
                        ? secretName 
                        : $"{_source.KeyPrefix}:{secretName}";
                    Data[key] = response.SecretString;
                }
            }
        }
        catch (ResourceNotFoundException)
        {
            if (!_source.Optional)
            {
                _logger.LogError("Required secret {SecretName} not found in AWS Secrets Manager", secretName);
                throw;
            }
            else
            {
                _logger.LogWarning("Optional secret {SecretName} not found in AWS Secrets Manager", secretName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load secret {SecretName} from AWS Secrets Manager", secretName);
            if (!_source.Optional)
            {
                throw;
            }
        }
    }

    public override bool TryGet(string key, out string? value)
    {
        return Data.TryGetValue(key, out value);
    }

    public override void Set(string key, string? value)
    {
        Data[key] = value;
    }

    public override IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string? parentPath)
    {
        var prefix = parentPath == null ? string.Empty : parentPath + ConfigurationPath.KeyDelimiter;

        return Data
            .Where(kv => kv.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(kv => Segment(kv.Key, prefix.Length))
            .Concat(earlierKeys)
            .OrderBy(k => k, ConfigurationKeyComparer.Instance);
    }

    private static string Segment(string key, int prefixLength)
    {
        var indexOf = key.IndexOf(ConfigurationPath.KeyDelimiter, prefixLength, StringComparison.OrdinalIgnoreCase);
        return indexOf < 0 ? key.Substring(prefixLength) : key.Substring(prefixLength, indexOf - prefixLength);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _secretsManagerClient?.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Configuration source for AWS Secrets Manager
/// </summary>
public class SecretsManagerConfigurationSource : IConfigurationSource
{
    /// <summary>
    /// AWS region for Secrets Manager
    /// </summary>
    public Amazon.RegionEndpoint Region { get; set; } = Amazon.RegionEndpoint.USEast1;

    /// <summary>
    /// Environment prefix for secret names
    /// </summary>
    public string Environment { get; set; } = string.Empty;

    /// <summary>
    /// List of secret names to load
    /// </summary>
    public List<string> SecretNames { get; set; } = new();

    /// <summary>
    /// Prefix to add to configuration keys
    /// </summary>
    public string KeyPrefix { get; set; } = string.Empty;

    /// <summary>
    /// Whether the secrets are optional
    /// </summary>
    public bool Optional { get; set; } = true;

    /// <summary>
    /// Builds the configuration provider
    /// </summary>
    /// <param name="builder">Configuration builder</param>
    /// <returns>Configuration provider</returns>
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        var serviceProvider = builder.Properties.TryGetValue("ServiceProvider", out var sp) 
            ? (IServiceProvider)sp! 
            : throw new InvalidOperationException("ServiceProvider not found in configuration builder properties");
            
        var logger = serviceProvider.GetService(typeof(ILogger<SecretsManagerConfigurationProvider>)) as ILogger<SecretsManagerConfigurationProvider>
            ?? throw new InvalidOperationException("Logger not found in service provider");
            
        return new SecretsManagerConfigurationProvider(this, logger);
    }
}