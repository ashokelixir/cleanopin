using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Infrastructure.Data;
using CleanArchTemplate.Infrastructure.Data.Contexts;
using CleanArchTemplate.Infrastructure.Data.Interceptors;
using CleanArchTemplate.Infrastructure.Data.Repositories;
using CleanArchTemplate.Infrastructure.Data.Seed;
using CleanArchTemplate.Infrastructure.Extensions;
using CleanArchTemplate.Infrastructure.Services;
using CleanArchTemplate.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

        // Add database context with connection pooling
        services.AddDbContextPool<ApplicationDbContext>((serviceProvider, options) =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            var performanceInterceptor = serviceProvider.GetRequiredService<PerformanceLoggingInterceptor>();
            
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
                npgsqlOptions.CommandTimeout(30);
            });

            // Add performance logging interceptor
            options.AddInterceptors(performanceInterceptor);

            // Configure EF Core options
            options.EnableServiceProviderCaching();
            options.EnableSensitiveDataLogging(false);
        }, poolSize: 100);

        // Add repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        // Add Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ResilientUnitOfWork>();

        // Add database seeder
        services.AddScoped<DatabaseSeeder>();

        // Add services
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        
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
}