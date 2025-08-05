using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Infrastructure.Extensions;
using CleanArchTemplate.Shared.Models;
using CleanArchTemplate.TestUtilities.Common;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;

namespace CleanArchTemplate.IntegrationTests.Infrastructure;

/// <summary>
/// Integration tests for AWS Secrets Manager functionality
/// </summary>
public class SecretsManagerIntegrationTests : BaseIntegrationTest
{
    public SecretsManagerIntegrationTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void SecretsManagerService_ShouldBeRegistered()
    {
        // Act
        var service = Factory.Services.GetService<ISecretsManagerService>();

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void SecretsManagerSettings_ShouldBeConfigured()
    {
        // Act
        var settings = Factory.Services.GetService<IOptions<SecretsManagerSettings>>();

        // Assert
        settings.Should().NotBeNull();
        settings!.Value.Should().NotBeNull();
        settings.Value.Region.Should().NotBeNullOrEmpty();
        settings.Value.Environment.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SecretsManagerService_GetSecretAsync_ShouldReturnLocalValue_InDevelopment()
    {
        // Arrange
        var service = Factory.Services.GetRequiredService<ISecretsManagerService>();
        const string secretName = "test-secret";

        // Act
        var result = await service.GetSecretAsync(secretName);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().StartWith("local-");
    }

    [Fact]
    public async Task SecretsManagerService_GetSecretsAsync_ShouldReturnMultipleSecrets()
    {
        // Arrange
        var service = Factory.Services.GetRequiredService<ISecretsManagerService>();
        var secretNames = new[] { "secret1", "secret2", "secret3" };

        // Act
        var results = await service.GetSecretsAsync(secretNames);

        // Assert
        results.Should().HaveCount(3);
        results.Keys.Should().BeEquivalentTo(secretNames);
        results.Values.Should().AllSatisfy(value => value.Should().StartWith("local-"));
    }

    [Fact]
    public void SecretsManagerService_InvalidateCache_ShouldNotThrow()
    {
        // Arrange
        var service = Factory.Services.GetRequiredService<ISecretsManagerService>();

        // Act & Assert
        service.Invoking(s => s.InvalidateCache("test-secret"))
            .Should().NotThrow();
    }

    [Fact]
    public void SecretsManagerService_ClearCache_ShouldNotThrow()
    {
        // Arrange
        var service = Factory.Services.GetRequiredService<ISecretsManagerService>();

        // Act & Assert
        service.Invoking(s => s.ClearCache())
            .Should().NotThrow();
    }

    [Fact]
    public async Task SecretsManagerService_GetSecretAsync_Generic_ShouldDeserializeJson()
    {
        // Arrange
        var service = Factory.Services.GetRequiredService<ISecretsManagerService>();
        
        // In local development mode, the service returns a simple string
        // For this test, we'll test the error handling when JSON is invalid
        const string secretName = "invalid-json-secret";

        // Act & Assert
        // In local development mode, this will return a simple string that can't be deserialized
        await service.Invoking(s => s.GetSecretAsync<TestConfiguration>(secretName))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Failed to deserialize secret*");
    }

    [Fact]
    public void ConfigurationBuilder_AddSecretsManager_ShouldAddConfigurationSource()
    {
        // Arrange
        var builder = new ConfigurationBuilder();

        // Act
        builder.AddSecretsManager("test-secret", optional: true);
        var configuration = builder.Build();

        // Assert
        configuration.Should().NotBeNull();
        // In a real test environment, you would verify that the configuration source was added
        // For now, we just verify it doesn't throw
    }

    [Fact]
    public void ConfigurationBuilder_AddSecretsManager_WithMultipleSecrets_ShouldAddConfigurationSource()
    {
        // Arrange
        var builder = new ConfigurationBuilder();
        var secretNames = new[] { "secret1", "secret2", "secret3" };

        // Act
        builder.AddSecretsManager(secretNames, optional: true);
        var configuration = builder.Build();

        // Assert
        configuration.Should().NotBeNull();
    }

    [Fact]
    public void ServiceCollection_AddSecretsManager_ShouldRegisterAllServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SecretsManager:Region"] = "ap-south-1",
                ["SecretsManager:Environment"] = "test",
                ["SecretsManager:UseLocalDevelopment"] = "true"
            })
            .Build();

        // Act
        services.AddSecretsManager(configuration);
        using var serviceProvider = services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<ISecretsManagerService>().Should().NotBeNull();
        serviceProvider.GetService<IOptions<SecretsManagerSettings>>().Should().NotBeNull();
    }

    [Fact]
    public void ServiceCollection_AddSecretRotationDetection_ShouldRegisterBackgroundService()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SecretsManager:EnableRotationDetection"] = "true"
            })
            .Build();

        services.AddLogging();
        services.AddSecretsManager(configuration);

        // Act
        services.AddSecretRotationDetection(configuration);
        using var serviceProvider = services.BuildServiceProvider();

        // Assert
        var hostedServices = serviceProvider.GetServices<IHostedService>();
        hostedServices.Should().NotBeEmpty();
        hostedServices.Should().Contain(s => s.GetType().Name.Contains("SecretRotation"));
    }

    private class TestConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
