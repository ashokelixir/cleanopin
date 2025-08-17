using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Application.Common.Messages;
using CleanArchTemplate.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CleanArchTemplate.Application.Features.Users.EventHandlers;

/// <summary>
/// Event handler for user-related domain events that publishes messages to SQS
/// </summary>
public class UserMessagingEventHandler :
    INotificationHandler<UserCreatedEvent>,
    INotificationHandler<UserProfileUpdatedEvent>,
    INotificationHandler<UserEmailUpdatedEvent>,
    INotificationHandler<UserEmailVerifiedEvent>,
    INotificationHandler<UserPasswordUpdatedEvent>,
    INotificationHandler<UserActivatedEvent>,
    INotificationHandler<UserDeactivatedEvent>
{
    private readonly IMessagePublisher _messagePublisher;
    private readonly ILogger<UserMessagingEventHandler> _logger;

    public UserMessagingEventHandler(
        IMessagePublisher messagePublisher,
        ILogger<UserMessagingEventHandler> logger)
    {
        _messagePublisher = messagePublisher ?? throw new ArgumentNullException(nameof(messagePublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handle(UserCreatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Publishing UserCreated message for UserId: {UserId}", 
                notification.UserId);

            var message = new UserCreatedMessage
            {
                UserId = notification.UserId,
                Email = notification.Email,
                // Parse FullName into FirstName and LastName (simple split)
                FirstName = notification.FullName.Split(' ').FirstOrDefault() ?? string.Empty,
                LastName = notification.FullName.Split(' ').Skip(1).FirstOrDefault() ?? string.Empty,
                IsEmailVerified = false, // Default value, can be updated based on business logic
                CreatedAt = notification.OccurredOn
            };

            await _messagePublisher.PublishAsync(message, "user-events", cancellationToken);
            
            _logger.LogInformation("Successfully published UserCreated message");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish UserCreated message for UserId: {UserId}", 
                notification.UserId);
            // Don't rethrow - messaging should not break the main flow
        }
    }

    public async Task Handle(UserProfileUpdatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Publishing UserProfileUpdated message for UserId: {UserId}", 
                notification.UserId);

            var message = new UserProfileUpdatedMessage
            {
                UserId = notification.UserId,
                OldFullName = notification.OldFullName,
                NewFullName = notification.NewFullName,
                UpdatedAt = notification.OccurredOn
            };

            await _messagePublisher.PublishAsync(message, "user-events", cancellationToken);
            
            _logger.LogInformation("Successfully published UserProfileUpdated message");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish UserProfileUpdated message for UserId: {UserId}", 
                notification.UserId);
        }
    }

    public async Task Handle(UserEmailUpdatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Publishing UserEmailUpdated message for UserId: {UserId}", 
                notification.UserId);

            var message = new UserEmailUpdatedMessage
            {
                UserId = notification.UserId,
                OldEmail = notification.OldEmail,
                NewEmail = notification.NewEmail,
                UpdatedAt = notification.OccurredOn
            };

            await _messagePublisher.PublishAsync(message, "user-events", cancellationToken);
            
            _logger.LogInformation("Successfully published UserEmailUpdated message");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish UserEmailUpdated message for UserId: {UserId}", 
                notification.UserId);
        }
    }

    public async Task Handle(UserEmailVerifiedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Publishing UserEmailVerified message for UserId: {UserId}", 
                notification.UserId);

            var message = new UserEmailVerifiedMessage
            {
                UserId = notification.UserId,
                Email = notification.Email,
                VerifiedAt = notification.OccurredOn
            };

            await _messagePublisher.PublishAsync(message, "user-events", cancellationToken);
            
            _logger.LogInformation("Successfully published UserEmailVerified message");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish UserEmailVerified message for UserId: {UserId}", 
                notification.UserId);
        }
    }

    public async Task Handle(UserPasswordUpdatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Publishing UserPasswordChanged message for UserId: {UserId}", 
                notification.UserId);

            var message = new UserPasswordChangedMessage
            {
                UserId = notification.UserId,
                Email = string.Empty, // UserPasswordUpdatedEvent doesn't include email
                ChangedAt = notification.OccurredOn
            };

            await _messagePublisher.PublishAsync(message, "user-events", cancellationToken);
            
            _logger.LogInformation("Successfully published UserPasswordChanged message");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish UserPasswordChanged message for UserId: {UserId}", 
                notification.UserId);
        }
    }

    public async Task Handle(UserActivatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Publishing UserActivated message for UserId: {UserId}", 
                notification.UserId);

            var message = new UserActivatedMessage
            {
                UserId = notification.UserId,
                Email = notification.Email,
                ActivatedAt = notification.OccurredOn
            };

            await _messagePublisher.PublishAsync(message, "user-events", cancellationToken);
            
            _logger.LogInformation("Successfully published UserActivated message");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish UserActivated message for UserId: {UserId}", 
                notification.UserId);
        }
    }

    public async Task Handle(UserDeactivatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Publishing UserDeactivated message for UserId: {UserId}", 
                notification.UserId);

            var message = new UserDeactivatedMessage
            {
                UserId = notification.UserId,
                Email = notification.Email,
                DeactivatedAt = notification.OccurredOn
            };

            await _messagePublisher.PublishAsync(message, "user-events", cancellationToken);
            
            _logger.LogInformation("Successfully published UserDeactivated message");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish UserDeactivated message for UserId: {UserId}", 
                notification.UserId);
        }
    }
}