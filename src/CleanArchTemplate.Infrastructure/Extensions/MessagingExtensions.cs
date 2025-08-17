using Amazon.SQS;
using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Infrastructure.Messaging;
using CleanArchTemplate.Infrastructure.Messaging.Handlers;
using CleanArchTemplate.Shared.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CleanArchTemplate.Infrastructure.Extensions;

/// <summary>
/// Extension methods for configuring messaging services
/// </summary>
public static class MessagingExtensions
{
    /// <summary>
    /// Adds messaging services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure messaging options
        services.Configure<MessagingOptions>(configuration.GetSection(MessagingOptions.SectionName));
        
        // Get messaging options to check if messaging is enabled
        var messagingOptions = configuration.GetSection(MessagingOptions.SectionName).Get<MessagingOptions>();
        
        // Only register messaging services if enabled and LocalStack endpoint is configured or we're in production
        if (messagingOptions?.Enabled == true && 
            (!string.IsNullOrEmpty(messagingOptions?.LocalStackEndpoint) || 
             !string.IsNullOrEmpty(messagingOptions?.AwsAccessKey)))
        {
            // Configure AWS SQS client
            services.AddSingleton<IAmazonSQS>(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<MessagingOptions>>().Value;
                
                var config = new AmazonSQSConfig
                {
                    RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(options.AwsRegion)
                };
                
                // Use LocalStack endpoint if configured (for development)
                if (!string.IsNullOrEmpty(options.LocalStackEndpoint))
                {
                    config.ServiceURL = options.LocalStackEndpoint;
                    config.UseHttp = true;
                    return new AmazonSQSClient("test", "test", config);
                }
                
                // Use AWS credentials if provided
                if (!string.IsNullOrEmpty(options.AwsAccessKey) && !string.IsNullOrEmpty(options.AwsSecretKey))
                {
                    return new AmazonSQSClient(options.AwsAccessKey, options.AwsSecretKey, config);
                }
                
                // Use default AWS credentials (IAM role, environment variables, etc.)
                return new AmazonSQSClient(config);
            });
            
            // Register messaging services
            services.AddScoped<IMessagePublisher, SqsMessagePublisher>();
            services.AddScoped<IMessageConsumer, SqsMessageConsumer>();
            services.AddScoped<SqsQueueManager>();
            
            // Register message handlers
            services.AddScoped<UserMessageHandler>();
            services.AddScoped<PermissionMessageHandler>();
            
            // Register the background service for consuming messages
            services.AddHostedService<MessageConsumerService>();
        }
        
        return services;
    }
}