using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace CleanArchTemplate.Application.Features.Permissions.EventHandlers;

/// <summary>
/// Event handler for permission-related domain events that creates audit log entries
/// </summary>
public class PermissionAuditEventHandler :
    INotificationHandler<UserPermissionAssignedEvent>,
    INotificationHandler<UserPermissionRemovedEvent>,
    INotificationHandler<PermissionUpdatedEvent>,
    INotificationHandler<PermissionCreatedEvent>
{
    private readonly IPermissionAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<PermissionAuditEventHandler> _logger;

    public PermissionAuditEventHandler(
        IPermissionAuditService auditService,
        ICurrentUserService currentUserService,
        ILogger<PermissionAuditEventHandler> logger)
    {
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handle(UserPermissionAssignedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Handling UserPermissionAssignedEvent for UserId: {UserId}, PermissionId: {PermissionId}", 
                notification.UserId, notification.PermissionId);

            // For now, use a system identifier. In a real application, you would get this from the current user context
            var performedBy = GetCurrentUserIdentifier();

            await _auditService.LogUserPermissionAssignedAsync(
                notification.UserId,
                notification.PermissionId,
                performedBy,
                notification.Reason,
                cancellationToken);

            _logger.LogInformation("Successfully logged user permission assignment audit entry");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log user permission assignment audit entry for UserId: {UserId}, PermissionId: {PermissionId}", 
                notification.UserId, notification.PermissionId);
            // Don't rethrow - audit logging should not break the main flow
        }
    }

    public async Task Handle(UserPermissionRemovedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Handling UserPermissionRemovedEvent for UserId: {UserId}, PermissionId: {PermissionId}", 
                notification.UserId, notification.PermissionId);

            var performedBy = GetCurrentUserIdentifier();

            await _auditService.LogUserPermissionRemovedAsync(
                notification.UserId,
                notification.PermissionId,
                performedBy,
                notification.Reason,
                cancellationToken);

            _logger.LogInformation("Successfully logged user permission removal audit entry");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log user permission removal audit entry for UserId: {UserId}, PermissionId: {PermissionId}", 
                notification.UserId, notification.PermissionId);
        }
    }

    public async Task Handle(PermissionUpdatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Handling PermissionUpdatedEvent for PermissionId: {PermissionId}", 
                notification.PermissionId);

            var performedBy = GetCurrentUserIdentifier();
            var oldValue = $"Resource: {notification.OldResource}, Action: {notification.OldAction}, Description: {notification.OldDescription}";
            var newValue = $"Resource: {notification.NewResource}, Action: {notification.NewAction}, Description: {notification.NewDescription}";

            await _auditService.LogPermissionModifiedAsync(
                notification.PermissionId,
                oldValue,
                newValue,
                performedBy,
                "Permission details updated",
                cancellationToken);

            _logger.LogInformation("Successfully logged permission modification audit entry");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log permission modification audit entry for PermissionId: {PermissionId}", 
                notification.PermissionId);
        }
    }

    public async Task Handle(PermissionCreatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Handling PermissionCreatedEvent for PermissionId: {PermissionId}", 
                notification.PermissionId);

            var performedBy = GetCurrentUserIdentifier();
            var newValue = $"Resource: {notification.Resource}, Action: {notification.Action}, Description: {notification.Description}";

            await _auditService.LogPermissionModifiedAsync(
                notification.PermissionId,
                string.Empty,
                newValue,
                performedBy,
                "Permission created",
                cancellationToken);

            _logger.LogInformation("Successfully logged permission creation audit entry");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log permission creation audit entry for PermissionId: {PermissionId}", 
                notification.PermissionId);
        }
    }

    private string GetCurrentUserIdentifier()
    {
        return _currentUserService.GetAuditIdentifier();
    }
}