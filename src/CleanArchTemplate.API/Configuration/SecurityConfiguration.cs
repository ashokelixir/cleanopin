using Microsoft.AspNetCore.HttpOverrides;
using System.Net;
using CleanArchTemplate.API.Middleware;

namespace CleanArchTemplate.API.Configuration;

public static class SecurityConfiguration
{
    public static IServiceCollection AddSecurityServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure HTTPS redirection
        services.AddHttpsRedirection(options =>
        {
            options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
            options.HttpsPort = 443;
        });

        // Configure HSTS (HTTP Strict Transport Security)
        services.AddHsts(options =>
        {
            options.Preload = true;
            options.IncludeSubDomains = true;
            options.MaxAge = TimeSpan.FromDays(365);
        });

        // Configure forwarded headers for reverse proxy scenarios
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        // Configure CORS
        services.AddCors(options =>
        {
            var corsSettings = configuration.GetSection("Cors");
            var allowedOrigins = corsSettings.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
            var allowedMethods = corsSettings.GetSection("AllowedMethods").Get<string[]>() ?? new[] { "GET", "POST", "PUT", "DELETE", "OPTIONS" };
            var allowedHeaders = corsSettings.GetSection("AllowedHeaders").Get<string[]>() ?? new[] { "Content-Type", "Authorization" };

            options.AddPolicy("DefaultCorsPolicy", policy =>
            {
                if (allowedOrigins.Length > 0 && !allowedOrigins.Contains("*"))
                {
                    policy.WithOrigins(allowedOrigins);
                }
                else
                {
                    policy.AllowAnyOrigin();
                }

                policy.WithMethods(allowedMethods)
                      .WithHeaders(allowedHeaders);

                if (!allowedOrigins.Contains("*"))
                {
                    policy.AllowCredentials();
                }
            });
        });

        return services;
    }

    public static IApplicationBuilder UseSecurityMiddleware(this IApplicationBuilder app, IWebHostEnvironment env)
    {
        // Use forwarded headers
        app.UseForwardedHeaders();

        // Use HSTS in production
        if (!env.IsDevelopment())
        {
            app.UseHsts();
        }

        // Use HTTPS redirection (only in production) - but skip health checks
        if (!env.IsDevelopment())
        {
            app.UseWhen(context => !IsHealthCheckPath(context.Request.Path),
                appBuilder => appBuilder.UseHttpsRedirection());
        }

        // Use security headers middleware
        app.UseMiddleware<SecurityHeadersMiddleware>();

        // Use CORS
        app.UseCors("DefaultCorsPolicy");

        return app;
    }

    private static bool IsHealthCheckPath(PathString path)
    {
        var healthPaths = new[] { "/health", "/api/health" };
        return healthPaths.Any(healthPath =>
            path.StartsWithSegments(healthPath, StringComparison.OrdinalIgnoreCase));
    }
}
