using CleanArchTemplate.Application.Common.Messages;
using Microsoft.Extensions.Logging;

namespace CleanArchTemplate.Infrastructure.Messaging.Handlers;

/// <summary>
/// Handler for user-related messages
/// </summary>
public class UserMessageHandler
{
    private readonly ILogger<UserMessageHandler> _logger;

    public UserMessageHandler(ILogger<UserMessageHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Handles user created messages
    /// </summary>
    /// <param name="message">The user created message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task HandleUserCreatedAsync(UserCreatedMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing user created message for user {UserId} ({Email})", 
                message.UserId, message.Email);

            // Example processing:
            // - Send welcome email
            // - Create user profile in external systems
            // - Initialize user preferences
            // - Trigger analytics events

            await SimulateProcessingAsync(cancellationToken);

            _logger.LogInformation("Successfully processed user created message for user {UserId}", message.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process user created message for user {UserId}", message.UserId);
            throw;
        }
    }

    /// <summary>
    /// Handles user updated messages
    /// </summary>
    /// <param name="message">The user updated message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task HandleUserUpdatedAsync(UserUpdatedMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing user updated message for user {UserId}. Updated fields: {UpdatedFields}", 
                message.UserId, string.Join(", ", message.UpdatedFields));

            // Example processing:
            // - Sync changes to external systems
            // - Update search indexes
            // - Invalidate caches
            // - Trigger audit logs

            await SimulateProcessingAsync(cancellationToken);

            _logger.LogInformation("Successfully processed user updated message for user {UserId}", message.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process user updated message for user {UserId}", message.UserId);
            throw;
        }
    }

    /// <summary>
    /// Handles user deleted messages
    /// </summary>
    /// <param name="message">The user deleted message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task HandleUserDeletedAsync(UserDeletedMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing user deleted message for user {UserId} ({Email}). Reason: {Reason}", 
                message.UserId, message.Email, message.DeletionReason ?? "Not specified");

            // Example processing:
            // - Clean up user data in external systems
            // - Archive user data
            // - Revoke access tokens
            // - Send deletion confirmation

            await SimulateProcessingAsync(cancellationToken);

            _logger.LogInformation("Successfully processed user deleted message for user {UserId}", message.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process user deleted message for user {UserId}", message.UserId);
            throw;
        }
    }

    private async Task SimulateProcessingAsync(CancellationToken cancellationToken)
    {
        // Simulate some processing time
        await Task.Delay(100, cancellationToken);
    }
}