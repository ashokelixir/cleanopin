using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Application.Common.Messages;
using CleanArchTemplate.Infrastructure.Messaging.Handlers;
using CleanArchTemplate.Shared.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CleanArchTemplate.Infrastructure.Messaging;

/// <summary>
/// Background service that starts message consumers for configured queues
/// </summary>
public class MessageConsumerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly MessagingOptions _options;
    private readonly ILogger<MessageConsumerService> _logger;

    public MessageConsumerService(
        IServiceProvider serviceProvider,
        IOptions<MessagingOptions> options,
        ILogger<MessageConsumerService> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting message consumer service");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var messageConsumer = scope.ServiceProvider.GetRequiredService<IMessageConsumer>();
            var userMessageHandler = scope.ServiceProvider.GetRequiredService<UserMessageHandler>();
            var permissionMessageHandler = scope.ServiceProvider.GetRequiredService<PermissionMessageHandler>();

            // Start consumers for configured queues
            var consumerTasks = new List<Task>();

            // User-related queues
            if (_options.Queues.ContainsKey("user-events"))
            {
                consumerTasks.Add(StartUserEventConsumers(messageConsumer, userMessageHandler, stoppingToken));
            }

            // Permission-related queues
            if (_options.Queues.ContainsKey("permission-events"))
            {
                consumerTasks.Add(StartPermissionEventConsumers(messageConsumer, permissionMessageHandler, stoppingToken));
            }

            // Wait for all consumers to complete
            await Task.WhenAll(consumerTasks);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Message consumer service was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in message consumer service");
            throw;
        }
    }

    private async Task StartUserEventConsumers(IMessageConsumer messageConsumer, UserMessageHandler userMessageHandler, CancellationToken cancellationToken)
    {
        try
        {
            // Start consumer for user created messages
            await messageConsumer.StartConsumingAsync<UserCreatedMessage>(
                "user-events",
                async (message, ct) => await userMessageHandler.HandleUserCreatedAsync(message, ct),
                cancellationToken);

            // Start consumer for user updated messages
            await messageConsumer.StartConsumingAsync<UserUpdatedMessage>(
                "user-events",
                async (message, ct) => await userMessageHandler.HandleUserUpdatedAsync(message, ct),
                cancellationToken);

            // Start consumer for user deleted messages
            await messageConsumer.StartConsumingAsync<UserDeletedMessage>(
                "user-events",
                async (message, ct) => await userMessageHandler.HandleUserDeletedAsync(message, ct),
                cancellationToken);

            _logger.LogInformation("Started user event consumers");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start user event consumers");
            throw;
        }
    }

    private async Task StartPermissionEventConsumers(IMessageConsumer messageConsumer, PermissionMessageHandler permissionMessageHandler, CancellationToken cancellationToken)
    {
        try
        {
            // Start consumer for permission assigned messages
            await messageConsumer.StartConsumingAsync<PermissionAssignedMessage>(
                "permission-events",
                async (message, ct) => await permissionMessageHandler.HandlePermissionAssignedAsync(message, ct),
                cancellationToken);

            // Start consumer for permission removed messages
            await messageConsumer.StartConsumingAsync<PermissionRemovedMessage>(
                "permission-events",
                async (message, ct) => await permissionMessageHandler.HandlePermissionRemovedAsync(message, ct),
                cancellationToken);

            // Start consumer for bulk permissions assigned messages
            await messageConsumer.StartConsumingAsync<BulkPermissionsAssignedMessage>(
                "permission-events",
                async (message, ct) => await permissionMessageHandler.HandleBulkPermissionsAssignedAsync(message, ct),
                cancellationToken);

            _logger.LogInformation("Started permission event consumers");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start permission event consumers");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping message consumer service");
        
        using var scope = _serviceProvider.CreateScope();
        var messageConsumer = scope.ServiceProvider.GetRequiredService<IMessageConsumer>();
        await messageConsumer.StopAllAsync();
        
        await base.StopAsync(cancellationToken);
    }
}