using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;

namespace CleanArchTemplate.API.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    
    // In-memory storage for rate limiting (use Redis in production)
    private static readonly ConcurrentDictionary<string, RateLimitInfo> _rateLimitStore = new();
    
    public RateLimitingMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientId = GetClientIdentifier(context);
        var endpoint = GetEndpointCategory(context.Request.Path);
        var key = $"{clientId}:{endpoint}";

        var rateLimitSettings = _configuration.GetSection("RateLimit");
        var (isAllowed, retryAfter) = IsRequestAllowed(key, endpoint, rateLimitSettings);

        if (!isAllowed)
        {
            _logger.LogWarning("Rate limit exceeded for client {ClientId} on endpoint {Endpoint}", clientId, endpoint);
            
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.Headers.Append("Retry-After", retryAfter.ToString());
            context.Response.ContentType = "application/json";

            var response = new
            {
                error = "Rate limit exceeded",
                message = "Too many requests. Please try again later.",
                retryAfter = retryAfter
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            return;
        }

        await _next(context);
    }

    private string GetClientIdentifier(HttpContext context)
    {
        // Check for authenticated user first
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            return $"user_{context.User.Identity.Name}";
        }

        // Fall back to IP address
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (ips.Length > 0)
            {
                return $"ip_{ips[0].Trim()}";
            }
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return $"ip_{realIp}";
        }

        return $"ip_{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
    }

    private string GetEndpointCategory(PathString path)
    {
        var pathValue = path.Value?.ToLower() ?? "";

        if (pathValue.Contains("/auth/"))
        {
            return "auth";
        }

        if (pathValue.Contains("/users/"))
        {
            return "users";
        }

        return "api";
    }

    private (bool isAllowed, int retryAfter) IsRequestAllowed(string key, string endpoint, IConfigurationSection rateLimitSettings)
    {
        var now = DateTime.UtcNow;
        var windowMinutes = endpoint switch
        {
            "auth" => rateLimitSettings.GetValue("AuthWindowMinutes", 15),
            "users" => rateLimitSettings.GetValue("UserWindowMinutes", 1),
            _ => rateLimitSettings.GetValue("GlobalWindowMinutes", 1)
        };

        var maxRequests = endpoint switch
        {
            "auth" => rateLimitSettings.GetValue("AuthPermitLimit", 10),
            "users" => rateLimitSettings.GetValue("UserPermitLimit", 200),
            _ => rateLimitSettings.GetValue("GlobalPermitLimit", 100)
        };

        var windowStart = now.AddMinutes(-windowMinutes);

        // Clean up old entries
        CleanupOldEntries(windowStart);

        var rateLimitInfo = _rateLimitStore.GetOrAdd(key, _ => new RateLimitInfo());

        lock (rateLimitInfo)
        {
            // Remove old requests outside the window
            rateLimitInfo.Requests.RemoveAll(r => r < windowStart);

            if (rateLimitInfo.Requests.Count >= maxRequests)
            {
                var oldestRequest = rateLimitInfo.Requests.Min();
                var retryAfter = (int)(oldestRequest.AddMinutes(windowMinutes) - now).TotalSeconds;
                return (false, Math.Max(retryAfter, 1));
            }

            rateLimitInfo.Requests.Add(now);
            return (true, 0);
        }
    }

    private void CleanupOldEntries(DateTime cutoff)
    {
        var keysToRemove = new List<string>();

        foreach (var kvp in _rateLimitStore)
        {
            lock (kvp.Value)
            {
                kvp.Value.Requests.RemoveAll(r => r < cutoff);
                if (kvp.Value.Requests.Count == 0)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
        }

        foreach (var key in keysToRemove)
        {
            _rateLimitStore.TryRemove(key, out _);
        }
    }
}

public class RateLimitInfo
{
    public List<DateTime> Requests { get; } = new();
}

public static class RateLimitingMiddlewareExtensions
{
    public static IApplicationBuilder UseRateLimitingMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RateLimitingMiddleware>();
    }
}