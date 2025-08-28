using CleanArchTemplate.Domain.Common;
using CleanArchTemplate.Domain.Interfaces;

namespace CleanArchTemplate.Domain.Entities;

/// <summary>
/// Represents a tenant-specific configuration setting
/// </summary>
public class TenantConfiguration : BaseAuditableEntity, ITenantEntity
{
    /// <summary>
    /// The tenant ID this configuration belongs to
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// The configuration key
    /// </summary>
    public string Key { get; private set; } = string.Empty;

    /// <summary>
    /// The configuration value (stored as JSON)
    /// </summary>
    public string Value { get; private set; } = string.Empty;

    /// <summary>
    /// The data type of the configuration value
    /// </summary>
    public string DataType { get; private set; } = string.Empty;

    /// <summary>
    /// Optional description of the configuration setting
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Indicates if this is a system configuration (read-only)
    /// </summary>
    public bool IsSystemConfiguration { get; private set; }

    /// <summary>
    /// Navigation property to the tenant
    /// </summary>
    public Tenant Tenant { get; private set; } = null!;

    // Private constructor for EF Core
    private TenantConfiguration() { }

    /// <summary>
    /// Creates a new tenant configuration
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="key">The configuration key</param>
    /// <param name="value">The configuration value</param>
    /// <param name="dataType">The data type</param>
    /// <param name="description">Optional description</param>
    /// <param name="isSystemConfiguration">Whether this is a system configuration</param>
    /// <returns>A new tenant configuration instance</returns>
    public static TenantConfiguration Create(Guid tenantId, string key, string value, string dataType, 
        string? description = null, bool isSystemConfiguration = false)
    {
        return new TenantConfiguration(tenantId, key, value, dataType, description, isSystemConfiguration);
    }

    /// <summary>
    /// Creates a new tenant configuration
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="key">The configuration key</param>
    /// <param name="value">The configuration value</param>
    /// <param name="dataType">The data type</param>
    /// <param name="description">Optional description</param>
    /// <param name="isSystemConfiguration">Whether this is a system configuration</param>
    private TenantConfiguration(Guid tenantId, string key, string value, string dataType, 
        string? description = null, bool isSystemConfiguration = false)
    {
        TenantId = tenantId;
        Key = key?.Trim() ?? throw new ArgumentNullException(nameof(key));
        Value = value ?? throw new ArgumentNullException(nameof(value));
        DataType = dataType?.Trim() ?? throw new ArgumentNullException(nameof(dataType));
        Description = description?.Trim();
        IsSystemConfiguration = isSystemConfiguration;

        if (string.IsNullOrWhiteSpace(Key))
            throw new ArgumentException("Configuration key cannot be empty.", nameof(key));

        if (string.IsNullOrWhiteSpace(DataType))
            throw new ArgumentException("Data type cannot be empty.", nameof(dataType));

        ValidateKey(Key);
    }

    /// <summary>
    /// Updates the configuration value
    /// </summary>
    /// <param name="value">The new value</param>
    /// <param name="dataType">The new data type</param>
    /// <param name="description">The new description</param>
    public void UpdateValue(string value, string? dataType = null, string? description = null)
    {
        if (IsSystemConfiguration)
            throw new InvalidOperationException("System configurations cannot be modified.");

        Value = value ?? throw new ArgumentNullException(nameof(value));
        
        if (dataType != null)
        {
            DataType = dataType.Trim();
            if (string.IsNullOrWhiteSpace(DataType))
                throw new ArgumentException("Data type cannot be empty.", nameof(dataType));
        }

        if (description != null)
        {
            Description = description.Trim();
        }
    }

    /// <summary>
    /// Validates the configuration key format
    /// </summary>
    /// <param name="key">The key to validate</param>
    private static void ValidateKey(string key)
    {
        if (key.Length > 200)
            throw new ArgumentException("Configuration key cannot exceed 200 characters.", nameof(key));

        if (!System.Text.RegularExpressions.Regex.IsMatch(key, @"^[a-zA-Z][a-zA-Z0-9._-]*$"))
            throw new ArgumentException("Configuration key must start with a letter and can only contain letters, numbers, dots, underscores, and hyphens.", nameof(key));
    }
}