using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CleanArchTemplate.Infrastructure.MultiTenancy;

/// <summary>
/// Service for tenant-specific configuration management
/// </summary>
public class TenantConfigurationService : ITenantConfigurationService
{
    private readonly ITenantConfigurationRepository _configurationRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<TenantConfigurationService> _logger;

    public TenantConfigurationService(
        ITenantConfigurationRepository configurationRepository,
        ITenantContext tenantContext,
        ILogger<TenantConfigurationService> logger)
    {
        _configurationRepository = configurationRepository;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>
    /// Gets a configuration value for the current tenant
    /// </summary>
    /// <typeparam name="T">The type of the configuration value</typeparam>
    /// <param name="key">The configuration key</param>
    /// <param name="defaultValue">The default value if not found</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The configuration value</returns>
    public async Task<T> GetConfigurationAsync<T>(string key, T defaultValue = default!, CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        return await GetConfigurationAsync(tenantId, key, defaultValue, cancellationToken);
    }

    /// <summary>
    /// Gets a configuration value for a specific tenant
    /// </summary>
    /// <typeparam name="T">The type of the configuration value</typeparam>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="key">The configuration key</param>
    /// <param name="defaultValue">The default value if not found</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The configuration value</returns>
    public async Task<T> GetConfigurationAsync<T>(Guid tenantId, string key, T defaultValue = default!, CancellationToken cancellationToken = default)
    {
        try
        {
            var configuration = await _configurationRepository.GetByTenantAndKeyAsync(tenantId, key, cancellationToken);
            
            if (configuration == null)
            {
                _logger.LogDebug("Configuration not found for tenant {TenantId} and key {Key}, returning default value", tenantId, key);
                return defaultValue;
            }

            return DeserializeValue<T>(configuration.Value, configuration.DataType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configuration for tenant {TenantId} and key {Key}", tenantId, key);
            return defaultValue;
        }
    }

    /// <summary>
    /// Sets a configuration value for the current tenant
    /// </summary>
    /// <typeparam name="T">The type of the configuration value</typeparam>
    /// <param name="key">The configuration key</param>
    /// <param name="value">The configuration value</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task SetConfigurationAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        await SetConfigurationAsync(tenantId, key, value, cancellationToken);
    }

    /// <summary>
    /// Sets a configuration value for a specific tenant
    /// </summary>
    /// <typeparam name="T">The type of the configuration value</typeparam>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="key">The configuration key</param>
    /// <param name="value">The configuration value</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task SetConfigurationAsync<T>(Guid tenantId, string key, T value, CancellationToken cancellationToken = default)
    {
        try
        {
            var (serializedValue, dataType) = SerializeValue(value);
            var existingConfiguration = await _configurationRepository.GetByTenantAndKeyAsync(tenantId, key, cancellationToken);

            if (existingConfiguration != null)
            {
                if (existingConfiguration.IsSystemConfiguration)
                {
                    throw new InvalidOperationException($"Cannot modify system configuration '{key}' for tenant {tenantId}");
                }

                existingConfiguration.UpdateValue(serializedValue, dataType);
                await _configurationRepository.UpdateAsync(existingConfiguration, cancellationToken);
                _logger.LogInformation("Updated configuration for tenant {TenantId} and key {Key}", tenantId, key);
            }
            else
            {
                var newConfiguration = TenantConfiguration.Create(tenantId, key, serializedValue, dataType);
                await _configurationRepository.AddAsync(newConfiguration, cancellationToken);
                _logger.LogInformation("Created new configuration for tenant {TenantId} and key {Key}", tenantId, key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting configuration for tenant {TenantId} and key {Key}", tenantId, key);
            throw;
        }
    }

    /// <summary>
    /// Gets all configuration values for the current tenant
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of configuration key-value pairs</returns>
    public async Task<Dictionary<string, object>> GetAllConfigurationAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        return await GetAllConfigurationAsync(tenantId, cancellationToken);
    }

    /// <summary>
    /// Gets all configuration values for a specific tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of configuration key-value pairs</returns>
    public async Task<Dictionary<string, object>> GetAllConfigurationAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var configurations = await _configurationRepository.GetByTenantAsync(tenantId, cancellationToken);
            var result = new Dictionary<string, object>();

            foreach (var config in configurations)
            {
                try
                {
                    var value = DeserializeValue<object>(config.Value, config.DataType);
                    result[config.Key] = value;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize configuration {Key} for tenant {TenantId}", config.Key, tenantId);
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all configurations for tenant {TenantId}", tenantId);
            return new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Removes a configuration value for the current tenant
    /// </summary>
    /// <param name="key">The configuration key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task RemoveConfigurationAsync(string key, CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        await RemoveConfigurationAsync(tenantId, key, cancellationToken);
    }

    /// <summary>
    /// Removes a configuration value for a specific tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="key">The configuration key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task RemoveConfigurationAsync(Guid tenantId, string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var configuration = await _configurationRepository.GetByTenantAndKeyAsync(tenantId, key, cancellationToken);
            
            if (configuration == null)
            {
                _logger.LogDebug("Configuration not found for tenant {TenantId} and key {Key}", tenantId, key);
                return;
            }

            if (configuration.IsSystemConfiguration)
            {
                throw new InvalidOperationException($"Cannot remove system configuration '{key}' for tenant {tenantId}");
            }

            await _configurationRepository.DeleteAsync(configuration.Id, cancellationToken);
            _logger.LogInformation("Removed configuration for tenant {TenantId} and key {Key}", tenantId, key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing configuration for tenant {TenantId} and key {Key}", tenantId, key);
            throw;
        }
    }

    /// <summary>
    /// Gets the current tenant ID from the tenant context
    /// </summary>
    /// <returns>The current tenant ID</returns>
    private Guid GetCurrentTenantId()
    {
        if (_tenantContext.CurrentTenant == null)
        {
            throw new InvalidOperationException("No tenant context available");
        }

        return _tenantContext.CurrentTenant.Id;
    }

    /// <summary>
    /// Serializes a value to JSON and determines its data type
    /// </summary>
    /// <typeparam name="T">The type of the value</typeparam>
    /// <param name="value">The value to serialize</param>
    /// <returns>Tuple of serialized value and data type</returns>
    private static (string SerializedValue, string DataType) SerializeValue<T>(T value)
    {
        if (value == null)
        {
            return ("null", "null");
        }

        var type = typeof(T);
        var dataType = type.Name;

        // Handle nullable types
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            dataType = Nullable.GetUnderlyingType(type)?.Name ?? "object";
        }

        var serializedValue = JsonSerializer.Serialize(value);
        return (serializedValue, dataType);
    }

    /// <summary>
    /// Deserializes a JSON value to the specified type
    /// </summary>
    /// <typeparam name="T">The target type</typeparam>
    /// <param name="serializedValue">The serialized JSON value</param>
    /// <param name="dataType">The original data type</param>
    /// <returns>The deserialized value</returns>
    private static T DeserializeValue<T>(string serializedValue, string dataType)
    {
        if (serializedValue == "null")
        {
            return default!;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(serializedValue) ?? default!;
        }
        catch (JsonException)
        {
            // If deserialization fails, try to convert from string
            if (typeof(T) == typeof(string))
            {
                return (T)(object)serializedValue;
            }

            throw;
        }
    }
}