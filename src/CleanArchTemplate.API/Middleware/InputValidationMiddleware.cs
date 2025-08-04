using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CleanArchTemplate.API.Middleware;

public class InputValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<InputValidationMiddleware> _logger;

    // Patterns for detecting potentially malicious input - more specific to avoid false positives
    private static readonly Regex SqlInjectionPattern = new(
        @"(\b(SELECT\s+\*\s+FROM|INSERT\s+INTO|DELETE\s+FROM|UPDATE\s+\w+\s+SET|DROP\s+TABLE|ALTER\s+TABLE|UNION\s+SELECT)|(\'\s*OR\s+\'\d*\'\s*=\s*\'\d*)|(\'\s*;\s*DROP)|(\-\-\s)|(/\*.*\*/))",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex XssPattern = new(
        @"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>|javascript:|vbscript:|onload=|onerror=|onclick=",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex PathTraversalPattern = new(
        @"(\.\.[\\/])|(\.\.[%2f%5c])|(%2e%2e[\\/])|(%2e%2e[%2f%5c])",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public InputValidationMiddleware(RequestDelegate next, ILogger<InputValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip validation for certain paths (like health checks)
        if (ShouldSkipValidation(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // Validate query parameters
        if (context.Request.Query.Any() && !ValidateQueryParameters(context))
        {
            await WriteErrorResponse(context, "Invalid query parameters detected");
            return;
        }

        // Validate headers
        if (!ValidateHeaders(context))
        {
            await WriteErrorResponse(context, "Invalid headers detected");
            return;
        }

        // Validate request body for POST/PUT requests
        if (HasRequestBody(context.Request) && !await ValidateRequestBody(context))
        {
            await WriteErrorResponse(context, "Invalid request body detected");
            return;
        }

        await _next(context);
    }

    private static bool ShouldSkipValidation(PathString path)
    {
        var skipPaths = new[] { "/health", "/api/health", "/swagger", "/api-docs" };
        return skipPaths.Any(skipPath => path.StartsWithSegments(skipPath, StringComparison.OrdinalIgnoreCase));
    }

    private bool ValidateQueryParameters(HttpContext context)
    {
        foreach (var param in context.Request.Query)
        {
            foreach (var value in param.Value)
            {
                if (string.IsNullOrEmpty(value)) continue;

                if (ContainsMaliciousContent(value))
                {
                    _logger.LogWarning("Malicious query parameter detected: {Parameter}={Value}", param.Key, value);
                    return false;
                }
            }
        }
        return true;
    }

    private bool ValidateHeaders(HttpContext context)
    {
        // Only validate specific headers that are more likely to contain malicious content
        // Exclude User-Agent as it commonly contains legitimate text that triggers false positives
        var headersToValidate = new[] { "Referer", "X-Forwarded-For" };
        
        foreach (var headerName in headersToValidate)
        {
            if (context.Request.Headers.TryGetValue(headerName, out var headerValues))
            {
                foreach (var value in headerValues)
                {
                    if (string.IsNullOrEmpty(value)) continue;

                    if (ContainsMaliciousContent(value))
                    {
                        _logger.LogWarning("Malicious header detected: {Header}={Value}", headerName, value);
                        return false;
                    }
                }
            }
        }

        // Log all headers for debugging (can be removed in production)
        _logger.LogInformation("=== REQUEST HEADERS ===");
        foreach (var header in context.Request.Headers)
        {
            _logger.LogInformation("Header: {Name} = {Value}", header.Key, string.Join(", ", header.Value.ToArray()));
        }
        
        return true;
    }

    private async Task<bool> ValidateRequestBody(HttpContext context)
    {
        if (!context.Request.HasFormContentType && 
            context.Request.ContentType?.Contains("application/json") != true)
        {
            _logger.LogInformation("Skipping body validation for content type: {ContentType}", context.Request.ContentType);
            return true; // Skip validation for non-JSON, non-form content
        }

        try
        {
            context.Request.EnableBuffering();
            var body = await ReadRequestBodyAsync(context.Request);
            
            _logger.LogInformation("=== REQUEST BODY === {Body}", body);
            
            if (string.IsNullOrEmpty(body))
                return true;

            // Reset stream position for downstream middleware
            context.Request.Body.Position = 0;

            if (ContainsMaliciousContent(body))
            {
                _logger.LogWarning("Malicious request body detected: {Body}", body);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating request body");
            return false;
        }
    }

    private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }

    private static bool HasRequestBody(HttpRequest request)
    {
        return request.Method == HttpMethods.Post || 
               request.Method == HttpMethods.Put || 
               request.Method == HttpMethods.Patch;
    }

    private bool ContainsMaliciousContent(string input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        // Check for SQL injection patterns
        if (SqlInjectionPattern.IsMatch(input))
        {
            _logger.LogWarning("SQL injection pattern detected in input: {Input}", input);
            return true;
        }

        // Check for XSS patterns
        if (XssPattern.IsMatch(input))
        {
            _logger.LogWarning("XSS pattern detected in input: {Input}", input);
            return true;
        }

        // Check for path traversal patterns
        if (PathTraversalPattern.IsMatch(input))
        {
            _logger.LogWarning("Path traversal pattern detected in input: {Input}", input);
            return true;
        }

        // Check for excessive length (potential DoS)
        if (input.Length > 10000)
        {
            _logger.LogWarning("Excessive length detected in input: {Length} characters", input.Length);
            return true;
        }

        return false;
    }

    private static async Task WriteErrorResponse(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = "Invalid Request",
            message = message,
            timestamp = DateTime.UtcNow
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}

public static class InputValidationMiddlewareExtensions
{
    public static IApplicationBuilder UseInputValidation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<InputValidationMiddleware>();
    }
}