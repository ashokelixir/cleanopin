using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Application.Common.Messages;
using CleanArchTemplate.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CleanArchTemplate.Application.Features.Permissions.EventHandlers;

/// <summary>
/// Event handler for permission-related domain events that publishes messages to SQS
/// </summary>
public class PermissionMessagingEventHandler :
    INotificationHandler<PermissionCreatedEvent>,
    INotificationHandler<PermissionUpdatedEvent>,
    INotificationHandler<PermissionActivatedEvent>,
    INotificationHandler<PermissionDeactivatedEvent>,
    INotificationHandler<UserPermissionAssignedEvent>,
    INotificationHandler<UserPermissionRemovedEvent>,
    INotificationHandler<UserPermissionUpdatedEvent>
{
    private readonly IMessagePublisher _messagePublisher;
    private readonly ILogger<PermissionMessagingEventHandler> _logger;

    public PermissionMessagingEventHandler(
        IMessagePublisher messagePublisher,
        ILogger<PermissionMessagingEventHandler> logger)
    {
        _messagePublisher = messagePublisher ?? throw new ArgumentNullException(nameof(messagePublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handle(PermissionCreatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Publishing PermissionCreated message for PermissionId: {PermissionId}", 
                notification.PermissionId);

            var message = new PermissionCreatedMessage
            {
                PermissionId = notification.PermissionId,
                Resource = notification.Resource,
                Action = notification.Action,
                Name = notification.Name,
                Description = notification.Description,
                Category = notification.Category,
                ParentPermissionId = notification.ParentPermissionId,
                CreatedAt = notification.OccurredOn
            };

            await _messagePublisher.PublishAsync(message, "permission-events", cancellationToken);
            
            _logger.LogInformation("Successfully published PermissionCreated message");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish PermissionCreated message for PermissionId: {PermissionId}", 
                notification.PermissionId);
            // Don't rethrow - messaging should not break the main flow
        }
    }

    public async Task Handle(PermissionUpdatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Publishing PermissionUpdated message for PermissionId: {PermissionId}", 
                notification.PermissionId);

            var message = new PermissionUpdatedMessage
            {
                PermissionId = notification.PermissionId,
                OldResource = notification.OldResource,
                NewResource = notification.NewResource,
                OldAction = notification.OldAction,
                NewAction = notification.NewAction,
                OldName = notification.OldName,
                NewName = notification.NewName,
                OldDescription = notification.OldDescription,
                NewDescription = notification.NewDescription,
                OldCategory = notification.OldCategory,
                NewCategory = notification.NewCategory,
                UpdatedAt = notification.OccurredOn
            };

            await _messagePublisher.PublishAsync(message, "permission-events", cancellationToken);
            
            _logger.LogInformation("Successfully published PermissionUpdated message");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish PermissionUpdated message for PermissionId: {PermissionId}", 
                notification.PermissionId);
        }
    }

    public async Task Handle(PermissionActivatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Publishing PermissionActivated message for PermissionId: {PermissionId}", 
                notification.PermissionId);

            var message = new PermissionStateChangedMessage
            {
                PermissionId = notification.PermissionId,
                Name = notification.Name,
                NewState = "Active",
                OldState = "Inactive",
                ChangedAt = notification.OccurredOn
            };

            await _messagePublisher.PublishAsync(message, "permission-events", cancellationToken);
            
            _logger.LogInformation("Successfully published PermissionActivated message");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish PermissionActivated message for PermissionId: {PermissionId}", 
                notification.PermissionId);
        }
    }

    public async Task Handle(PermissionDeactivatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Publishing PermissionDeactivated message for PermissionId: {PermissionId}", 
                notification.PermissionId);

            var message = new PermissionStateChangedMessage
            {
                PermissionId = notification.PermissionId,
                Name = notification.Name,
                NewState = "Inactive",
                OldState = "Active",
                ChangedAt = notification.OccurredOn
            };

            await _messagePublisher.PublishAsync(message, "permission-events", cancellationToken);
            
            _logger.LogInformation("Successfully published PermissionDeactivated message");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish PermissionDeactivated message for PermissionId: {PermissionId}", 
                notification.PermissionId);
        }
    }

    public async Task Handle(UserPermissionAssignedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Publishing UserPermissionAssigned message for UserId: {UserId}, PermissionId: {PermissionId}", 
                notification.UserId, notification.PermissionId);

            var message = new UserPermissionAssignedMessage
            {
                UserId = notification.UserId,
                PermissionId = notification.PermissionId,
                PermissionName = notification.PermissionName,
                State = notification.State.ToString(),
                Reason = notification.Reason,
                AssignedAt = notification.OccurredOn
            };

            await _messagePublisher.PublishAsync(message, "user-permission-events", cancellationToken);
            
            _logger.LogInformation("Successfully published UserPermissionAssigned message");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish UserPermissionAssigned message for UserId: {UserId}, PermissionId: {PermissionId}", 
                notification.UserId, notification.PermissionId);
        }
    }

    public async Task Handle(UserPermissionRemovedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Publishing UserPermissionRemoved message for UserId: {UserId}, PermissionId: {PermissionId}", 
                notification.UserId, notification.PermissionId);

            var message = new UserPermissionRemovedMessage
            {
                UserId = notification.UserId,
                PermissionId = notification.PermissionId,
                PermissionName = notification.PermissionName,
                State = notification.State.ToString(),
                Reason = notification.Reason,
                RemovedAt = notification.OccurredOn
            };

            await _messagePublisher.PublishAsync(message, "user-permission-events", cancellationToken);
            
            _logger.LogInformation("Successfully published UserPermissionRemoved message");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish UserPermissionRemoved message for UserId: {UserId}, PermissionId: {PermissionId}", 
                notification.UserId, notification.PermissionId);
        }
    }

    public async Task Handle(UserPermissionUpdatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Publishing UserPermissionUpdated message for UserId: {UserId}, PermissionId: {PermissionId}", 
                notification.UserId, notification.PermissionId);

            var message = new UserPermissionUpdatedMessage
            {
                UserId = notification.UserId,
                PermissionId = notification.PermissionId,
                OldState = notification.OldState.ToString(),
                NewState = notification.NewState.ToString(),
                OldReason = notification.OldReason,
                NewReason = notification.NewReason,
                UpdatedAt = notification.OccurredOn
            };

            await _messagePublisher.PublishAsync(message, "user-permission-events", cancellationToken);
            
            _logger.LogInformation("Successfully published UserPermissionUpdated message");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish UserPermissionUpdated message for UserId: {UserId}, PermissionId: {PermissionId}", 
                notification.UserId, notification.PermissionId);
        }
    }
}