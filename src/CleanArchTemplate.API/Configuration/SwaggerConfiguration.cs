using Microsoft.OpenApi.Models;
using System.Reflection;

namespace CleanArchTemplate.API.Configuration;

/// <summary>
/// Swagger configuration for API documentation
/// </summary>
public static class SwaggerConfiguration
{
    /// <summary>
    /// Adds Swagger services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            // API Information
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "Clean Architecture Template API",
                Description = "A comprehensive .NET 8 Clean Architecture template with enterprise-grade features including authentication, resilience patterns, and observability.",
                Contact = new OpenApiContact
                {
                    Name = "Clean Architecture Template",
                    Email = "support@cleanarchtemplate.com",
                    Url = new Uri("https://github.com/cleanarchtemplate/api")
                },
                License = new OpenApiLicense
                {
                    Name = "MIT License",
                    Url = new Uri("https://opensource.org/licenses/MIT")
                },
                TermsOfService = new Uri("https://cleanarchtemplate.com/terms")
            });

            // JWT Authentication
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter 'Bearer' followed by a space and your JWT token. Example: 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...'"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // XML Comments
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }

            // Include XML comments from other projects
            var applicationXmlFile = "CleanArchTemplate.Application.xml";
            var applicationXmlPath = Path.Combine(AppContext.BaseDirectory, applicationXmlFile);
            if (File.Exists(applicationXmlPath))
            {
                options.IncludeXmlComments(applicationXmlPath);
            }

            var sharedXmlFile = "CleanArchTemplate.Shared.xml";
            var sharedXmlPath = Path.Combine(AppContext.BaseDirectory, sharedXmlFile);
            if (File.Exists(sharedXmlPath))
            {
                options.IncludeXmlComments(sharedXmlPath);
            }

            // Custom schema mappings
            options.MapType<DateOnly>(() => new OpenApiSchema
            {
                Type = "string",
                Format = "date",
                Example = new Microsoft.OpenApi.Any.OpenApiString("2024-01-15")
            });

            options.MapType<TimeOnly>(() => new OpenApiSchema
            {
                Type = "string",
                Format = "time",
                Example = new Microsoft.OpenApi.Any.OpenApiString("14:30:00")
            });

            // Custom operation filters
            options.OperationFilter<SwaggerDefaultValues>();
            options.DocumentFilter<SwaggerDocumentFilter>();

            // Group endpoints by tags
            options.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] });
            options.DocInclusionPredicate((name, api) => true);

            // Custom schema filters
            options.SchemaFilter<SwaggerSchemaFilter>();

            // Enable annotations
            options.EnableAnnotations();

            // Order actions by method
            options.OrderActionsBy(apiDesc => $"{apiDesc.ActionDescriptor.RouteValues["controller"]}_{apiDesc.HttpMethod}");
        });

        return services;
    }

    /// <summary>
    /// Configures Swagger middleware in the application pipeline
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <param name="environment">The web host environment</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The application builder</returns>
    public static IApplicationBuilder UseSwaggerDocumentation(this IApplicationBuilder app, IWebHostEnvironment environment, IConfiguration configuration)
    {
        var swaggerSettings = configuration.GetSection("Swagger");
        var enableInProduction = swaggerSettings.GetValue("EnableInProduction", false);
        var requireAuthentication = swaggerSettings.GetValue("RequireAuthentication", true);

        // Enable Swagger in development, staging, or if explicitly enabled in production
        if (environment.IsDevelopment() || environment.IsStaging() || enableInProduction)
        {
            // Add authentication middleware for Swagger in production
            if (environment.IsProduction() && requireAuthentication)
            {
                app.UseSwaggerAuthentication();
            }

            app.UseSwagger(options =>
            {
                options.RouteTemplate = "api-docs/{documentName}/swagger.json";
                
                // Security: Don't expose server information
                options.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
                {
                    swaggerDoc.Servers.Clear();
                    swaggerDoc.Servers.Add(new OpenApiServer { Url = $"{httpReq.Scheme}://{httpReq.Host.Value}" });
                });
            });

            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/api-docs/v1/swagger.json", "Clean Architecture Template API v1");
                options.RoutePrefix = "api-docs";
                options.DocumentTitle = "Clean Architecture Template API Documentation";
                
                // UI Customization
                options.DefaultModelsExpandDepth(-1); // Hide schemas section by default
                options.DefaultModelExpandDepth(2);
                options.DisplayRequestDuration();
                options.EnableDeepLinking();
                options.EnableFilter();
                options.ShowExtensions();
                options.EnableValidator();
                
                // Custom CSS
                options.InjectStylesheet("/swagger-ui/custom.css");
                
                // Security: Disable try-it-out in production
                if (environment.IsProduction())
                {
                    options.SupportedSubmitMethods(); // Empty array disables all submit methods
                }
                
                // OAuth2 configuration (if needed in the future)
                options.OAuthClientId("swagger-ui");
                options.OAuthAppName("Clean Architecture Template API");
                options.OAuthUsePkce();
            });
        }

        return app;
    }

    /// <summary>
    /// Adds basic authentication for Swagger in production environments
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <returns>The application builder</returns>
    private static IApplicationBuilder UseSwaggerAuthentication(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/api-docs"))
            {
                string? authHeader = context.Request.Headers.Authorization;
                if (authHeader != null && authHeader.StartsWith("Basic "))
                {
                    // Extract credentials
                    var parts = authHeader.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2) 
                    {
                        // Return authentication challenge
                        context.Response.Headers.WWWAuthenticate = "Basic";
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsync("Swagger documentation requires authentication.");
                        return;
                    }
                    var encodedUsernamePassword = parts[1]?.Trim();
                    if (!string.IsNullOrEmpty(encodedUsernamePassword))
                    {
                        var decodedUsernamePassword = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encodedUsernamePassword));
                        var username = decodedUsernamePassword.Split(':', 2)[0];
                        var password = decodedUsernamePassword.Split(':', 2)[1];

                        // Check credentials (in production, use proper authentication)
                        if (IsValidSwaggerCredentials(username, password))
                        {
                            await next();
                            return;
                        }
                    }
                }

                // Return authentication challenge
                context.Response.Headers.WWWAuthenticate = "Basic";
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Swagger documentation requires authentication.");
                return;
            }

            await next();
        });
    }

    private static bool IsValidSwaggerCredentials(string username, string password)
    {
        // In production, implement proper credential validation
        // This is a simplified example - use proper authentication in real scenarios
        return username == "swagger" && password == "swagger123!";
    }
}