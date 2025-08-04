using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json;

namespace CleanArchTemplate.API.Configuration;

/// <summary>
/// Swagger operation filter to set default values and improve documentation
/// </summary>
public class SwaggerDefaultValues : IOperationFilter
{
    /// <summary>
    /// Applies the filter to the specified operation using the given context
    /// </summary>
    /// <param name="operation">The operation</param>
    /// <param name="context">The context</param>
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var apiDescription = context.ApiDescription;

        // Set deprecated flag (check if method exists)
        var isDeprecated = apiDescription.ActionDescriptor.EndpointMetadata?.Any(m => m is ObsoleteAttribute) ?? false;
        operation.Deprecated |= isDeprecated;

        // Add response types if not already present
        if (operation.Responses.Any())
        {
            return;
        }

        // Add common response types
        foreach (var responseType in context.ApiDescription.SupportedResponseTypes)
        {
            var responseKey = responseType.IsDefaultResponse ? "default" : responseType.StatusCode.ToString();
            var response = operation.Responses[responseKey];

            foreach (var contentType in response.Content.Keys)
            {
                if (responseType.ModelMetadata?.ModelType != null)
                {
                    var schema = context.SchemaGenerator.GenerateSchema(responseType.ModelMetadata.ModelType, context.SchemaRepository);
                    response.Content[contentType].Schema = schema;
                }
            }
        }

        // Add examples for request bodies
        if (operation.RequestBody?.Content != null)
        {
            foreach (var content in operation.RequestBody.Content.Values)
            {
                if (content.Schema?.Reference?.Id != null)
                {
                    AddExampleForSchema(content, content.Schema.Reference.Id);
                }
            }
        }

        // Add examples for responses
        foreach (var response in operation.Responses.Values)
        {
            if (response.Content != null)
            {
                foreach (var content in response.Content.Values)
                {
                    if (content.Schema?.Reference?.Id != null)
                    {
                        AddExampleForSchema(content, content.Schema.Reference.Id);
                    }
                }
            }
        }
    }

    private static void AddExampleForSchema(OpenApiMediaType content, string schemaId)
    {
        // Add examples based on schema type
        switch (schemaId)
        {
            case "CreateUserCommand":
                content.Example = new Microsoft.OpenApi.Any.OpenApiObject
                {
                    ["email"] = new Microsoft.OpenApi.Any.OpenApiString("user@example.com"),
                    ["firstName"] = new Microsoft.OpenApi.Any.OpenApiString("John"),
                    ["lastName"] = new Microsoft.OpenApi.Any.OpenApiString("Doe"),
                    ["password"] = new Microsoft.OpenApi.Any.OpenApiString("SecurePassword123!")
                };
                break;

            case "LoginCommand":
                content.Example = new Microsoft.OpenApi.Any.OpenApiObject
                {
                    ["email"] = new Microsoft.OpenApi.Any.OpenApiString("user@example.com"),
                    ["password"] = new Microsoft.OpenApi.Any.OpenApiString("SecurePassword123!")
                };
                break;

            case "RegisterCommand":
                content.Example = new Microsoft.OpenApi.Any.OpenApiObject
                {
                    ["email"] = new Microsoft.OpenApi.Any.OpenApiString("newuser@example.com"),
                    ["firstName"] = new Microsoft.OpenApi.Any.OpenApiString("Jane"),
                    ["lastName"] = new Microsoft.OpenApi.Any.OpenApiString("Smith"),
                    ["password"] = new Microsoft.OpenApi.Any.OpenApiString("SecurePassword123!"),
                    ["confirmPassword"] = new Microsoft.OpenApi.Any.OpenApiString("SecurePassword123!")
                };
                break;

            case "RefreshTokenCommand":
                content.Example = new Microsoft.OpenApi.Any.OpenApiObject
                {
                    ["refreshToken"] = new Microsoft.OpenApi.Any.OpenApiString("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...")
                };
                break;

            case "UpdateUserCommand":
                content.Example = new Microsoft.OpenApi.Any.OpenApiObject
                {
                    ["userId"] = new Microsoft.OpenApi.Any.OpenApiString("123e4567-e89b-12d3-a456-426614174000"),
                    ["firstName"] = new Microsoft.OpenApi.Any.OpenApiString("John"),
                    ["lastName"] = new Microsoft.OpenApi.Any.OpenApiString("Doe"),
                    ["isActive"] = new Microsoft.OpenApi.Any.OpenApiBoolean(true)
                };
                break;
        }
    }
}