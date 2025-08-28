using CleanArchTemplate.Domain.Common;
using CleanArchTemplate.Domain.Events;

namespace CleanArchTemplate.Domain.Entities;

/// <summary>
/// Represents a tenant in the multi-tenant system
/// </summary>
public class Tenant : BaseAuditableEntity
{
    private readonly List<User> _users = new();
    private readonly List<Role> _roles = new();

    /// <summary>
    /// The tenant's display name
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// The tenant's unique identifier (used for subdomain, etc.)
    /// </summary>
    public string Identifier { get; private set; } = string.Empty;

    /// <summary>
    /// The tenant's database connection string (if using separate databases)
    /// </summary>
    public string? ConnectionString { get; private set; }

    /// <summary>
    /// JSON configuration specific to this tenant
    /// </summary>
    public string Configuration { get; private set; } = "{}";

    /// <summary>
    /// Indicates whether the tenant is active
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// The date and time when the tenant's subscription expires
    /// </summary>
    public DateTime? SubscriptionExpiresAt { get; private set; }

    /// <summary>
    /// Navigation property for users belonging to this tenant
    /// </summary>
    public IReadOnlyCollection<User> Users => _users.AsReadOnly();

    /// <summary>
    /// Navigation property for roles belonging to this tenant
    /// </summary>
    public IReadOnlyCollection<Role> Roles => _roles.AsReadOnly();

    // Private constructor for EF Core
    private Tenant() { }

    /// <summary>
    /// Creates a new tenant
    /// </summary>
    /// <param name="name">The tenant's display name</param>
    /// <param name="identifier">The tenant's unique identifier</param>
    /// <param name="connectionString">Optional database connection string</param>
    /// <param name="configuration">Optional JSON configuration</param>
    /// <returns>A new tenant instance</returns>
    public static Tenant Create(string name, string identifier, string? connectionString = null, string? configuration = null)
    {
        return new Tenant(name, identifier, connectionString, configuration);
    }

    /// <summary>
    /// Creates a new tenant
    /// </summary>
    /// <param name="name">The tenant's display name</param>
    /// <param name="identifier">The tenant's unique identifier</param>
    /// <param name="connectionString">Optional database connection string</param>
    /// <param name="configuration">Optional JSON configuration</param>
    private Tenant(string name, string identifier, string? connectionString = null, string? configuration = null)
    {
        Name = name?.Trim() ?? throw new ArgumentNullException(nameof(name));
        Identifier = identifier?.Trim().ToLowerInvariant() ?? throw new ArgumentNullException(nameof(identifier));
        ConnectionString = connectionString?.Trim();
        Configuration = configuration?.Trim() ?? "{}";

        if (string.IsNullOrWhiteSpace(Name))
            throw new ArgumentException("Tenant name cannot be empty.", nameof(name));

        if (string.IsNullOrWhiteSpace(Identifier))
            throw new ArgumentException("Tenant identifier cannot be empty.", nameof(identifier));

        ValidateIdentifier(Identifier);

        AddDomainEvent(new TenantCreatedEvent(Id, Name, Identifier));
    }

    /// <summary>
    /// Updates the tenant's information
    /// </summary>
    /// <param name="name">The new display name</param>
    /// <param name="configuration">The new configuration</param>
    public void Update(string name, string? configuration = null)
    {
        var oldName = Name;
        Name = name?.Trim() ?? throw new ArgumentNullException(nameof(name));
        
        if (string.IsNullOrWhiteSpace(Name))
            throw new ArgumentException("Tenant name cannot be empty.", nameof(name));

        if (configuration != null)
        {
            Configuration = configuration.Trim();
        }

        AddDomainEvent(new TenantUpdatedEvent(Id, oldName, Name));
    }

    /// <summary>
    /// Updates the tenant's connection string
    /// </summary>
    /// <param name="connectionString">The new connection string</param>
    public void UpdateConnectionString(string? connectionString)
    {
        ConnectionString = connectionString?.Trim();
        AddDomainEvent(new TenantConnectionStringUpdatedEvent(Id, Name));
    }

    /// <summary>
    /// Sets the tenant's subscription expiry date
    /// </summary>
    /// <param name="expiresAt">The expiry date</param>
    public void SetSubscriptionExpiry(DateTime? expiresAt)
    {
        SubscriptionExpiresAt = expiresAt;
        AddDomainEvent(new TenantSubscriptionUpdatedEvent(Id, Name, expiresAt));
    }

    /// <summary>
    /// Activates the tenant
    /// </summary>
    public void Activate()
    {
        if (IsActive)
            return;

        IsActive = true;
        AddDomainEvent(new TenantActivatedEvent(Id, Name, Identifier));
    }

    /// <summary>
    /// Deactivates the tenant
    /// </summary>
    public void Deactivate()
    {
        if (!IsActive)
            return;

        IsActive = false;
        AddDomainEvent(new TenantDeactivatedEvent(Id, Name, Identifier));
    }

    /// <summary>
    /// Checks if the tenant's subscription is expired
    /// </summary>
    /// <returns>True if the subscription is expired</returns>
    public bool IsSubscriptionExpired()
    {
        return SubscriptionExpiresAt.HasValue && SubscriptionExpiresAt.Value < DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the tenant is available for use
    /// </summary>
    /// <returns>True if the tenant is active and subscription is not expired</returns>
    public bool IsAvailable()
    {
        return IsActive && !IsSubscriptionExpired();
    }

    /// <summary>
    /// Validates the tenant identifier format
    /// </summary>
    /// <param name="identifier">The identifier to validate</param>
    private static void ValidateIdentifier(string identifier)
    {
        if (identifier.Length < 2 || identifier.Length > 50)
            throw new ArgumentException("Tenant identifier must be between 2 and 50 characters.", nameof(identifier));

        if (!System.Text.RegularExpressions.Regex.IsMatch(identifier, @"^[a-z0-9][a-z0-9-]*[a-z0-9]$"))
            throw new ArgumentException("Tenant identifier can only contain lowercase letters, numbers, and hyphens, and must start and end with alphanumeric characters.", nameof(identifier));

        var reservedIdentifiers = new[] { "www", "api", "admin", "app", "mail", "ftp", "localhost", "test", "staging", "prod", "production" };
        if (reservedIdentifiers.Contains(identifier))
            throw new ArgumentException($"Tenant identifier '{identifier}' is reserved and cannot be used.", nameof(identifier));
    }
}