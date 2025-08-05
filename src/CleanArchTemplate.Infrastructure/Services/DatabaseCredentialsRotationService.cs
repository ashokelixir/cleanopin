using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Infrastructure.Data.Contexts;
using CleanArchTemplate.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CleanArchTemplate.Infrastructure.Services;

/// <summary>
/// Service for handling automatic database credentials rotation
/// </summary>
public class DatabaseCredentialsRotationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseCredentialsRotationService> _logger;
    private readonly IConfiguration _configuration;
    private readonly SecretsManagerSettings _secretsSettings;
    private readonly string _databaseSecretName;
    private string? _currentConnectionString;

    public DatabaseCredentialsRotationService(
        IServiceProvider serviceProvider,
        ILogger<DatabaseCredentialsRotationService> logger,
        IConfiguration configuration,
        IOptions<SecretsManagerSettings> secretsSettings)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
        _secretsSettings = secretsSettings.Value;
        _currentConnectionString = _configuration.GetConnectionString("DefaultConnection");
        
        // Build the secret name based on settings
        _databaseSecretName = $"{_secretsSettings.ProjectName}-{_secretsSettings.Environment}/database";
        
        _logger.LogInformation("Database credentials rotation service initialized with secret name: {SecretName}", _databaseSecretName);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting database credentials rotation service");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndRotateCredentialsAsync(stoppingToken);
                
                // Check every 10 minutes
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during database credentials rotation check");
                
                // Wait before retrying
                await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
            }
        }

        _logger.LogInformation("Database credentials rotation service stopped");
    }

    private async Task CheckAndRotateCredentialsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var secretsService = scope.ServiceProvider.GetService<ISecretsManagerService>();
        
        if (secretsService == null)
        {
            _logger.LogDebug("Secrets Manager service not available for credentials rotation");
            return;
        }

        try
        {          
            _logger.LogInformation("Checking for rotated database credentials from secret: {SecretName}", _databaseSecretName);

            var databaseCredString = await secretsService.GetRawSecretAsync(
                _databaseSecretName, cancellationToken).ConfigureAwait(false);

            _logger.LogDebug("Retrieved database credentials secret string from: {SecretName}", _databaseSecretName);            

            // Get the latest database credentials from Secrets Manager
            var databaseCredentials = await secretsService.GetSecretAsync<DatabaseCredentials>(
                _databaseSecretName, cancellationToken);

            if (databaseCredentials == null)
            {
                _logger.LogDebug("Database credentials secret not found or empty");
                return;
            }

            var newConnectionString = BuildConnectionString(databaseCredentials);

            // Check if credentials have changed
            if (newConnectionString != _currentConnectionString)
            {
                _logger.LogInformation("Database credentials have been rotated, updating connection");
                
                // Test the new connection
                if (await TestConnectionAsync(newConnectionString, cancellationToken))
                {
                    _currentConnectionString = newConnectionString;
                    
                    // Update the configuration
                    _configuration["ConnectionStrings:DefaultConnection"] = newConnectionString;
                    
                    _logger.LogInformation("Successfully updated database connection with rotated credentials");
                    
                    // Optionally, you could trigger a connection pool refresh here
                    // This would require additional implementation in the DbContext configuration
                }
                else
                {
                    _logger.LogError("Failed to connect with rotated database credentials");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check for rotated database credentials");
        }
    }

    private async Task<bool> TestConnectionAsync(string connectionString, CancellationToken cancellationToken)
    {
        try
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            using var context = new ApplicationDbContext(optionsBuilder.Options);
            await context.Database.CanConnectAsync(cancellationToken);
            
            _logger.LogDebug("Successfully tested database connection with new credentials");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to test database connection with new credentials");
            return false;
        }
    }

    private string BuildConnectionString(DatabaseCredentials credentials)
    {
        // If we already have a connection string in the secret, use it
        if (!string.IsNullOrEmpty(credentials.ConnectionString))
        {
            return credentials.ConnectionString;
        }
        
        // Otherwise, build it from individual components
        return $"Host={credentials.CleanHost};Port={credentials.Port};Database={credentials.Database};Username={credentials.Username};Password={credentials.Password};SSL Mode=Require;";
    }
}

/// <summary>
/// Model for database credentials stored in AWS Secrets Manager
/// </summary>
public class DatabaseCredentials
{
    [JsonPropertyName("host")]
    public string Host { get; set; } = string.Empty;
    
    [JsonPropertyName("port")]
    public int Port { get; set; } = 5432;
    
    [JsonPropertyName("dbname")]
    public string Database { get; set; } = string.Empty;
    
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
    
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
    
    [JsonPropertyName("engine")]
    public string Engine { get; set; } = "postgres";
    
    [JsonPropertyName("connectionString")]
    public string ConnectionString { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets the clean host without port (if port is included in host field)
    /// </summary>
    public string CleanHost
    {
        get
        {
            if (string.IsNullOrEmpty(Host))
                return string.Empty;
                
            // If host contains port, extract just the hostname
            var colonIndex = Host.LastIndexOf(':');
            if (colonIndex > 0 && int.TryParse(Host.Substring(colonIndex + 1), out _))
            {
                return Host.Substring(0, colonIndex);
            }
            
            return Host;
        }
    }
}

/// <summary>
/// Extension methods for database credentials rotation
/// </summary>
public static class DatabaseCredentialsRotationExtensions
{
    /// <summary>
    /// Adds database credentials rotation service
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddDatabaseCredentialsRotation(this IServiceCollection services)
    {
        services.AddHostedService<DatabaseCredentialsRotationService>();
        return services;
    }
}
