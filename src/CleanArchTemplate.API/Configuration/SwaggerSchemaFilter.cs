using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace CleanArchTemplate.API.Configuration;

/// <summary>
/// Schema filter to customize Swagger schemas
/// </summary>
public class SwaggerSchemaFilter : ISchemaFilter
{
    /// <summary>
    /// Applies the filter to the specified schema
    /// </summary>
    /// <param name="schema">The schema</param>
    /// <param name="context">The context</param>
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == null) return;

        // Add examples and descriptions based on type
        ApplyCustomizations(schema, context.Type);

        // Handle enums
        if (context.Type.IsEnum)
        {
            schema.Enum.Clear();
            foreach (var enumValue in Enum.GetValues(context.Type))
            {
                schema.Enum.Add(new Microsoft.OpenApi.Any.OpenApiString(enumValue.ToString()));
            }
        }

        // Handle nullable types
        if (Nullable.GetUnderlyingType(context.Type) != null)
        {
            schema.Nullable = true;
        }

        // Add validation attributes information
        ApplyValidationAttributes(schema, context.Type);
    }

    private static void ApplyCustomizations(OpenApiSchema schema, Type type)
    {
        switch (type.Name)
        {
            case "PaginationRequest":
                schema.Description = "Pagination parameters for list endpoints";
                if (schema.Properties != null)
                {
                    if (schema.Properties.ContainsKey("pageNumber"))
                    {
                        schema.Properties["pageNumber"].Description = "Page number (1-based)";
                        schema.Properties["pageNumber"].Minimum = 1;
                        schema.Properties["pageNumber"].Example = new Microsoft.OpenApi.Any.OpenApiInteger(1);
                    }
                    if (schema.Properties.ContainsKey("pageSize"))
                    {
                        schema.Properties["pageSize"].Description = "Number of items per page";
                        schema.Properties["pageSize"].Minimum = 1;
                        schema.Properties["pageSize"].Maximum = 100;
                        schema.Properties["pageSize"].Example = new Microsoft.OpenApi.Any.OpenApiInteger(20);
                    }
                }
                break;

            case "UserDto":
                schema.Description = "User information with roles and audit data";
                break;

            case "UserSummaryDto":
                schema.Description = "Simplified user information for list views";
                break;

            case "RoleDto":
                schema.Description = "Role information with associated permissions";
                break;

            case "PermissionDto":
                schema.Description = "Permission information for role-based access control";
                break;

            case "LoginCommand":
                schema.Description = "User login credentials";
                break;

            case "RegisterCommand":
                schema.Description = "User registration information";
                break;

            case "CreateUserCommand":
                schema.Description = "User creation data with resilience patterns";
                break;

            case "UpdateUserCommand":
                schema.Description = "User update information";
                break;

            case "RefreshTokenCommand":
                schema.Description = "Refresh token for obtaining new access tokens";
                break;
        }
    }

    private static void ApplyValidationAttributes(OpenApiSchema schema, Type type)
    {
        if (schema.Properties == null) return;

        foreach (var property in type.GetProperties())
        {
            var propertyName = char.ToLowerInvariant(property.Name[0]) + property.Name[1..];
            if (!schema.Properties.ContainsKey(propertyName)) continue;

            var propertySchema = schema.Properties[propertyName];

            // Required attribute
            var requiredAttribute = property.GetCustomAttribute<RequiredAttribute>();
            if (requiredAttribute != null)
            {
                if (schema.Required == null)
                    schema.Required = new HashSet<string>();
                schema.Required.Add(propertyName);
                
                if (!string.IsNullOrEmpty(requiredAttribute.ErrorMessage))
                {
                    propertySchema.Description += $" (Required: {requiredAttribute.ErrorMessage})";
                }
            }

            // StringLength attribute
            var stringLengthAttribute = property.GetCustomAttribute<StringLengthAttribute>();
            if (stringLengthAttribute != null)
            {
                propertySchema.MaxLength = stringLengthAttribute.MaximumLength;
                if (stringLengthAttribute.MinimumLength > 0)
                {
                    propertySchema.MinLength = stringLengthAttribute.MinimumLength;
                }
            }

            // Range attribute
            var rangeAttribute = property.GetCustomAttribute<RangeAttribute>();
            if (rangeAttribute != null)
            {
                if (rangeAttribute.Minimum is int minInt)
                    propertySchema.Minimum = minInt;
                if (rangeAttribute.Maximum is int maxInt)
                    propertySchema.Maximum = maxInt;
            }

            // EmailAddress attribute
            var emailAttribute = property.GetCustomAttribute<EmailAddressAttribute>();
            if (emailAttribute != null)
            {
                propertySchema.Format = "email";
                propertySchema.Pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            }

            // Phone attribute
            var phoneAttribute = property.GetCustomAttribute<PhoneAttribute>();
            if (phoneAttribute != null)
            {
                propertySchema.Format = "phone";
            }

            // Url attribute
            var urlAttribute = property.GetCustomAttribute<UrlAttribute>();
            if (urlAttribute != null)
            {
                propertySchema.Format = "uri";
            }
        }
    }
}