using Serilog;
using Serilog.Events;
using Serilog.Filters;
using Serilog.Formatting.Json;

namespace CleanArchTemplate.API.Configuration;

public static class SerilogConfiguration
{
    public static void ConfigureSerilog(this WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;
        var environment = builder.Environment;

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentName()
            .Enrich.WithMachineName()
            .Enrich.WithProcessId()
            .Enrich.WithThreadId()
            .Enrich.WithCorrelationId()
            .Enrich.WithProperty("Application", "CleanArchTemplate")
            .Enrich.WithProperty("Version", GetApplicationVersion())
            .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.Hosting"))
            .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.Mvc"))
            .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.Routing"))
            .WriteTo.Logger(lc => ConfigureConsoleSink(lc, environment))
            .WriteTo.Logger(lc => ConfigureFileSink(lc, environment))
            .WriteTo.Logger(lc => ConfigureSeqSink(lc, configuration, environment))
            .CreateLogger();

        builder.Host.UseSerilog();
    }

    private static void ConfigureConsoleSink(LoggerConfiguration loggerConfiguration, IWebHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            loggerConfiguration
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}",
                    restrictedToMinimumLevel: LogEventLevel.Debug);
        }
        else
        {
            loggerConfiguration
                .WriteTo.Console(
                    formatter: new JsonFormatter(),
                    restrictedToMinimumLevel: LogEventLevel.Information);
        }
    }

    private static void ConfigureFileSink(LoggerConfiguration loggerConfiguration, IWebHostEnvironment environment)
    {
        var logPath = environment.IsDevelopment() 
            ? "logs/cleanarch-.txt" 
            : "/app/logs/cleanarch-.txt";

        loggerConfiguration
            .WriteTo.File(
                path: logPath,
                formatter: new JsonFormatter(),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                restrictedToMinimumLevel: LogEventLevel.Information,
                shared: true,
                flushToDiskInterval: TimeSpan.FromSeconds(1));
    }

    private static void ConfigureSeqSink(LoggerConfiguration loggerConfiguration, IConfiguration configuration, IWebHostEnvironment environment)
    {
        var seqUrl = configuration["Serilog:WriteTo:1:Args:serverUrl"];
        var seqApiKey = configuration["Serilog:WriteTo:1:Args:apiKey"];

        if (!string.IsNullOrEmpty(seqUrl))
        {
            loggerConfiguration
                .WriteTo.Seq(
                    serverUrl: seqUrl,
                    apiKey: seqApiKey,
                    restrictedToMinimumLevel: LogEventLevel.Information);
        }
    }

    private static string GetApplicationVersion()
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version?.ToString() ?? "1.0.0";
    }

    public static void ConfigureRequestLogging(this WebApplication app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
            options.GetLevel = GetLogLevel;
            options.EnrichDiagnosticContext = EnrichFromRequest;
        });
    }

    private static LogEventLevel GetLogLevel(HttpContext ctx, double _, Exception? ex)
    {
        if (ex != null) return LogEventLevel.Error;

        return ctx.Response.StatusCode switch
        {
            >= 500 => LogEventLevel.Error,
            >= 400 => LogEventLevel.Warning,
            >= 300 => LogEventLevel.Information,
            _ => LogEventLevel.Information
        };
    }

    private static void EnrichFromRequest(IDiagnosticContext diagnosticContext, HttpContext httpContext)
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.FirstOrDefault());
        diagnosticContext.Set("ClientIP", GetClientIpAddress(httpContext));
        
        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            diagnosticContext.Set("UserId", httpContext.User.FindFirst("sub")?.Value);
            diagnosticContext.Set("UserEmail", httpContext.User.FindFirst("email")?.Value);
        }

        if (httpContext.Request.ContentLength.HasValue)
        {
            diagnosticContext.Set("RequestSize", httpContext.Request.ContentLength.Value);
        }

        diagnosticContext.Set("ResponseSize", httpContext.Response.ContentLength);
    }

    private static string? GetClientIpAddress(HttpContext context)
    {
        // Check for forwarded IP first (load balancer scenarios)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }
}