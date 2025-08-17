using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CleanArchTemplate.Infrastructure.Extensions;

/// <summary>
/// Extension methods for permission seeding configuration
/// </summary>
public static class PermissionSeedingExtensions
{
    /// <summary>
    /// Adds permission seeding services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddPermissionSeeding(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IPermissionSeedingService, PermissionSeedingService>();
        
        // Configure permission seeding options
        services.Configure<PermissionSeedingOptions>(
            configuration.GetSection(PermissionSeedingOptions.SectionName));
        
        return services;
    }

    /// <summary>
    /// Seeds permissions based on environment and configuration
    /// </summary>
    /// <param name="serviceProvider">The service provider</param>
    /// <param name="environment">The hosting environment</param>
    /// <returns>Task representing the async operation</returns>
    public static async Task SeedPermissionsAsync(this IServiceProvider serviceProvider, IHostEnvironment environment)
    {
        using var scope = serviceProvider.CreateScope();
        var seedingService = scope.ServiceProvider.GetRequiredService<IPermissionSeedingService>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<IPermissionSeedingService>>();

        try
        {
            logger.LogInformation("Starting permission seeding for environment: {Environment}", environment.EnvironmentName);

            // Always seed default permissions
            await seedingService.SeedDefaultPermissionsAsync();

            // Seed environment-specific permissions
            await seedingService.SeedEnvironmentPermissionsAsync(environment.EnvironmentName);

            // Seed role-permission assignments
            await seedingService.SeedDefaultRolePermissionsAsync();

            // Validate permission integrity
            var isValid = await seedingService.ValidatePermissionIntegrityAsync();
            if (!isValid)
            {
                logger.LogWarning("Permission integrity validation failed after seeding");
            }

            // Clean up orphaned permissions if configured
            var shouldCleanup = configuration.GetValue<bool>("PermissionSeeding:CleanupOrphanedPermissions", false);
            if (shouldCleanup)
            {
                await seedingService.CleanupOrphanedPermissionsAsync();
            }

            logger.LogInformation("Permission seeding completed successfully for environment: {Environment}", environment.EnvironmentName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred during permission seeding for environment: {Environment}", environment.EnvironmentName);
            throw;
        }
    }
}

/// <summary>
/// Configuration options for permission seeding
/// </summary>
public class PermissionSeedingOptions
{
    public const string SectionName = "PermissionSeeding";

    /// <summary>
    /// Whether to automatically seed permissions on startup
    /// </summary>
    public bool AutoSeedOnStartup { get; set; } = true;

    /// <summary>
    /// Whether to clean up orphaned permissions during seeding
    /// </summary>
    public bool CleanupOrphanedPermissions { get; set; } = false;

    /// <summary>
    /// Whether to validate permission integrity after seeding
    /// </summary>
    public bool ValidateIntegrityAfterSeeding { get; set; } = true;

    /// <summary>
    /// Environment-specific permission configurations
    /// </summary>
    public Dictionary<string, EnvironmentPermissionConfig> EnvironmentConfigs { get; set; } = new();
}

/// <summary>
/// Environment-specific permission configuration
/// </summary>
public class EnvironmentPermissionConfig
{
    /// <summary>
    /// Whether to seed environment-specific permissions
    /// </summary>
    public bool SeedEnvironmentPermissions { get; set; } = true;

    /// <summary>
    /// Additional permissions to seed for this environment
    /// </summary>
    public List<PermissionDefinition> AdditionalPermissions { get; set; } = new();
}

/// <summary>
/// Permission definition for configuration
/// </summary>
public class PermissionDefinition
{
    /// <summary>
    /// Permission resource
    /// </summary>
    public string Resource { get; set; } = string.Empty;

    /// <summary>
    /// Permission action
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Permission description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Permission category
    /// </summary>
    public string Category { get; set; } = string.Empty;
}