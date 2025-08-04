using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Infrastructure.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace CleanArchTemplate.Infrastructure.Services;

/// <summary>
/// Service for handling automatic database credentials rotation
/// </summary>
public class DatabaseCredentialsRotationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseCredentialsRotationService> _logger;
    private readonly IConfiguration _configuration;
    private string? _currentConnectionString;

    public DatabaseCredentialsRotationService(
        IServiceProvider serviceProvider,
        ILogger<DatabaseCredentialsRotationService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
        _currentConnectionString = _configuration.GetConnectionString("DefaultConnection");
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
            // Get the latest database credentials from Secrets Manager
            var databaseCredentials = await secretsService.GetSecretAsync<DatabaseCredentials>(
                "database-credentials", cancellationToken);

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
        return $"Host={credentials.Host};Port={credentials.Port};Database={credentials.Database};Username={credentials.Username};Password={credentials.Password}";
    }
}

/// <summary>
/// Model for database credentials stored in AWS Secrets Manager
/// </summary>
public class DatabaseCredentials
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 5432;
    public string Database { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Engine { get; set; } = "postgres";
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