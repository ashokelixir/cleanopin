namespace CleanArchTemplate.API.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    public SecurityHeadersMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Remove server header
        context.Response.Headers.Remove("Server");

        // Add security headers
        var headers = context.Response.Headers;

        // X-Content-Type-Options
        headers.Append("X-Content-Type-Options", "nosniff");

        // X-Frame-Options
        headers.Append("X-Frame-Options", "DENY");

        // X-XSS-Protection
        headers.Append("X-XSS-Protection", "1; mode=block");

        // Referrer-Policy
        headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

        // Content Security Policy
        var cspPolicy = GetContentSecurityPolicy();
        headers.Append("Content-Security-Policy", cspPolicy);

        // Permissions Policy
        headers.Append("Permissions-Policy", "geolocation=(), microphone=(), camera=()");

        // X-Permitted-Cross-Domain-Policies
        headers.Append("X-Permitted-Cross-Domain-Policies", "none");

        await _next(context);
    }

    private string GetContentSecurityPolicy()
    {
        var isDevelopment = _configuration.GetValue<bool>("IsDevelopment");
        
        if (isDevelopment)
        {
            // More relaxed CSP for development (allows Swagger UI)
            return "default-src 'self'; " +
                   "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                   "style-src 'self' 'unsafe-inline'; " +
                   "img-src 'self' data: https:; " +
                   "font-src 'self' data:; " +
                   "connect-src 'self'; " +
                   "frame-ancestors 'none'; " +
                   "base-uri 'self'; " +
                   "form-action 'self'";
        }

        // Strict CSP for production
        return "default-src 'self'; " +
               "script-src 'self'; " +
               "style-src 'self'; " +
               "img-src 'self' data:; " +
               "font-src 'self'; " +
               "connect-src 'self'; " +
               "frame-ancestors 'none'; " +
               "base-uri 'self'; " +
               "form-action 'self'";
    }
}

public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecurityHeadersMiddleware>();
    }
}