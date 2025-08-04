namespace CleanArchTemplate.Domain.Common;

/// <summary>
/// Base auditable entity class that provides audit properties for all domain entities
/// </summary>
public abstract class BaseAuditableEntity : BaseEntity
{
    /// <summary>
    /// The date and time when the entity was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The user who created the entity
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// The date and time when the entity was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// The user who last updated the entity
    /// </summary>
    public string? UpdatedBy { get; set; }
}