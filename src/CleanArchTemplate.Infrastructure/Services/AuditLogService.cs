using CleanArchTemplate.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CleanArchTemplate.Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(ILogger<AuditLogService> logger)
    {
        _logger = logger;
    }

    public async Task LogUserActionAsync(string action, Guid userId, string? details = null, object? additionalData = null)
    {
        var auditLog = new
        {
            EventType = "UserAction",
            Action = action,
            UserId = userId,
            Details = details,
            AdditionalData = additionalData,
            Timestamp = DateTime.UtcNow
        };

        _logger.LogInformation("User action performed: {Action} by user {UserId}. Details: {Details}. Data: {@AdditionalData}",
            action, userId, details, additionalData);

        await Task.CompletedTask;
    }

    public async Task LogRoleActionAsync(string action, Guid roleId, Guid? performedByUserId = null, string? details = null, object? additionalData = null)
    {
        var auditLog = new
        {
            EventType = "RoleAction",
            Action = action,
            RoleId = roleId,
            PerformedByUserId = performedByUserId,
            Details = details,
            AdditionalData = additionalData,
            Timestamp = DateTime.UtcNow
        };

        _logger.LogInformation("Role action performed: {Action} on role {RoleId} by user {PerformedByUserId}. Details: {Details}. Data: {@AdditionalData}",
            action, roleId, performedByUserId, details, additionalData);

        await Task.CompletedTask;
    }

    public async Task LogPermissionActionAsync(string action, Guid permissionId, Guid? performedByUserId = null, string? details = null, object? additionalData = null)
    {
        var auditLog = new
        {
            EventType = "PermissionAction",
            Action = action,
            PermissionId = permissionId,
            PerformedByUserId = performedByUserId,
            Details = details,
            AdditionalData = additionalData,
            Timestamp = DateTime.UtcNow
        };

        _logger.LogInformation("Permission action performed: {Action} on permission {PermissionId} by user {PerformedByUserId}. Details: {Details}. Data: {@AdditionalData}",
            action, permissionId, performedByUserId, details, additionalData);

        await Task.CompletedTask;
    }

    public async Task LogSecurityEventAsync(string eventType, string description, Guid? userId = null, string? ipAddress = null, object? additionalData = null)
    {
        var auditLog = new
        {
            EventType = "SecurityEvent",
            SecurityEventType = eventType,
            Description = description,
            UserId = userId,
            IpAddress = ipAddress,
            AdditionalData = additionalData,
            Timestamp = DateTime.UtcNow
        };

        _logger.LogWarning("Security event: {EventType} - {Description}. User: {UserId}, IP: {IpAddress}. Data: {@AdditionalData}",
            eventType, description, userId, ipAddress, additionalData);

        await Task.CompletedTask;
    }

    public async Task LogDataAccessAsync(string operation, string entityType, Guid entityId, Guid? userId = null, object? changes = null)
    {
        var auditLog = new
        {
            EventType = "DataAccess",
            Operation = operation,
            EntityType = entityType,
            EntityId = entityId,
            UserId = userId,
            Changes = changes,
            Timestamp = DateTime.UtcNow
        };

        _logger.LogInformation("Data access: {Operation} on {EntityType} {EntityId} by user {UserId}. Changes: {@Changes}",
            operation, entityType, entityId, userId, changes);

        await Task.CompletedTask;
    }
}