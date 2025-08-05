using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using CleanArchTemplate.Infrastructure.Services;
using CleanArchTemplate.Shared.Models;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;
using Xunit;

namespace CleanArchTemplate.UnitTests.Infrastructure.Services;

/// <summary>
/// Unit tests for SecretsManagerService
/// </summary>
public class SecretsManagerServiceTests : IDisposable
{
    private readonly Mock<IAmazonSecretsManager> _mockSecretsManagerClient;
    private readonly IMemoryCache _memoryCache;
    private readonly Mock<ILogger<SecretsManagerService>> _mockLogger;
    private readonly SecretsManagerSettings _settings;
    private readonly SecretsManagerService _service;

    public SecretsManagerServiceTests()
    {
        _mockSecretsManagerClient = new Mock<IAmazonSecretsManager>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _mockLogger = new Mock<ILogger<SecretsManagerService>>();
        _settings = new SecretsManagerSettings
        {
            Region = "ap-south-1",
            Environment = "test",
            CacheDurationMinutes = 15,
            MaxRetryAttempts = 3,
            BaseDelayMs = 100, // Shorter delay for tests
            UseLocalDevelopment = false
        };

        var options = Options.Create(_settings);
        _service = new SecretsManagerService(
            _mockSecretsManagerClient.Object,
            _memoryCache,
            _mockLogger.Object,
            options);
    }

    [Fact]
    public async Task GetSecretAsync_ReturnsSecret_WhenSecretExists()
    {
        // Arrange
        const string secretName = "test-secret";
        const string secretValue = "test-value";
        const string fullSecretName = "test/test-secret";

        _mockSecretsManagerClient
            .Setup(x => x.GetSecretValueAsync(
                It.Is<GetSecretValueRequest>(r => r.SecretId == fullSecretName),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetSecretValueResponse
            {
                SecretString = secretValue
            });

        // Act
        var result = await _service.GetSecretAsync(secretName);

        // Assert
        result.Should().Be(secretValue);
        _mockSecretsManagerClient.Verify(
            x => x.GetSecretValueAsync(
                It.Is<GetSecretValueRequest>(r => r.SecretId == fullSecretName),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetSecretAsync_ReturnsCachedValue_WhenSecretIsCached()
    {
        // Arrange
        const string secretName = "test-secret";
        const string secretValue = "cached-value";

        // Pre-populate cache
        var cacheKey = $"secret:test/{secretName}";
        _memoryCache.Set(cacheKey, secretValue);

        // Act
        var result = await _service.GetSecretAsync(secretName);

        // Assert
        result.Should().Be(secretValue);
        _mockSecretsManagerClient.Verify(
            x => x.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetSecretAsync_ReturnsLocalValue_WhenInLocalDevelopmentMode()
    {
        // Arrange
        const string secretName = "test-secret";
        _settings.UseLocalDevelopment = true;

        // Act
        var result = await _service.GetSecretAsync(secretName);

        // Assert
        result.Should().Be($"local-{secretName}-value");
        _mockSecretsManagerClient.Verify(
            x => x.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetSecretAsync_ThrowsArgumentException_WhenSecretNameIsEmpty()
    {
        // Act & Assert
        await _service.Invoking(s => s.GetSecretAsync(string.Empty))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("Secret name cannot be null or empty*");
    }

    [Fact]
    public async Task GetSecretAsync_Generic_DeserializesJsonSecret_WhenSecretIsValidJson()
    {
        // Arrange
        const string secretName = "test-secret";
        var testObject = new TestSecret { Name = "Test", Value = 42 };
        var secretValue = JsonSerializer.Serialize(testObject);
        const string fullSecretName = "test/test-secret";

        _mockSecretsManagerClient
            .Setup(x => x.GetSecretValueAsync(
                It.Is<GetSecretValueRequest>(r => r.SecretId == fullSecretName),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetSecretValueResponse
            {
                SecretString = secretValue
            });

        // Act
        var result = await _service.GetSecretAsync<TestSecret>(secretName);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test");
        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task GetSecretAsync_Generic_ThrowsInvalidOperationException_WhenJsonIsInvalid()
    {
        // Arrange
        const string secretName = "test-secret";
        const string invalidJson = "{ invalid json }";
        const string fullSecretName = "test/test-secret";

        _mockSecretsManagerClient
            .Setup(x => x.GetSecretValueAsync(
                It.Is<GetSecretValueRequest>(r => r.SecretId == fullSecretName),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetSecretValueResponse
            {
                SecretString = invalidJson
            });

        // Act & Assert
        await _service.Invoking(s => s.GetSecretAsync<TestSecret>(secretName))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Failed to deserialize secret*");
    }

    [Fact]
    public async Task GetSecretsAsync_ReturnsMultipleSecrets_WhenAllSecretsExist()
    {
        // Arrange
        var secretNames = new[] { "secret1", "secret2", "secret3" };
        var expectedSecrets = new Dictionary<string, string>
        {
            { "secret1", "value1" },
            { "secret2", "value2" },
            { "secret3", "value3" }
        };

        foreach (var kvp in expectedSecrets)
        {
            _mockSecretsManagerClient
                .Setup(x => x.GetSecretValueAsync(
                    It.Is<GetSecretValueRequest>(r => r.SecretId == $"test/{kvp.Key}"),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetSecretValueResponse
                {
                    SecretString = kvp.Value
                });
        }

        // Act
        var result = await _service.GetSecretsAsync(secretNames);

        // Assert
        result.Should().HaveCount(3);
        result.Should().BeEquivalentTo(expectedSecrets);
    }

    [Fact]
    public async Task GetSecretsAsync_ReturnsEmptyDictionary_WhenNoSecretNamesProvided()
    {
        // Act
        var result = await _service.GetSecretsAsync(Array.Empty<string>());

        // Assert
        result.Should().BeEmpty();
        _mockSecretsManagerClient.Verify(
            x => x.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetSecretsAsync_ThrowsArgumentNullException_WhenSecretNamesIsNull()
    {
        // Act & Assert
        await _service.Invoking(s => s.GetSecretsAsync(null!))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void InvalidateCache_RemovesSecretFromCache()
    {
        // Arrange
        const string secretName = "test-secret";
        const string secretValue = "test-value";
        var cacheKey = $"secret:test/{secretName}";
        
        _memoryCache.Set(cacheKey, secretValue);
        _memoryCache.TryGetValue(cacheKey, out _).Should().BeTrue();

        // Act
        _service.InvalidateCache(secretName);

        // Assert
        _memoryCache.TryGetValue(cacheKey, out _).Should().BeFalse();
    }

    [Fact]
    public void InvalidateCache_ThrowsArgumentException_WhenSecretNameIsEmpty()
    {
        // Act & Assert
        _service.Invoking(s => s.InvalidateCache(string.Empty))
            .Should().Throw<ArgumentException>()
            .WithMessage("Secret name cannot be null or empty*");
    }

    [Fact]
    public void ClearCache_RemovesAllSecretsFromCache()
    {
        // Arrange
        _memoryCache.Set("secret:test/secret1", "value1");
        _memoryCache.Set("secret:test/secret2", "value2");
        _memoryCache.Set("other:key", "other-value");

        // Act
        _service.ClearCache();

        // Assert
        // Note: MemoryCache.Clear() removes all entries, not just secrets
        _memoryCache.TryGetValue("secret:test/secret1", out _).Should().BeFalse();
        _memoryCache.TryGetValue("secret:test/secret2", out _).Should().BeFalse();
        _memoryCache.TryGetValue("other:key", out _).Should().BeFalse();
    }

    [Fact]
    public async Task GetSecretAsync_RetriesOnFailure_WhenAmazonSecretsManagerExceptionOccurs()
    {
        // Arrange
        const string secretName = "test-secret";
        const string secretValue = "test-value";
        const string fullSecretName = "test/test-secret";

        _mockSecretsManagerClient
            .SetupSequence(x => x.GetSecretValueAsync(
                It.Is<GetSecretValueRequest>(r => r.SecretId == fullSecretName),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonSecretsManagerException("Temporary failure"))
            .ThrowsAsync(new AmazonSecretsManagerException("Another temporary failure"))
            .ReturnsAsync(new GetSecretValueResponse
            {
                SecretString = secretValue
            });

        // Act
        var result = await _service.GetSecretAsync(secretName);

        // Assert
        result.Should().Be(secretValue);
        _mockSecretsManagerClient.Verify(
            x => x.GetSecretValueAsync(
                It.Is<GetSecretValueRequest>(r => r.SecretId == fullSecretName),
                It.IsAny<CancellationToken>()),
            Times.Exactly(3)); // Initial attempt + 2 retries
    }

    [Fact]
    public async Task PreloadSecretsAsync_LoadsConfiguredSecrets()
    {
        // Arrange
        _settings.PreloadSecrets.AddRange(new[] { "secret1", "secret2" });

        _mockSecretsManagerClient
            .Setup(x => x.GetSecretValueAsync(
                It.Is<GetSecretValueRequest>(r => r.SecretId == "test/secret1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetSecretValueResponse { SecretString = "value1" });

        _mockSecretsManagerClient
            .Setup(x => x.GetSecretValueAsync(
                It.Is<GetSecretValueRequest>(r => r.SecretId == "test/secret2"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetSecretValueResponse { SecretString = "value2" });

        // Act
        await _service.PreloadSecretsAsync();

        // Assert
        _mockSecretsManagerClient.Verify(
            x => x.GetSecretValueAsync(
                It.Is<GetSecretValueRequest>(r => r.SecretId == "test/secret1"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mockSecretsManagerClient.Verify(
            x => x.GetSecretValueAsync(
                It.Is<GetSecretValueRequest>(r => r.SecretId == "test/secret2"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Verify secrets are cached
        _memoryCache.TryGetValue("secret:test/secret1", out var cachedValue1).Should().BeTrue();
        cachedValue1.Should().Be("value1");

        _memoryCache.TryGetValue("secret:test/secret2", out var cachedValue2).Should().BeTrue();
        cachedValue2.Should().Be("value2");
    }

    public void Dispose()
    {
        _service?.Dispose();
        _memoryCache?.Dispose();
    }

    private class TestSecret
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}
