using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Application.Common.Messages;
using CleanArchTemplate.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CleanArchTemplate.Application.Features.Roles.EventHandlers;

/// <summary>
/// Event handler for role-related domain events that publishes messages to SQS
/// </summary>
public class RoleMessagingEventHandler :
    INotificationHandler<RoleCreatedEvent>,
    INotificationHandler<RoleUpdatedEvent>,
    INotificationHandler<RoleActivatedEvent>,
    INotificationHandler<RoleDeactivatedEvent>,
    INotificationHandler<RolePermissionAssignedEvent>,
    INotificationHandler<RolePermissionRemovedEvent>,
    INotificationHandler<UserRoleAssignedEvent>,
    INotificationHandler<UserRoleRemovedEvent>
{
    private readonly IMessagePublisher _messagePublisher;
    private readonly ILogger<RoleMessagingEventHandler> _logger;

    public RoleMessagingEventHandler(
        IMessagePublisher messagePublisher,
        ILogger<RoleMessagingEventHandler> logger)
    {
        _messagePublisher = messagePublisher ?? throw new ArgumentNullException(nameof(messagePublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handle(RoleCreatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Publishing RoleCreated message for RoleId: {RoleId}", 
                notification.RoleId);

            var message = new RoleCreatedMessage
            {
                RoleId = notification.RoleId,
                Name = notification.Name,
                Description = notification.Description,
                CreatedAt = notification.OccurredOn
            };

            await _messagePublisher.PublishAsync(message, "role-events", cancellationToken);
            
            _logger.LogInformation("Successfully published RoleCreated message");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish RoleCreated message for RoleId: {RoleId}", 
                notification.RoleId);
            // Don't rethrow - messaging should not break the main flow
        }
    }

    public async Task Handle(RoleUpdatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Publishing RoleUpdated message for RoleId: {RoleId}", 
                notification.RoleId);

            var message = new RoleUpdatedMessage
            {
                RoleId = notification.RoleId,
                OldName = notification.OldName,
                NewName = notification.NewName,
                OldDescription = notification.OldDescription,
                NewDescription = notification.NewDescription,
                UpdatedAt = notification.OccurredOn
            };

            await _messagePublisher.PublishAsync(message, "role-events", cancellationToken);
            
            _logger.LogInformation("Successfully published RoleUpdated message");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish RoleUpdated message for RoleId: {RoleId}", 
                notification.RoleId);
        }
    }

    public async Task Handle(RoleActivatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Publishing RoleActivated message for RoleId: {RoleId}", 
                notification.RoleId);

            var message = new RoleStateChangedMessage
            {
                RoleId = notification.RoleId,
                Name = notification.Name,
                OldState = "Inactive",
                NewState = "Active",
                ChangedAt = notification.OccurredOn
            };

            await _messagePublisher.PublishAsync(message, "role-events", cancellationToken);
            
            _logger.LogInformation("Successfully published RoleActivated message");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish RoleActivated message for RoleId: {RoleId}", 
                notification.RoleId);
        }
    }

    public async Task Handle(RoleDeactivatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Publishing RoleDeactivated message for RoleId: {RoleId}", 
                notification.RoleId);

            var message = new RoleStateChangedMessage
            {
                RoleId = notification.RoleId,
                Name = notification.Name,
                OldState = "Active",
                NewState = "Inactive",
                ChangedAt = notification.OccurredOn
            };

            await _messagePublisher.PublishAsync(message, "role-events", cancellationToken);
            
            _logger.LogInformation("Successfully published RoleDeactivated message");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish RoleDeactivated message for RoleId: {RoleId}", 
                notification.RoleId);
        }
    }

    public async Task Handle(RolePermissionAssignedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Publishing RolePermissionAssigned message for RoleId: {RoleId}, PermissionId: {PermissionId}", 
                notification.RoleId, notification.PermissionId);

            var message = new RolePermissionAssignedMessage
            {
                RoleId = notification.RoleId,
                RoleName = notification.RoleName,
                PermissionId = notification.PermissionId,
                PermissionName = notification.PermissionName,
                AssignedAt = notification.OccurredOn
            };

            await _messagePublisher.PublishAsync(message, "role-permission-events", cancellationToken);
            
            _logger.LogInformation("Successfully published RolePermissionAssigned message");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish RolePermissionAssigned message for RoleId: {RoleId}, PermissionId: {PermissionId}", 
                notification.RoleId, notification.PermissionId);
        }
    }

    public async Task Handle(RolePermissionRemovedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Publishing RolePermissionRemoved message for RoleId: {RoleId}, PermissionId: {PermissionId}", 
                notification.RoleId, notification.PermissionId);

            var message = new RolePermissionRemovedMessage
            {
                RoleId = notification.RoleId,
                RoleName = notification.RoleName,
                PermissionId = notification.PermissionId,
                PermissionName = string.Empty, // RolePermissionRemovedEvent doesn't include PermissionName
                RemovedAt = notification.OccurredOn
            };

            await _messagePublisher.PublishAsync(message, "role-permission-events", cancellationToken);
            
            _logger.LogInformation("Successfully published RolePermissionRemoved message");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish RolePermissionRemoved message for RoleId: {RoleId}, PermissionId: {PermissionId}", 
                notification.RoleId, notification.PermissionId);
        }
    }

    public async Task Handle(UserRoleAssignedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Publishing UserRoleAssigned message for UserId: {UserId}, RoleId: {RoleId}", 
                notification.UserId, notification.RoleId);

            var message = new UserRoleAssignedMessage
            {
                UserId = notification.UserId,
                RoleId = notification.RoleId,
                RoleName = notification.RoleName,
                AssignedAt = notification.OccurredOn
            };

            await _messagePublisher.PublishAsync(message, "user-role-events", cancellationToken);
            
            _logger.LogInformation("Successfully published UserRoleAssigned message");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish UserRoleAssigned message for UserId: {UserId}, RoleId: {RoleId}", 
                notification.UserId, notification.RoleId);
        }
    }

    public async Task Handle(UserRoleRemovedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Publishing UserRoleRemoved message for UserId: {UserId}, RoleId: {RoleId}", 
                notification.UserId, notification.RoleId);

            var message = new UserRoleRemovedMessage
            {
                UserId = notification.UserId,
                RoleId = notification.RoleId,
                RemovedAt = notification.OccurredOn
            };

            await _messagePublisher.PublishAsync(message, "user-role-events", cancellationToken);
            
            _logger.LogInformation("Successfully published UserRoleRemoved message");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish UserRoleRemoved message for UserId: {UserId}, RoleId: {RoleId}", 
                notification.UserId, notification.RoleId);
        }
    }
}