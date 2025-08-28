namespace CleanArchTemplate.Domain.Common;

/// <summary>
/// Interface for entities that belong to a tenant
/// </summary>
public interface ITenantEntity
{
    /// <summary>
    /// The ID of the tenant this entity belongs to
    /// </summary>
    Guid TenantId { get; set; }
}