using Asp.Versioning;
using Asp.Versioning.ApiExplorer;

namespace CleanArchTemplate.API.Configuration;

public static class ApiVersioningConfiguration
{
    public static IServiceCollection AddCustomApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            // Default version
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;

            // Version reading strategies
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new QueryStringApiVersionReader("version"),
                new HeaderApiVersionReader("X-Version"),
                new MediaTypeApiVersionReader("ver")
            );

            // This will be configured through attributes on controllers

            // Version reporting
            options.ReportApiVersions = true;
        })
        .AddApiExplorer(setup =>
        {
            setup.GroupNameFormat = "'v'VVV";
            setup.SubstituteApiVersionInUrl = true;
        });

        return services;
    }

    public static IApplicationBuilder UseApiVersioning(this IApplicationBuilder app)
    {
        // API versioning is automatically used when configured in services
        return app;
    }
}