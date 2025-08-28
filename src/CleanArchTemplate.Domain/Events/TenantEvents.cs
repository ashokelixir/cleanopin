using CleanArchTemplate.Domain.Common;

namespace CleanArchTemplate.Domain.Events;

/// <summary>
/// Domain event raised when a tenant is created
/// </summary>
public sealed class TenantCreatedEvent : BaseDomainEvent
{
    public Guid TenantId { get; }
    public string Name { get; }
    public string Identifier { get; }

    public TenantCreatedEvent(Guid tenantId, string name, string identifier)
    {
        TenantId = tenantId;
        Name = name;
        Identifier = identifier;
    }
}

/// <summary>
/// Domain event raised when a tenant is updated
/// </summary>
public sealed class TenantUpdatedEvent : BaseDomainEvent
{
    public Guid TenantId { get; }
    public string OldName { get; }
    public string NewName { get; }

    public TenantUpdatedEvent(Guid tenantId, string oldName, string newName)
    {
        TenantId = tenantId;
        OldName = oldName;
        NewName = newName;
    }
}

/// <summary>
/// Domain event raised when a tenant's connection string is updated
/// </summary>
public sealed class TenantConnectionStringUpdatedEvent : BaseDomainEvent
{
    public Guid TenantId { get; }
    public string Name { get; }

    public TenantConnectionStringUpdatedEvent(Guid tenantId, string name)
    {
        TenantId = tenantId;
        Name = name;
    }
}

/// <summary>
/// Domain event raised when a tenant's subscription is updated
/// </summary>
public sealed class TenantSubscriptionUpdatedEvent : BaseDomainEvent
{
    public Guid TenantId { get; }
    public string Name { get; }
    public DateTime? ExpiresAt { get; }

    public TenantSubscriptionUpdatedEvent(Guid tenantId, string name, DateTime? expiresAt)
    {
        TenantId = tenantId;
        Name = name;
        ExpiresAt = expiresAt;
    }
}

/// <summary>
/// Domain event raised when a tenant is activated
/// </summary>
public sealed class TenantActivatedEvent : BaseDomainEvent
{
    public Guid TenantId { get; }
    public string Name { get; }
    public string Identifier { get; }

    public TenantActivatedEvent(Guid tenantId, string name, string identifier)
    {
        TenantId = tenantId;
        Name = name;
        Identifier = identifier;
    }
}

/// <summary>
/// Domain event raised when a tenant is deactivated
/// </summary>
public sealed class TenantDeactivatedEvent : BaseDomainEvent
{
    public Guid TenantId { get; }
    public string Name { get; }
    public string Identifier { get; }

    public TenantDeactivatedEvent(Guid tenantId, string name, string identifier)
    {
        TenantId = tenantId;
        Name = name;
        Identifier = identifier;
    }
}