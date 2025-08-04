using CleanArchTemplate.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CleanArchTemplate.Application.Features.Authentication.EventHandlers;

/// <summary>
/// Handler for UserLoggedInEvent
/// </summary>
public class UserLoggedInEventHandler : INotificationHandler<UserLoggedInEvent>
{
    private readonly ILogger<UserLoggedInEventHandler> _logger;

    public UserLoggedInEventHandler(ILogger<UserLoggedInEventHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handle(UserLoggedInEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User logged in: {UserId} - {Email}", 
            notification.UserId, notification.Email);

        // Here you could:
        // - Update last login timestamp (already done in entity)
        // - Log security audit trail
        // - Update user activity metrics
        // - Send notifications to security monitoring
        // - Invalidate cached user data
        // - Track login analytics

        await Task.CompletedTask;
    }
}