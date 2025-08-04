using CleanArchTemplate.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CleanArchTemplate.Application.Features.Authentication.EventHandlers;

/// <summary>
/// Handler for UserPasswordResetRequestedEvent
/// </summary>
public class UserPasswordResetRequestedEventHandler : INotificationHandler<UserPasswordResetRequestedEvent>
{
    private readonly ILogger<UserPasswordResetRequestedEventHandler> _logger;

    public UserPasswordResetRequestedEventHandler(ILogger<UserPasswordResetRequestedEventHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handle(UserPasswordResetRequestedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Password reset requested for user: {UserId} - {Email}", 
            notification.UserId, notification.Email);

        // Here you could:
        // - Send password reset email with token
        // - Log security event
        // - Rate limit password reset requests
        // - Notify security team of suspicious activity
        // - Update user security metrics

        await Task.CompletedTask;
    }
}