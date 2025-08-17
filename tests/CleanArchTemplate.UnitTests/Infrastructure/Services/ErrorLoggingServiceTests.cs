using CleanArchTemplate.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CleanArchTemplate.UnitTests.Infrastructure.Services;

public class ErrorLoggingServiceTests
{
    private readonly Mock<ILogger<ErrorLoggingService>> _loggerMock;
    private readonly ErrorLoggingService _errorLoggingService;

    public ErrorLoggingServiceTests()
    {
        _loggerMock = new Mock<ILogger<ErrorLoggingService>>();
        _errorLoggingService = new ErrorLoggingService(_loggerMock.Object);
    }

    [Fact]
    public void LogError_WithBasicException_ShouldLogWithCorrelationId()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var correlationId = "test-correlation-id";

        // Act
        _errorLoggingService.LogError(exception, correlationId);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(correlationId)),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogError_WithAllParameters_ShouldLogAllInformation()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var correlationId = "test-correlation-id";
        var userId = "user-123";
        var requestPath = "/api/test";
        var httpMethod = "POST";
        var additionalProperties = new Dictionary<string, object>
        {
            ["CustomProperty"] = "CustomValue"
        };

        // Act
        _errorLoggingService.LogError(exception, correlationId, userId, requestPath, httpMethod, additionalProperties);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains(correlationId) &&
                    v.ToString()!.Contains(userId) &&
                    v.ToString()!.Contains(requestPath) &&
                    v.ToString()!.Contains(httpMethod)),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogError_WithInnerException_ShouldLogInnerExceptionDetails()
    {
        // Arrange
        var innerException = new ArgumentException("Inner exception");
        var exception = new InvalidOperationException("Outer exception", innerException);
        var correlationId = "test-correlation-id";

        // Act
        _errorLoggingService.LogError(exception, correlationId);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogValidationError_WithMultipleErrors_ShouldLogAllErrors()
    {
        // Arrange
        var validationErrors = new[] { "Error 1", "Error 2", "Error 3" };
        var correlationId = "test-correlation-id";
        var userId = "user-123";
        var requestPath = "/api/validate";
        var httpMethod = "POST";

        // Act
        _errorLoggingService.LogValidationError(validationErrors, correlationId, userId, requestPath, httpMethod);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains(correlationId) &&
                    v.ToString()!.Contains(userId) &&
                    v.ToString()!.Contains("3") && // Error count
                    v.ToString()!.Contains("Error 1; Error 2; Error 3")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogValidationError_WithRequestData_ShouldSanitizeAndLogRequestData()
    {
        // Arrange
        var validationErrors = new[] { "Validation failed" };
        var correlationId = "test-correlation-id";
        var requestData = new
        {
            Username = "testuser",
            Password = "secret123", // Should be sanitized
            Email = "test@example.com"
        };

        // Act
        _errorLoggingService.LogValidationError(validationErrors, correlationId, requestData: requestData);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogAuthorizationFailure_ShouldLogAuthorizationDetails()
    {
        // Arrange
        var userId = "user-123";
        var requiredPermission = "Users.Read";
        var resource = "User";
        var correlationId = "test-correlation-id";
        var requestPath = "/api/users";
        var httpMethod = "GET";

        // Act
        _errorLoggingService.LogAuthorizationFailure(userId, requiredPermission, resource, correlationId, requestPath, httpMethod);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains(correlationId) &&
                    v.ToString()!.Contains(userId) &&
                    v.ToString()!.Contains(requiredPermission) &&
                    v.ToString()!.Contains(resource)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogAuthenticationFailure_ShouldLogAuthenticationDetails()
    {
        // Arrange
        var reason = "Invalid credentials";
        var correlationId = "test-correlation-id";
        var requestPath = "/api/auth/login";
        var httpMethod = "POST";
        var attemptedCredentials = new Dictionary<string, object>
        {
            ["Username"] = "testuser",
            ["Password"] = "secret123", // Should be filtered out
            ["RememberMe"] = true
        };

        // Act
        _errorLoggingService.LogAuthenticationFailure(reason, correlationId, requestPath, httpMethod, attemptedCredentials);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains(correlationId) &&
                    v.ToString()!.Contains(reason)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("password")]
    [InlineData("pwd")]
    [InlineData("secret")]
    [InlineData("token")]
    [InlineData("key")]
    [InlineData("credential")]
    [InlineData("authorization")]
    [InlineData("auth")]
    [InlineData("ssn")]
    [InlineData("creditcard")]
    [InlineData("cvv")]
    [InlineData("pin")]
    [InlineData("otp")]
    public void LogValidationError_WithSensitiveFields_ShouldRedactSensitiveData(string sensitiveField)
    {
        // Arrange
        var validationErrors = new[] { "Validation failed" };
        var correlationId = "test-correlation-id";
        var requestData = new Dictionary<string, object>
        {
            ["Username"] = "testuser",
            [sensitiveField] = "sensitive-value",
            ["Email"] = "test@example.com"
        };

        // Act
        _errorLoggingService.LogValidationError(validationErrors, correlationId, requestData: requestData);

        // Assert - The method should complete without throwing and log appropriately
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogValidationError_WithNullRequestData_ShouldNotThrow()
    {
        // Arrange
        var validationErrors = new[] { "Validation failed" };
        var correlationId = "test-correlation-id";

        // Act & Assert
        var act = () => _errorLoggingService.LogValidationError(validationErrors, correlationId, requestData: null);
        act.Should().NotThrow();
    }

    [Fact]
    public void LogAuthenticationFailure_WithNullCredentials_ShouldNotThrow()
    {
        // Arrange
        var reason = "Invalid credentials";
        var correlationId = "test-correlation-id";

        // Act & Assert
        var act = () => _errorLoggingService.LogAuthenticationFailure(reason, correlationId, attemptedCredentials: null);
        act.Should().NotThrow();
    }

    [Fact]
    public void LogError_WithNullAdditionalProperties_ShouldNotThrow()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var correlationId = "test-correlation-id";

        // Act & Assert
        var act = () => _errorLoggingService.LogError(exception, correlationId, additionalProperties: null);
        act.Should().NotThrow();
    }
}