namespace CleanArchTemplate.Application.Common.Models;

public class PermissionDto : BaseAuditableDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public Guid? ParentPermissionId { get; set; }
    public IEnumerable<PermissionDto> ChildPermissions { get; set; } = new List<PermissionDto>();
}

public class PermissionSummaryDto : BaseDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}