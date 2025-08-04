using CleanArchTemplate.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CleanArchTemplate.Application.Features.Authentication.EventHandlers;

/// <summary>
/// Handler for UserCreatedEvent
/// </summary>
public class UserCreatedEventHandler : INotificationHandler<UserCreatedEvent>
{
    private readonly ILogger<UserCreatedEventHandler> _logger;

    public UserCreatedEventHandler(ILogger<UserCreatedEventHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handle(UserCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User created: {UserId} - {Email} ({FullName})", 
            notification.UserId, notification.Email, notification.FullName);

        // Here you could:
        // - Send welcome email
        // - Create user profile
        // - Initialize user preferences
        // - Send integration events to other bounded contexts
        // - Log audit trail

        await Task.CompletedTask;
    }
}