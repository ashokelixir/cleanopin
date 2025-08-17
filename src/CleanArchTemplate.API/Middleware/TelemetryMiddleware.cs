using CleanArchTemplate.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http.Extensions;
using System.Diagnostics;

namespace CleanArchTemplate.API.Middleware;

/// <summary>
/// Middleware for automatic telemetry collection on HTTP requests
/// </summary>
public class TelemetryMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ITelemetryService _telemetryService;
    private readonly ILogger<TelemetryMiddleware> _logger;

    public TelemetryMiddleware(RequestDelegate next, ITelemetryService telemetryService, ILogger<TelemetryMiddleware> logger)
    {
        _next = next;
        _telemetryService = telemetryService;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var path = context.Request.Path.Value ?? "unknown";
        var method = context.Request.Method;
        var userAgent = context.Request.Headers.UserAgent.ToString();
        var remoteIpAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        
        // Start activity for this request
        using var activity = _telemetryService.StartActivity($"{method} {path}", ActivityKind.Server);
        
        // Add request tags
        _telemetryService.AddTag("http.method", method);
        _telemetryService.AddTag("http.url", context.Request.GetDisplayUrl());
        _telemetryService.AddTag("http.scheme", context.Request.Scheme);
        _telemetryService.AddTag("http.host", context.Request.Host.Value);
        _telemetryService.AddTag("http.path", path);
        _telemetryService.AddTag("http.query", context.Request.QueryString.Value);
        _telemetryService.AddTag("http.user_agent", userAgent);
        _telemetryService.AddTag("http.remote_addr", remoteIpAddress);
        
        // Add user information if available
        if (context.User.Identity?.IsAuthenticated == true)
        {
            _telemetryService.AddTag("user.id", context.User.FindFirst("sub")?.Value ?? context.User.FindFirst("id")?.Value);
            _telemetryService.AddTag("user.name", context.User.Identity.Name);
        }

        // Add correlation ID if available
        if (context.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId))
        {
            _telemetryService.AddTag("correlation.id", correlationId.ToString());
        }

        Exception? exception = null;
        int statusCode = 200;

        try
        {
            // Add event for request start
            _telemetryService.AddEvent("request.start");
            
            await _next(context);
            
            statusCode = context.Response.StatusCode;
        }
        catch (Exception ex)
        {
            exception = ex;
            statusCode = 500;
            
            // Record the exception
            _telemetryService.RecordException(ex,
                new KeyValuePair<string, object?>("http.method", method),
                new KeyValuePair<string, object?>("http.path", path),
                new KeyValuePair<string, object?>("http.status_code", statusCode));
            
            throw;
        }
        finally
        {
            stopwatch.Stop();
            
            // Add response tags
            _telemetryService.AddTag("http.status_code", statusCode);
            _telemetryService.AddTag("http.response.size", context.Response.ContentLength);
            
            // Determine if request was successful
            var isSuccess = exception == null && statusCode < 400;
            _telemetryService.AddTag("http.success", isSuccess);
            
            // Add event for request end
            _telemetryService.AddEvent("request.end", 
                new KeyValuePair<string, object?>("duration_ms", stopwatch.ElapsedMilliseconds));
            
            // Record metrics
            _telemetryService.RecordCounter("http_requests_total", 1,
                new KeyValuePair<string, object?>("method", method),
                new KeyValuePair<string, object?>("status_code", statusCode),
                new KeyValuePair<string, object?>("success", isSuccess));
                
            _telemetryService.RecordHistogram("http_request_duration_seconds", stopwatch.Elapsed.TotalSeconds,
                new KeyValuePair<string, object?>("method", method),
                new KeyValuePair<string, object?>("path", GetNormalizedPath(path)),
                new KeyValuePair<string, object?>("status_code", statusCode));

            // Record business metrics based on endpoint
            RecordBusinessMetrics(method, path, statusCode, stopwatch.Elapsed);
            
            _logger.LogInformation(
                "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMilliseconds}ms",
                method, path, statusCode, stopwatch.ElapsedMilliseconds);
        }
    }

    private void RecordBusinessMetrics(string method, string path, int statusCode, TimeSpan duration)
    {
        try
        {
            // Authentication metrics
            if (path.StartsWith("/api/auth", StringComparison.OrdinalIgnoreCase))
            {
                var operation = GetAuthOperation(path);
                if (!string.IsNullOrEmpty(operation))
                {
                    _telemetryService.RecordCounter("auth_operations_total", 1,
                        new KeyValuePair<string, object?>("operation", operation),
                        new KeyValuePair<string, object?>("success", statusCode < 400));
                        
                    _telemetryService.RecordHistogram("auth_operation_duration_seconds", duration.TotalSeconds,
                        new KeyValuePair<string, object?>("operation", operation));
                }
            }
            
            // User management metrics
            if (path.StartsWith("/api/users", StringComparison.OrdinalIgnoreCase))
            {
                _telemetryService.RecordCounter("user_operations_total", 1,
                    new KeyValuePair<string, object?>("method", method),
                    new KeyValuePair<string, object?>("success", statusCode < 400));
            }
            
            // Role management metrics
            if (path.StartsWith("/api/roles", StringComparison.OrdinalIgnoreCase))
            {
                _telemetryService.RecordCounter("role_operations_total", 1,
                    new KeyValuePair<string, object?>("method", method),
                    new KeyValuePair<string, object?>("success", statusCode < 400));
            }
            
            // Permission metrics
            if (path.StartsWith("/api/permissions", StringComparison.OrdinalIgnoreCase))
            {
                _telemetryService.RecordCounter("permission_operations_total", 1,
                    new KeyValuePair<string, object?>("method", method),
                    new KeyValuePair<string, object?>("success", statusCode < 400));
            }
            
            // Health check metrics
            if (path.StartsWith("/health", StringComparison.OrdinalIgnoreCase))
            {
                _telemetryService.RecordCounter("health_check_requests_total", 1,
                    new KeyValuePair<string, object?>("endpoint", path),
                    new KeyValuePair<string, object?>("status", statusCode < 400 ? "healthy" : "unhealthy"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record business metrics for {Method} {Path}", method, path);
        }
    }

    private static string GetAuthOperation(string path)
    {
        return path.ToLowerInvariant() switch
        {
            var p when p.Contains("/login") => "login",
            var p when p.Contains("/register") => "register",
            var p when p.Contains("/refresh") => "refresh",
            var p when p.Contains("/logout") => "logout",
            var p when p.Contains("/verify") => "verify",
            var p when p.Contains("/forgot") => "forgot_password",
            var p when p.Contains("/reset") => "reset_password",
            _ => "unknown"
        };
    }

    private static string GetNormalizedPath(string path)
    {
        // Normalize paths to remove IDs and other variable parts for better metric grouping
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var normalizedSegments = new List<string>();
        
        foreach (var segment in segments)
        {
            // Replace GUIDs and numeric IDs with placeholders
            if (Guid.TryParse(segment, out _))
            {
                normalizedSegments.Add("{id}");
            }
            else if (int.TryParse(segment, out _))
            {
                normalizedSegments.Add("{id}");
            }
            else
            {
                normalizedSegments.Add(segment);
            }
        }
        
        return "/" + string.Join("/", normalizedSegments);
    }
}