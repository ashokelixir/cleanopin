using CleanArchTemplate.Infrastructure.Extensions;
using CleanArchTemplate.Infrastructure.Services;

namespace CleanArchTemplate.API.Configuration;

/// <summary>
/// Configuration extensions for AWS Secrets Manager
/// </summary>
public static class SecretsManagerConfiguration
{
    /// <summary>
    /// Configures AWS Secrets Manager services
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <param name="environment">The hosting environment</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddSecretsManagerConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // Add AWS Secrets Manager services
        services.AddSecretsManager(configuration);

        // Add database credentials rotation in production
        if (environment.IsProduction())
        {
            services.AddDatabaseCredentialsRotation();
        }

        return services;
    }

    /// <summary>
    /// Adds AWS Secrets Manager to the configuration builder
    /// </summary>
    /// <param name="builder">The configuration builder</param>
    /// <param name="environment">The hosting environment</param>
    /// <returns>The configuration builder</returns>
    public static IConfigurationBuilder AddSecretsManagerConfiguration(
        this IConfigurationBuilder builder,
        IWebHostEnvironment environment)
    {
        // Only use Secrets Manager in non-development environments
        if (!environment.IsDevelopment())
        {
            // Add common secrets that all environments need
            builder.AddSecretsManager(
                new[] { 
                    "database-credentials", 
                    "jwt-settings", 
                    "external-api-keys",
                    "redis-connection",
                    "datadog-api-key"
                },
                region: Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-1",
                environment: environment.EnvironmentName.ToLowerInvariant(),
                optional: true);

            // Add environment-specific secrets
            var environmentSpecificSecrets = environment.EnvironmentName.ToLowerInvariant() switch
            {
                "staging" => new[] { "staging-specific-config" },
                "production" => new[] { "production-specific-config", "ssl-certificates" },
                _ => Array.Empty<string>()
            };

            if (environmentSpecificSecrets.Any())
            {
                builder.AddSecretsManager(
                    environmentSpecificSecrets,
                    region: Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-1",
                    environment: environment.EnvironmentName.ToLowerInvariant(),
                    optional: true);
            }
        }

        return builder;
    }
}

/// <summary>
/// Example of how to structure secrets in AWS Secrets Manager
/// </summary>
public static class SecretsManagerExamples
{
    /// <summary>
    /// Example database credentials secret structure
    /// Secret name: {environment}/database-credentials
    /// </summary>
    public const string DatabaseCredentialsExample = @"
    {
        ""host"": ""your-rds-endpoint.region.rds.amazonaws.com"",
        ""port"": 5432,
        ""database"": ""cleanarch"",
        ""username"": ""app_user"",
        ""password"": ""secure-generated-password"",
        ""engine"": ""postgres""
    }";

    /// <summary>
    /// Example JWT settings secret structure
    /// Secret name: {environment}/jwt-settings
    /// </summary>
    public const string JwtSettingsExample = @"
    {
        ""Jwt:SecretKey"": ""your-super-secret-jwt-signing-key-that-is-at-least-32-characters-long"",
        ""Jwt:Issuer"": ""CleanArchTemplate"",
        ""Jwt:Audience"": ""CleanArchTemplate"",
        ""Jwt:AccessTokenExpirationMinutes"": ""60""
    }";

    /// <summary>
    /// Example external API keys secret structure
    /// Secret name: {environment}/external-api-keys
    /// </summary>
    public const string ExternalApiKeysExample = @"
    {
        ""Datadog:ApiKey"": ""your-datadog-api-key"",
        ""Datadog:ApplicationKey"": ""your-datadog-app-key"",
        ""SendGrid:ApiKey"": ""your-sendgrid-api-key"",
        ""Stripe:SecretKey"": ""your-stripe-secret-key""
    }";

    /// <summary>
    /// Example Redis connection secret structure
    /// Secret name: {environment}/redis-connection
    /// </summary>
    public const string RedisConnectionExample = @"
    {
        ""Redis:ConnectionString"": ""your-redis-cluster-endpoint:6379"",
        ""Redis:Password"": ""your-redis-auth-token"",
        ""Redis:Database"": ""0""
    }";
}