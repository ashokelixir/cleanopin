using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Reflection;

namespace CleanArchTemplate.API.Extensions;

/// <summary>
/// Extension methods for configuring OpenTelemetry
/// </summary>
public static class OpenTelemetryExtensions
{
    /// <summary>
    /// Adds OpenTelemetry services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddOpenTelemetryServices(this IServiceCollection services, IConfiguration configuration)
    {
        var serviceName = Assembly.GetExecutingAssembly().GetName().Name ?? "CleanArchTemplate";
        var serviceVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        
        // Configure resource attributes
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(serviceName, serviceVersion)
            .AddAttributes(new Dictionary<string, object>
            {
                ["deployment.environment"] = environment,
                ["service.instance.id"] = Environment.MachineName,
                ["host.name"] = Environment.MachineName
            });

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .Clear()
                .AddService(serviceName, serviceVersion)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = environment,
                    ["service.instance.id"] = Environment.MachineName,
                    ["host.name"] = Environment.MachineName
                }))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource("CleanArchTemplate")
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.EnrichWithHttpRequest = (activity, request) =>
                        {
                            activity.SetTag("http.request.body.size", request.ContentLength);
                            activity.SetTag("http.request.header.user_agent", request.Headers.UserAgent.ToString());
                        };
                        options.EnrichWithHttpResponse = (activity, response) =>
                        {
                            activity.SetTag("http.response.body.size", response.ContentLength);
                        };
                    })
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.EnrichWithHttpRequestMessage = (activity, request) =>
                        {
                            activity.SetTag("http.client.request.body.size", request.Content?.Headers.ContentLength);
                        };
                        options.EnrichWithHttpResponseMessage = (activity, response) =>
                        {
                            activity.SetTag("http.client.response.body.size", response.Content.Headers.ContentLength);
                        };
                    })
                    .AddEntityFrameworkCoreInstrumentation(options =>
                    {
                        options.SetDbStatementForText = true;
                        options.SetDbStatementForStoredProcedure = true;
                        options.EnrichWithIDbCommand = (activity, command) =>
                        {
                            activity.SetTag("db.command.timeout", command.CommandTimeout);
                        };
                    });

                // Add Redis instrumentation if Redis is configured
                var redisConnectionString = configuration.GetConnectionString("Redis");
                if (!string.IsNullOrEmpty(redisConnectionString))
                {
                    tracing.AddRedisInstrumentation();
                }

                // Configure exporters based on environment
                if (environment == "Development")
                {
                    tracing.AddConsoleExporter();
                }
                else
                {
                    // Add OTLP exporter for production (can be configured for Datadog, Jaeger, etc.)
                    var otlpEndpoint = configuration["OpenTelemetry:OtlpEndpoint"];
                    if (!string.IsNullOrEmpty(otlpEndpoint))
                    {
                        tracing.AddOtlpExporter(options =>
                        {
                            options.Endpoint = new Uri(otlpEndpoint);
                            options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                        });
                    }
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter("CleanArchTemplate")
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                // Add custom meters
                metrics.AddMeter("CleanArchTemplate.Database");
                metrics.AddMeter("CleanArchTemplate.Cache");
                metrics.AddMeter("CleanArchTemplate.ExternalServices");

                // Configure exporters based on environment
                if (environment == "Development")
                {
                    metrics.AddConsoleExporter();
                }
                else
                {
                    // Add OTLP exporter for production
                    var otlpEndpoint = configuration["OpenTelemetry:OtlpEndpoint"];
                    if (!string.IsNullOrEmpty(otlpEndpoint))
                    {
                        metrics.AddOtlpExporter(options =>
                        {
                            options.Endpoint = new Uri(otlpEndpoint);
                            options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                        });
                    }
                }
            });

        return services;
    }

    /// <summary>
    /// Adds Datadog tracing services
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddDatadogTracing(this IServiceCollection services, IConfiguration configuration)
    {
        var datadogSection = configuration.GetSection("Datadog");
        var apiKey = datadogSection["ApiKey"];
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        
        if (!string.IsNullOrEmpty(apiKey) && environment != "Development")
        {
            // Configure Datadog environment variables
            Environment.SetEnvironmentVariable("DD_API_KEY", apiKey);
            Environment.SetEnvironmentVariable("DD_ENV", environment.ToLowerInvariant());
            Environment.SetEnvironmentVariable("DD_SERVICE", "cleanarch-template");
            Environment.SetEnvironmentVariable("DD_VERSION", Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0");
            
            // Enable automatic instrumentation
            Environment.SetEnvironmentVariable("DD_TRACE_ENABLED", "true");
            Environment.SetEnvironmentVariable("DD_RUNTIME_METRICS_ENABLED", "true");
            Environment.SetEnvironmentVariable("DD_LOGS_INJECTION", "true");
            
            // Configure service mapping
            Environment.SetEnvironmentVariable("DD_SERVICE_MAPPING", "postgresql:cleanarch-db,redis:cleanarch-cache,sqs:cleanarch-queue");
            
            // Configure sampling
            Environment.SetEnvironmentVariable("DD_TRACE_SAMPLE_RATE", datadogSection["SampleRate"] ?? "1.0");
            
            // Configure additional settings
            var agentHost = datadogSection["AgentHost"];
            var agentPort = datadogSection["AgentPort"];
            
            if (!string.IsNullOrEmpty(agentHost))
            {
                Environment.SetEnvironmentVariable("DD_AGENT_HOST", agentHost);
            }
            
            if (!string.IsNullOrEmpty(agentPort))
            {
                Environment.SetEnvironmentVariable("DD_TRACE_AGENT_PORT", agentPort);
            }
        }

        return services;
    }
}