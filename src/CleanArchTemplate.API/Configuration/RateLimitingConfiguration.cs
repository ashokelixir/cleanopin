namespace CleanArchTemplate.API.Configuration;

public static class RateLimitingConfiguration
{
    public static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        // Rate limiting is implemented via middleware
        // Configuration is handled in appsettings.json
        return services;
    }
}