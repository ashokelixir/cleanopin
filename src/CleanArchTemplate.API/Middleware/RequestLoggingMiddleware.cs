using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace CleanArchTemplate.API.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    private readonly TimeSpan _slowRequestThreshold;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _slowRequestThreshold = TimeSpan.FromMilliseconds(1000); // Log requests taking more than 1 second
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var originalBodyStream = context.Response.Body;

        try
        {
            // Log request details
            await LogRequestAsync(context);

            // Capture response for logging
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context);

            stopwatch.Stop();

            // Log response details
            await LogResponseAsync(context, stopwatch.Elapsed, responseBody);

            // Copy response back to original stream
            await responseBody.CopyToAsync(originalBodyStream);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await LogExceptionAsync(context, ex, stopwatch.Elapsed);
            throw;
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private async Task LogRequestAsync(HttpContext context)
    {
        var request = context.Request;
        var requestBody = await GetRequestBodyAsync(request);

        _logger.LogInformation(
            "HTTP Request: {Method} {Path} {QueryString} from {ClientIP}. Content-Length: {ContentLength}. Body: {RequestBody}",
            request.Method,
            request.Path,
            request.QueryString,
            GetClientIpAddress(context),
            request.ContentLength,
            requestBody);
    }

    private async Task LogResponseAsync(HttpContext context, TimeSpan duration, MemoryStream responseBody)
    {
        var response = context.Response;
        var responseBodyText = await GetResponseBodyAsync(responseBody);

        var logLevel = GetLogLevelForResponse(response.StatusCode, duration);

        _logger.Log(logLevel,
            "HTTP Response: {Method} {Path} responded {StatusCode} in {Duration}ms. Content-Length: {ContentLength}. Body: {ResponseBody}",
            context.Request.Method,
            context.Request.Path,
            response.StatusCode,
            duration.TotalMilliseconds,
            response.ContentLength,
            responseBodyText);

        // Log slow requests separately
        if (duration > _slowRequestThreshold)
        {
            _logger.LogWarning(
                "Slow HTTP request detected: {Method} {Path} took {Duration}ms",
                context.Request.Method,
                context.Request.Path,
                duration.TotalMilliseconds);
        }
    }

    private Task LogExceptionAsync(HttpContext context, Exception exception, TimeSpan duration)
    {
        _logger.LogError(exception,
            "HTTP Request failed: {Method} {Path} after {Duration}ms. Exception: {ExceptionMessage}",
            context.Request.Method,
            context.Request.Path,
            duration.TotalMilliseconds,
            exception.Message);
        
        return Task.CompletedTask;
    }

    private static async Task<string> GetRequestBodyAsync(HttpRequest request)
    {
        if (!request.Body.CanSeek || request.ContentLength == 0)
            return string.Empty;

        request.EnableBuffering();
        request.Body.Position = 0;

        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync().ConfigureAwait(false);
        request.Body.Position = 0;

        // Truncate large bodies for logging
        return body.Length > 1000 ? body[..1000] + "..." : body;
    }

    private static async Task<string> GetResponseBodyAsync(MemoryStream responseBody)
    {
        responseBody.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(responseBody, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync().ConfigureAwait(false);
        responseBody.Seek(0, SeekOrigin.Begin);

        // Truncate large bodies for logging
        return body.Length > 1000 ? body[..1000] + "..." : body;
    }

    private static LogLevel GetLogLevelForResponse(int statusCode, TimeSpan duration)
    {
        return statusCode switch
        {
            >= 500 => LogLevel.Error,
            >= 400 => LogLevel.Warning,
            _ when duration > TimeSpan.FromMilliseconds(1000) => LogLevel.Warning,
            _ => LogLevel.Information
        };
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

public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestLoggingMiddleware>();
    }
}