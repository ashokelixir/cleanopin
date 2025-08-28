using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Infrastructure.MultiTenancy;
using Serilog.Context;

namespace CleanArchTemplate.API.Middleware;

/// <summary>
/// Middleware for resolving and setting tenant context
/// </summary>
public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Processes the HTTP request and resolves tenant context
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <param name="tenantResolver">The tenant resolver service</param>
    /// <param name="tenantContext">The tenant context service</param>
    public async Task InvokeAsync(HttpContext context, IHttpTenantResolver tenantResolver, ITenantContext tenantContext)
    {
        try
        {
            // Skip tenant resolution for certain paths
            if (ShouldSkipTenantResolution(context.Request.Path))
            {
                _logger.LogDebug("Skipping tenant resolution for path: {Path}", context.Request.Path);
                await _next(context);
                return;
            }

            // Resolve tenant
            var tenant = await tenantResolver.ResolveTenantAsync(context);

            if (tenant == null)
            {
                _logger.LogWarning("No tenant could be resolved for request: {Method} {Path}", 
                    context.Request.Method, context.Request.Path);
                
                // For API endpoints, return 404 if no tenant is found
                if (IsApiEndpoint(context.Request.Path))
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync("Tenant not found");
                    return;
                }
            }
            else
            {
                // Check if tenant is available
                if (!tenant.IsAvailable())
                {
                    _logger.LogWarning("Tenant is not available: {TenantId} ({TenantName}) - Active: {IsActive}, Expired: {IsExpired}", 
                        tenant.Id, tenant.Name, tenant.IsActive, tenant.IsSubscriptionExpired());
                    
                    context.Response.StatusCode = tenant.IsActive ? 402 : 403; // Payment Required or Forbidden
                    await context.Response.WriteAsync(tenant.IsActive ? "Subscription expired" : "Tenant is inactive");
                    return;
                }

                // Set tenant context
                tenantContext.SetTenant(tenant);
                
                _logger.LogDebug("Tenant context set: {TenantId} ({TenantName})", tenant.Id, tenant.Name);
            }

            // Add tenant information to logging context
            using (LogContext.PushProperty("TenantId", tenant?.Id))
            using (LogContext.PushProperty("TenantName", tenant?.Name))
            using (LogContext.PushProperty("TenantIdentifier", tenant?.Identifier))
            {
                await _next(context);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in tenant middleware");
            
            // Clear tenant context on error
            tenantContext.ClearTenant();
            
            // Continue with the request but without tenant context
            await _next(context);
        }
        finally
        {
            // Clear tenant context after request
            tenantContext.ClearTenant();
        }
    }

    /// <summary>
    /// Determines if tenant resolution should be skipped for the given path
    /// </summary>
    /// <param name="path">The request path</param>
    /// <returns>True if tenant resolution should be skipped</returns>
    private static bool ShouldSkipTenantResolution(PathString path)
    {
        var pathValue = path.Value?.ToLowerInvariant() ?? string.Empty;
        
        // Skip for health checks, metrics, and other infrastructure endpoints
        var skipPaths = new[]
        {
            "/health",
            "/metrics",
            "/swagger",
            "/favicon.ico",
            "/.well-known",
            "/robots.txt"
        };

        return skipPaths.Any(skipPath => pathValue.StartsWith(skipPath));
    }

    /// <summary>
    /// Determines if the request path is an API endpoint
    /// </summary>
    /// <param name="path">The request path</param>
    /// <returns>True if the path is an API endpoint</returns>
    private static bool IsApiEndpoint(PathString path)
    {
        var pathValue = path.Value?.ToLowerInvariant() ?? string.Empty;
        return pathValue.StartsWith("/api/");
    }
}