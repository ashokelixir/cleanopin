using CleanArchTemplate.Infrastructure.Services;
using CleanArchTemplate.Shared.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace CleanArchTemplate.Infrastructure.Services.Tests;

/// <summary>
/// Unit tests for DatabaseCredentialsRotationService
/// </summary>
public class DatabaseCredentialsRotationServiceTests
{
    [Theory]
    [InlineData("cleanarch-template", "dev", "cleanarch-template-dev/database")]
    [InlineData("my-app", "prod", "my-app-prod/database")]
    [InlineData("test-project", "staging", "test-project-staging/database")]
    public void Constructor_ShouldBuildCorrectSecretName(string projectName, string environment, string expectedSecretName)
    {
        // Arrange
        var serviceProvider = new Mock<IServiceProvider>();
        var logger = new Mock<ILogger<DatabaseCredentialsRotationService>>();
        var configuration = new Mock<IConfiguration>();
        
        var secretsSettings = new SecretsManagerSettings
        {
            ProjectName = projectName,
            Environment = environment
        };
        
        var options = new Mock<IOptions<SecretsManagerSettings>>();
        options.Setup(x => x.Value).Returns(secretsSettings);
        
        configuration.Setup(x => x.GetConnectionString("DefaultConnection"))
                    .Returns("Host=localhost;Database=test;");

        // Act
        var service = new DatabaseCredentialsRotationService(
            serviceProvider.Object,
            logger.Object,
            configuration.Object,
            options.Object);

        // Assert
        logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedSecretName)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_ShouldUseDefaultValues_WhenSettingsAreEmpty()
    {
        // Arrange
        var serviceProvider = new Mock<IServiceProvider>();
        var logger = new Mock<ILogger<DatabaseCredentialsRotationService>>();
        var configuration = new Mock<IConfiguration>();
        
        var secretsSettings = new SecretsManagerSettings(); // Uses default values
        
        var options = new Mock<IOptions<SecretsManagerSettings>>();
        options.Setup(x => x.Value).Returns(secretsSettings);
        
        configuration.Setup(x => x.GetConnectionString("DefaultConnection"))
                    .Returns("Host=localhost;Database=test;");

        // Act
        var service = new DatabaseCredentialsRotationService(
            serviceProvider.Object,
            logger.Object,
            configuration.Object,
            options.Object);

        // Assert - Should use default values: cleanarch-template-development/database
        logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("cleanarch-template-development/database")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}

/// <summary>
/// Unit tests for DatabaseCredentials class
/// </summary>
public class DatabaseCredentialsTests
{
    [Fact]
    public void DatabaseCredentials_ShouldDeserialize_FromActualSecretFormat()
    {
        // Arrange - This is the actual secret format from AWS
        var secretJson = """
        {
            "connectionString": "Host=cleanarch-template-dev-postgres.cxmywqowkjjp.ap-south-1.rds.amazonaws.com:5432;Port=5432;Database=cleanarch_template_dev;Username=app_user;Password=E%8R#}g]]r_bK$z$]cnx!G{r:(*0cY}i;SSL Mode=Require;",
            "dbname": "cleanarch_template_dev",
            "engine": "postgres",
            "host": "cleanarch-template-dev-postgres.cxmywqowkjjp.ap-south-1.rds.amazonaws.com:5432",
            "password": "E%8R#}g]]r_bK$z$]cnx!G{r:(*0cY}i",
            "port": 5432,
            "username": "app_user"
        }
        """;

        // Act
        var credentials = System.Text.Json.JsonSerializer.Deserialize<DatabaseCredentials>(secretJson);

        // Assert
        Assert.NotNull(credentials);
        Assert.Equal("cleanarch_template_dev", credentials.Database);
        Assert.Equal("postgres", credentials.Engine);
        Assert.Equal("cleanarch-template-dev-postgres.cxmywqowkjjp.ap-south-1.rds.amazonaws.com:5432", credentials.Host);
        Assert.Equal("E%8R#}g]]r_bK$z$]cnx!G{r:(*0cY}i", credentials.Password);
        Assert.Equal(5432, credentials.Port);
        Assert.Equal("app_user", credentials.Username);
        Assert.Contains("Host=cleanarch-template-dev-postgres.cxmywqowkjjp.ap-south-1.rds.amazonaws.com:5432", credentials.ConnectionString);
    }

    [Fact]
    public void CleanHost_ShouldExtractHostname_WhenHostContainsPort()
    {
        // Arrange
        var credentials = new DatabaseCredentials
        {
            Host = "cleanarch-template-dev-postgres.cxmywqowkjjp.ap-south-1.rds.amazonaws.com:5432"
        };

        // Act
        var cleanHost = credentials.CleanHost;

        // Assert
        Assert.Equal("cleanarch-template-dev-postgres.cxmywqowkjjp.ap-south-1.rds.amazonaws.com", cleanHost);
    }

    [Fact]
    public void CleanHost_ShouldReturnHost_WhenHostDoesNotContainPort()
    {
        // Arrange
        var credentials = new DatabaseCredentials
        {
            Host = "localhost"
        };

        // Act
        var cleanHost = credentials.CleanHost;

        // Assert
        Assert.Equal("localhost", cleanHost);
    }
}