using Amazon.SQS;
using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Infrastructure.Caching;
using CleanArchTemplate.Infrastructure.Data;
using CleanArchTemplate.Infrastructure.Data.Contexts;
using CleanArchTemplate.Infrastructure.Data.Interceptors;
using CleanArchTemplate.Infrastructure.Data.Repositories;
using CleanArchTemplate.Infrastructure.Data.Seed;
using CleanArchTemplate.Infrastructure.Extensions;
using CleanArchTemplate.Infrastructure.Messaging;
using CleanArchTemplate.Infrastructure.Messaging.Handlers;
using CleanArchTemplate.Infrastructure.MultiTenancy;
using CleanArchTemplate.Infrastructure.Services;
using CleanArchTemplate.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;

namespace CleanArchTemplate.Infrastructure;

/// <summary>
/// Dependency injection configuration for Infrastructure layer
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Infrastructure layer services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add performance logging interceptor as singleton
        services.AddSingleton<PerformanceLoggingInterceptor>();
        
        // Add domain event dispatching interceptor as singleton
        services.AddSingleton<DomainEventDispatchingInterceptor>();

        // Add database context with connection pooling
        services.AddDbContextPool<ApplicationDbContext>((serviceProvider, options) =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            var performanceInterceptor = serviceProvider.GetRequiredService<PerformanceLoggingInterceptor>();
            var domainEventInterceptor = serviceProvider.GetRequiredService<DomainEventDispatchingInterceptor>();
            
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
                npgsqlOptions.CommandTimeout(30);
            });

            // Add interceptors
            options.AddInterceptors(performanceInterceptor, domainEventInterceptor);

            // Configure EF Core options
            options.EnableServiceProviderCaching();
            options.EnableSensitiveDataLogging(false);
        }, poolSize: 100);

        // Add telemetry service
        services.AddSingleton<ITelemetryService, TelemetryService>();

        // Add repositories (telemetry will be handled by EF Core instrumentation)
        services.AddScoped<Domain.Interfaces.IUserRepository, UserRepository>();
        services.AddScoped<Domain.Interfaces.IRoleRepository, RoleRepository>();
        services.AddScoped<Domain.Interfaces.IPermissionRepository, PermissionRepository>();
        services.AddScoped<Domain.Interfaces.IUserPermissionRepository, UserPermissionRepository>();
        services.AddScoped<Domain.Interfaces.IRolePermissionRepository, RolePermissionRepository>();
        services.AddScoped<Domain.Interfaces.IPermissionAuditLogRepository, PermissionAuditLogRepository>();
        services.AddScoped<Domain.Interfaces.IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<Domain.Interfaces.ITenantRepository, TenantRepository>();
        services.AddScoped<Domain.Interfaces.ITenantConfigurationRepository, TenantConfigurationRepository>();
        services.AddScoped<Domain.Interfaces.ITenantUsageMetricRepository, TenantUsageMetricRepository>();

        // Add domain services
        services.AddScoped<Domain.Services.IPermissionEvaluationService, Domain.Services.PermissionEvaluationService>();

        // Add Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ResilientUnitOfWork>();

        // Add database seeder
        services.AddScoped<DatabaseSeeder>();

        // Add services
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<IPermissionAuditService, PermissionAuditService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IPermissionAuthorizationService, PermissionAuthorizationService>();
        services.AddScoped<IPermissionMatrixService, PermissionMatrixService>();
        services.AddScoped<IPermissionSeedingService, PermissionSeedingService>();
        services.AddScoped<Application.Common.Services.IErrorLoggingService, ErrorLoggingService>();
        
        // Add caching services
        services.AddMemoryCache();
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
        });
        
        // Add cache service with telemetry decorator
        services.AddScoped<MemoryCacheService>();
        services.AddScoped<ICacheService>(provider =>
            new Decorators.TelemetryCacheServiceDecorator(
                provider.GetRequiredService<MemoryCacheService>(),
                provider.GetRequiredService<ITelemetryService>()));
        
        services.Configure<PermissionCacheOptions>(
            configuration.GetSection(PermissionCacheOptions.SectionName));
        services.AddScoped<IPermissionCacheService, PermissionCacheService>();
        
        // Add tenant-aware cache service
        services.AddScoped<ITenantCacheService, TenantCacheService>();
        
        // Add resilience services
        services.Configure<ResilienceSettings>(configuration.GetSection(ResilienceSettings.SectionName));
        services.AddSingleton<IResilienceService, ResilienceService>();
        
        // Add HttpClient for resilient services
        services.AddHttpClient();
        
        services.AddScoped<ResilientHttpService>();
        services.AddScoped<ResilientUserService>();
        services.AddScoped<ResilientHealthCheckService>();

        // Add AWS Secrets Manager services
        services.AddSecretsManager(configuration);
        
        // Add secret rotation detection if enabled
        services.AddSecretRotationDetection(configuration);
        
        // Add database credentials rotation service
        services.AddDatabaseCredentialsRotation();

        // Add messaging services
        services.AddMessaging(configuration);

        // Add multi-tenancy services
        services.AddMultiTenancy(configuration);

        return services;
    }

    /// <summary>
    /// Ensures the database is created and seeded
    /// </summary>
    /// <param name="serviceProvider">The service provider</param>
    /// <returns>Task representing the async operation</returns>
    public static async Task EnsureDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            logger.LogInformation("Ensuring database exists and is up to date...");

            // Apply any pending migrations (this will create the database if it doesn't exist)
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                logger.LogInformation("Applying {Count} pending migrations: {Migrations}", 
                    pendingMigrations.Count(), string.Join(", ", pendingMigrations));
                await context.Database.MigrateAsync();
                logger.LogInformation("Migrations applied successfully.");
            }
            else
            {
                logger.LogInformation("Database is up to date, no migrations to apply.");
            }

            // Seed the database
            var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
            await seeder.SeedAsync();

            logger.LogInformation("Database setup completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while setting up the database.");
            throw;
        }
    }

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
        
        // Only register messaging services if enabled
        if (messagingOptions?.Enabled != true)
        {
            // Register no-op implementations when messaging is disabled
            services.AddScoped<IMessagePublisher, NoOpMessagePublisher>();
            return services;
        }

        // Add AWS SQS client
        services.AddSingleton<IAmazonSQS>(serviceProvider =>
        {
            var messagingOptions = configuration.GetSection(MessagingOptions.SectionName).Get<MessagingOptions>() ?? new MessagingOptions();
            
            var config = new AmazonSQSConfig
            {
                RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(messagingOptions.AwsRegion)
            };

            // Use LocalStack endpoint for development if configured
            if (!string.IsNullOrEmpty(messagingOptions.LocalStackEndpoint))
            {
                config.ServiceURL = messagingOptions.LocalStackEndpoint;
                config.UseHttp = true;
            }

            // Create SQS client with credentials if provided
            if (!string.IsNullOrEmpty(messagingOptions.AwsAccessKey) && !string.IsNullOrEmpty(messagingOptions.AwsSecretKey))
            {
                return new AmazonSQSClient(messagingOptions.AwsAccessKey, messagingOptions.AwsSecretKey, config);
            }

            // Use default credential chain (IAM roles, environment variables, etc.)
            return new AmazonSQSClient(config);
        });

        // Add resilience pipeline for messaging
        services.AddSingleton<ResiliencePipeline>(serviceProvider =>
        {
            var messagingOptions = configuration.GetSection(MessagingOptions.SectionName).Get<MessagingOptions>() ?? new MessagingOptions();
            
            return new ResiliencePipelineBuilder()
                .AddRetry(new Polly.Retry.RetryStrategyOptions
                {
                    MaxRetryAttempts = messagingOptions.RetryPolicy.MaxRetryAttempts,
                    Delay = TimeSpan.FromMilliseconds(messagingOptions.RetryPolicy.InitialDelayMs),
                    MaxDelay = TimeSpan.FromMilliseconds(messagingOptions.RetryPolicy.MaxDelayMs),
                    BackoffType = DelayBackoffType.Exponential
                })
                .AddTimeout(TimeSpan.FromSeconds(30))
                .Build();
        });

        // Add messaging services with telemetry decorators
        services.AddScoped<SqsMessagePublisher>();
        services.AddScoped<IMessagePublisher>(provider =>
            new Decorators.TelemetryMessagePublisherDecorator(
                provider.GetRequiredService<SqsMessagePublisher>(),
                provider.GetRequiredService<ITelemetryService>()));
        services.AddScoped<IMessageConsumer, SqsMessageConsumer>();
        services.AddScoped<SqsQueueManager>();

        // Add message handlers
        services.AddScoped<UserMessageHandler>();
        services.AddScoped<PermissionMessageHandler>();

        // Add background service for message consumers
        services.AddHostedService<MessageConsumerService>();

        return services;
    }

    /// <summary>
    /// Adds multi-tenancy services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddMultiTenancy(this IServiceCollection services, IConfiguration configuration)
    {
        // Add tenant context as scoped service
        services.AddScoped<ITenantContext, MultiTenancy.TenantContext>();

        // Add tenant service
        services.AddScoped<ITenantService, MultiTenancy.TenantService>();
        
        // Add tenant management services
        services.AddScoped<ITenantConfigurationService, MultiTenancy.TenantConfigurationService>();
        services.AddScoped<ITenantFeatureService, MultiTenancy.TenantFeatureService>();
        services.AddScoped<ITenantUsageService, MultiTenancy.TenantUsageService>();

        // Add tenant resolvers
        services.AddScoped<MultiTenancy.Resolvers.SubdomainTenantResolver>();
        services.AddScoped<MultiTenancy.Resolvers.HeaderTenantResolver>();
        services.AddScoped<MultiTenancy.Resolvers.JwtTenantResolver>();

        // Add composite resolver as the main resolver
        services.AddScoped<IHttpTenantResolver>(provider =>
        {
            var resolvers = new List<IHttpTenantResolver>
            {
                provider.GetRequiredService<MultiTenancy.Resolvers.SubdomainTenantResolver>(),
                provider.GetRequiredService<MultiTenancy.Resolvers.HeaderTenantResolver>(),
                provider.GetRequiredService<MultiTenancy.Resolvers.JwtTenantResolver>()
            };

            return new MultiTenancy.Resolvers.CompositeTenantResolver(
                resolvers,
                provider.GetRequiredService<ILogger<MultiTenancy.Resolvers.CompositeTenantResolver>>());
        });

        // Also register as ITenantResolver for cases where only identifier resolution is needed
        services.AddScoped<ITenantResolver>(provider => provider.GetRequiredService<IHttpTenantResolver>());

        return services;
    }
}