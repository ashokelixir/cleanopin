using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CleanArchTemplate.Infrastructure.Services;

/// <summary>
/// Configuration provider that loads configuration from AWS Secrets Manager
/// </summary>
public class SecretsManagerConfigurationProvider : ConfigurationProvider
{
    private readonly SecretsManagerConfigurationSource _source;
    private readonly ILogger<SecretsManagerConfigurationProvider> _logger;

    public SecretsManagerConfigurationProvider(
        SecretsManagerConfigurationSource source,
        ILogger<SecretsManagerConfigurationProvider> logger)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Loads configuration data from AWS Secrets Manager
    /// </summary>
    public override void Load()
    {
        LoadAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Asynchronously loads configuration data from AWS Secrets Manager
    /// </summary>
    /// <returns>Task representing the async operation</returns>
    private async Task LoadAsync()
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using var client = new AmazonSecretsManagerClient(_source.Region);

            foreach (var secretName in _source.SecretNames)
            {
                try
                {
                    _logger.LogDebug("Loading secret {SecretName} from AWS Secrets Manager", secretName);

                    var request = new GetSecretValueRequest
                    {
                        SecretId = GetFullSecretName(secretName)
                    };

                    var response = await client.GetSecretValueAsync(request);
                    
                    if (string.IsNullOrEmpty(response.SecretString))
                    {
                        _logger.LogWarning("Secret {SecretName} is empty or null", secretName);
                        continue;
                    }

                    // Try to parse as JSON first
                    if (TryParseAsJson(response.SecretString, out var jsonData))
                    {
                        foreach (var kvp in jsonData)
                        {
                            var configKey = _source.KeyPrefix + kvp.Key;
                            data[configKey] = kvp.Value;
                            _logger.LogDebug("Loaded configuration key {Key} from secret {SecretName}", 
                                configKey, secretName);
                        }
                    }
                    else
                    {
                        // Treat as plain text
                        var configKey = _source.KeyPrefix + secretName.Replace("/", ":");
                        data[configKey] = response.SecretString;
                        _logger.LogDebug("Loaded configuration key {Key} as plain text from secret {SecretName}", 
                            configKey, secretName);
                    }

                    _logger.LogInformation("Successfully loaded secret {SecretName}", secretName);
                }
                catch (ResourceNotFoundException)
                {
                    _logger.LogWarning("Secret {SecretName} not found in AWS Secrets Manager", secretName);
                    
                    if (!_source.Optional)
                    {
                        throw new InvalidOperationException($"Required secret '{secretName}' not found in AWS Secrets Manager");
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

            Data = data;
            _logger.LogInformation("Successfully loaded {Count} configuration values from AWS Secrets Manager", 
                data.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configuration from AWS Secrets Manager");
            
            if (!_source.Optional)
            {
                throw;
            }
            
            Data = data; // Use whatever we managed to load
        }
    }

    /// <summary>
    /// Gets the full secret name with environment prefix
    /// </summary>
    /// <param name="secretName">The base secret name</param>
    /// <returns>The full secret name</returns>
    private string GetFullSecretName(string secretName)
    {
        return string.IsNullOrWhiteSpace(_source.Environment) 
            ? secretName 
            : $"{_source.Environment}/{secretName}";
    }

    /// <summary>
    /// Tries to parse a string as JSON and extract key-value pairs
    /// </summary>
    /// <param name="jsonString">The JSON string to parse</param>
    /// <param name="data">The extracted key-value pairs</param>
    /// <returns>True if parsing was successful, false otherwise</returns>
    private bool TryParseAsJson(string jsonString, out Dictionary<string, string> data)
    {
        data = new Dictionary<string, string>();

        try
        {
            using var document = JsonDocument.Parse(jsonString);
            
            foreach (var property in document.RootElement.EnumerateObject())
            {
                var value = property.Value.ValueKind switch
                {
                    JsonValueKind.String => property.Value.GetString(),
                    JsonValueKind.Number => property.Value.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    JsonValueKind.Null => null,
                    _ => property.Value.GetRawText()
                };

                if (value != null)
                {
                    data[property.Name] = value;
                }
            }

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
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
    /// Whether the secrets are optional (won't throw if not found)
    /// </summary>
    public bool Optional { get; set; } = true;

    /// <summary>
    /// Builds the configuration provider
    /// </summary>
    /// <param name="builder">The configuration builder</param>
    /// <returns>The configuration provider</returns>
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        var loggerFactory = LoggerFactory.Create(builder => { });
        var logger = loggerFactory.CreateLogger<SecretsManagerConfigurationProvider>();
        
        return new SecretsManagerConfigurationProvider(this, logger);
    }
}