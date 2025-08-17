using CleanArchTemplate.API.Filters;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchTemplate.API.Extensions;

/// <summary>
/// Extension methods for configuring validation services
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// Adds validation services and configures model validation
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddValidationServices(this IServiceCollection services)
    {
        // Register the model validation filter
        services.AddScoped<ModelValidationFilter>();

        // Configure API behavior options to suppress default model validation
        services.Configure<ApiBehaviorOptions>(options =>
        {
            // Suppress the default model validation filter since we're using our custom one
            options.SuppressModelStateInvalidFilter = true;
            
            // Configure custom client error mapping
            options.ClientErrorMapping[400] = new ClientErrorData
            {
                Link = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Bad Request"
            };
            
            options.ClientErrorMapping[401] = new ClientErrorData
            {
                Link = "https://tools.ietf.org/html/rfc7235#section-3.1",
                Title = "Unauthorized"
            };
            
            options.ClientErrorMapping[403] = new ClientErrorData
            {
                Link = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                Title = "Forbidden"
            };
            
            options.ClientErrorMapping[404] = new ClientErrorData
            {
                Link = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                Title = "Not Found"
            };
            
            options.ClientErrorMapping[409] = new ClientErrorData
            {
                Link = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                Title = "Conflict"
            };
            
            options.ClientErrorMapping[422] = new ClientErrorData
            {
                Link = "https://tools.ietf.org/html/rfc4918#section-11.2",
                Title = "Unprocessable Entity"
            };
            
            options.ClientErrorMapping[500] = new ClientErrorData
            {
                Link = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Title = "Internal Server Error"
            };
        });

        return services;
    }

    /// <summary>
    /// Adds global model validation to all controllers
    /// </summary>
    /// <param name="options">The MVC options</param>
    /// <returns>The MVC options</returns>
    public static MvcOptions AddGlobalModelValidation(this MvcOptions options)
    {
        options.Filters.Add<ModelValidationFilter>();
        return options;
    }
}