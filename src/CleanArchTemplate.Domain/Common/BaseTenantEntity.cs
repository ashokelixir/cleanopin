namespace CleanArchTemplate.Domain.Common;

/// <summary>
/// Base entity class for tenant-aware entities
/// </summary>
public abstract class BaseTenantEntity : BaseAuditableEntity, ITenantEntity
{
    /// <summary>
    /// The ID of the tenant this entity belongs to
    /// </summary>
    public Guid TenantId { get; set; }
}