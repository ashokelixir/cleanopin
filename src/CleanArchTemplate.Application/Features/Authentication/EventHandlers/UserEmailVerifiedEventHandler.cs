using CleanArchTemplate.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CleanArchTemplate.Application.Features.Authentication.EventHandlers;

/// <summary>
/// Handler for UserEmailVerifiedEvent
/// </summary>
public class UserEmailVerifiedEventHandler : INotificationHandler<UserEmailVerifiedEvent>
{
    private readonly ILogger<UserEmailVerifiedEventHandler> _logger;

    public UserEmailVerifiedEventHandler(ILogger<UserEmailVerifiedEventHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handle(UserEmailVerifiedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User email verified: {UserId} - {Email}", 
            notification.UserId, notification.Email);

        // Here you could:
        // - Send welcome email after verification
        // - Activate premium features
        // - Update user status in external systems
        // - Send analytics events
        // - Update user preferences

        await Task.CompletedTask;
    }
}