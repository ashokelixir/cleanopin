using CleanArchTemplate.Infrastructure.Data.Contexts;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace CleanArchTemplate.API.Extensions;

/// <summary>
/// Extension methods for configuring health checks
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>
    /// Adds comprehensive health checks to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddComprehensiveHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var healthChecksBuilder = services.AddHealthChecks();

        // Database health check
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrEmpty(connectionString))
        {
            healthChecksBuilder.AddDbContextCheck<ApplicationDbContext>(
                name: "database",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "db", "postgresql" });
        }

        // Redis health check
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            healthChecksBuilder.AddRedis(
                redisConnectionString,
                name: "redis",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "cache", "redis" });
        }

        // AWS SQS health check (simplified for now - can be enhanced later)
        // Note: SQS health check requires specific configuration that may not be available in all environments

        // Memory health check
        healthChecksBuilder.AddCheck<MemoryHealthCheck>(
            name: "memory",
            failureStatus: HealthStatus.Degraded,
            tags: new[] { "memory", "system" });

        // Disk space health check
        healthChecksBuilder.AddCheck<DiskSpaceHealthCheck>(
            name: "disk_space",
            failureStatus: HealthStatus.Degraded,
            tags: new[] { "disk", "system" });

        // Application health check
        healthChecksBuilder.AddCheck<ApplicationHealthCheck>(
            name: "application",
            failureStatus: HealthStatus.Unhealthy,
            tags: new[] { "application", "startup" });

        return services;
    }

    /// <summary>
    /// Configures health check endpoints
    /// </summary>
    /// <param name="app">The web application</param>
    /// <returns>The web application</returns>
    public static WebApplication UseHealthCheckEndpoints(this WebApplication app)
    {
        // Basic health check endpoint
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            }
        });

        // Detailed health check endpoint
        app.MapHealthChecks("/health/detailed", new HealthCheckOptions
        {
            ResponseWriter = WriteDetailedHealthCheckResponse,
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            }
        });

        // Ready endpoint (for Kubernetes readiness probe)
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        // Live endpoint (for Kubernetes liveness probe)
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        return app;
    }

    private static async Task WriteDetailedHealthCheckResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var response = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                duration = entry.Value.Duration.TotalMilliseconds,
                description = entry.Value.Description,
                data = entry.Value.Data,
                exception = entry.Value.Exception?.Message,
                tags = entry.Value.Tags
            }),
            timestamp = DateTimeOffset.UtcNow,
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
            machineName = Environment.MachineName
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        await context.Response.WriteAsync(json);
    }
}

/// <summary>
/// Health check for memory usage
/// </summary>
public class MemoryHealthCheck : IHealthCheck
{
    private const long MaxMemoryBytes = 1024 * 1024 * 1024; // 1 GB

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var allocatedBytes = GC.GetTotalMemory(false);
        var data = new Dictionary<string, object>
        {
            ["allocated_bytes"] = allocatedBytes,
            ["allocated_mb"] = allocatedBytes / 1024 / 1024,
            ["gen0_collections"] = GC.CollectionCount(0),
            ["gen1_collections"] = GC.CollectionCount(1),
            ["gen2_collections"] = GC.CollectionCount(2)
        };

        var status = allocatedBytes < MaxMemoryBytes ? HealthStatus.Healthy : HealthStatus.Degraded;
        var description = $"Memory usage: {allocatedBytes / 1024 / 1024} MB";

        return Task.FromResult(new HealthCheckResult(status, description, data: data));
    }
}

/// <summary>
/// Health check for disk space
/// </summary>
public class DiskSpaceHealthCheck : IHealthCheck
{
    private const long MinFreeSpaceBytes = 1024 * 1024 * 1024; // 1 GB

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var drives = DriveInfo.GetDrives().Where(d => d.IsReady);
            var data = new Dictionary<string, object>();
            var allHealthy = true;

            foreach (var drive in drives)
            {
                var freeSpaceGB = drive.AvailableFreeSpace / 1024 / 1024 / 1024;
                var totalSpaceGB = drive.TotalSize / 1024 / 1024 / 1024;
                var usedSpaceGB = totalSpaceGB - freeSpaceGB;
                var usagePercentage = (double)usedSpaceGB / totalSpaceGB * 100;

                data[$"drive_{drive.Name.Replace(":", "").Replace("\\", "")}_free_gb"] = freeSpaceGB;
                data[$"drive_{drive.Name.Replace(":", "").Replace("\\", "")}_total_gb"] = totalSpaceGB;
                data[$"drive_{drive.Name.Replace(":", "").Replace("\\", "")}_usage_percent"] = Math.Round(usagePercentage, 2);

                if (drive.AvailableFreeSpace < MinFreeSpaceBytes)
                {
                    allHealthy = false;
                }
            }

            var status = allHealthy ? HealthStatus.Healthy : HealthStatus.Degraded;
            var description = $"Disk space check for {drives.Count()} drives";

            return Task.FromResult(new HealthCheckResult(status, description, data: data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new HealthCheckResult(HealthStatus.Unhealthy, "Failed to check disk space", ex));
        }
    }
}

/// <summary>
/// Health check for application-specific status
/// </summary>
public class ApplicationHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _serviceProvider;

    public ApplicationHealthCheck(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var data = new Dictionary<string, object>
            {
                ["startup_time"] = DateTime.UtcNow,
                ["environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                ["version"] = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown",
                ["machine_name"] = Environment.MachineName,
                ["process_id"] = Environment.ProcessId,
                ["thread_count"] = System.Diagnostics.Process.GetCurrentProcess().Threads.Count,
                ["working_set_mb"] = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024
            };

            // Check if critical services are registered
            var criticalServices = new[]
            {
                typeof(ApplicationDbContext),
                typeof(CleanArchTemplate.Application.Common.Interfaces.ITelemetryService)
            };

            var missingServices = new List<string>();
            foreach (var serviceType in criticalServices)
            {
                try
                {
                    _serviceProvider.GetService(serviceType);
                }
                catch
                {
                    missingServices.Add(serviceType.Name);
                }
            }

            if (missingServices.Any())
            {
                data["missing_services"] = missingServices;
                return Task.FromResult(new HealthCheckResult(
                    HealthStatus.Unhealthy,
                    $"Missing critical services: {string.Join(", ", missingServices)}",
                    data: data));
            }

            return Task.FromResult(new HealthCheckResult(HealthStatus.Healthy, "Application is healthy", data: data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new HealthCheckResult(HealthStatus.Unhealthy, "Application health check failed", ex));
        }
    }
}