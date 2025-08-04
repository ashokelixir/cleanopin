using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CleanArchTemplate.API.Configuration;

/// <summary>
/// Document filter to customize the Swagger document
/// </summary>
public class SwaggerDocumentFilter : IDocumentFilter
{
    /// <summary>
    /// Applies the filter to the specified document
    /// </summary>
    /// <param name="swaggerDoc">The swagger document</param>
    /// <param name="context">The context</param>
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        // Add custom tags with descriptions
        swaggerDoc.Tags = new List<OpenApiTag>
        {
            new OpenApiTag
            {
                Name = "Authentication",
                Description = "Authentication and authorization endpoints including login, registration, and token management"
            },
            new OpenApiTag
            {
                Name = "Users",
                Description = "User management operations with resilience patterns for creating, updating, and retrieving user information"
            },
            new OpenApiTag
            {
                Name = "Health",
                Description = "Health check endpoints for monitoring system status, database connectivity, and resilience patterns"
            },
            new OpenApiTag
            {
                Name = "Roles",
                Description = "Role-based access control management for user permissions and authorization"
            }
        };

        // Add servers information
        swaggerDoc.Servers = new List<OpenApiServer>
        {
            new OpenApiServer
            {
                Url = "https://localhost:7001",
                Description = "Development server (HTTPS)"
            },
            new OpenApiServer
            {
                Url = "http://localhost:5000",
                Description = "Development server (HTTP)"
            }
        };

        // External documentation can be added here if needed

        // Remove any unwanted paths or operations
        var pathsToRemove = new List<string>();
        foreach (var path in swaggerDoc.Paths)
        {
            // Remove WeatherForecast endpoints if they exist
            if (path.Key.Contains("WeatherForecast", StringComparison.OrdinalIgnoreCase))
            {
                pathsToRemove.Add(path.Key);
            }
        }

        foreach (var path in pathsToRemove)
        {
            swaggerDoc.Paths.Remove(path);
        }

        // Add common response schemas
        if (swaggerDoc.Components?.Schemas != null)
        {
            // Add error response schema
            swaggerDoc.Components.Schemas.Add("ErrorResponse", new OpenApiSchema
            {
                Type = "object",
                Properties = new Dictionary<string, OpenApiSchema>
                {
                    ["error"] = new OpenApiSchema
                    {
                        Type = "string",
                        Description = "Error message describing what went wrong"
                    },
                    ["details"] = new OpenApiSchema
                    {
                        Type = "array",
                        Items = new OpenApiSchema { Type = "string" },
                        Description = "Additional error details or validation errors"
                    },
                    ["traceId"] = new OpenApiSchema
                    {
                        Type = "string",
                        Description = "Unique identifier for tracing the request"
                    }
                },
                Required = new HashSet<string> { "error" }
            });

            // Add validation error response schema
            swaggerDoc.Components.Schemas.Add("ValidationErrorResponse", new OpenApiSchema
            {
                Type = "object",
                Properties = new Dictionary<string, OpenApiSchema>
                {
                    ["error"] = new OpenApiSchema
                    {
                        Type = "string",
                        Description = "Validation error message"
                    },
                    ["details"] = new OpenApiSchema
                    {
                        Type = "object",
                        AdditionalProperties = new OpenApiSchema
                        {
                            Type = "array",
                            Items = new OpenApiSchema { Type = "string" }
                        },
                        Description = "Field-specific validation errors"
                    }
                },
                Required = new HashSet<string> { "error" }
            });
        }
    }
}