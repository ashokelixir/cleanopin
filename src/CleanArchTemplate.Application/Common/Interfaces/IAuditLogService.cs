namespace CleanArchTemplate.Application.Common.Interfaces;

public interface IAuditLogService
{
    Task LogUserActionAsync(string action, Guid userId, string? details = null, object? additionalData = null);
    Task LogRoleActionAsync(string action, Guid roleId, Guid? performedByUserId = null, string? details = null, object? additionalData = null);
    Task LogPermissionActionAsync(string action, Guid permissionId, Guid? performedByUserId = null, string? details = null, object? additionalData = null);
    Task LogSecurityEventAsync(string eventType, string description, Guid? userId = null, string? ipAddress = null, object? additionalData = null);
    Task LogDataAccessAsync(string operation, string entityType, Guid entityId, Guid? userId = null, object? changes = null);
}