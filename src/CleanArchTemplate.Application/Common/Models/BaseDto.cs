namespace CleanArchTemplate.Application.Common.Models;

public abstract class BaseDto
{
    public Guid Id { get; set; }
}

public abstract class BaseAuditableDto : BaseDto
{
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}