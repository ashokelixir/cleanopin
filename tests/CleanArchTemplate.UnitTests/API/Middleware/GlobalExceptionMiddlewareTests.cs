using CleanArchTemplate.API.Middleware;
using CleanArchTemplate.API.Models;
using CleanArchTemplate.Application.Common.Exceptions;
using CleanArchTemplate.Domain.Exceptions;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Security;
using System.Text.Json;
using Xunit;

namespace CleanArchTemplate.UnitTests.API.Middleware;

public class GlobalExceptionMiddlewareTests
{
    private readonly Mock<ILogger<GlobalExceptionMiddleware>> _loggerMock;
    private readonly Mock<IWebHostEnvironment> _environmentMock;
    private readonly GlobalExceptionMiddleware _middleware;
    private readonly DefaultHttpContext _httpContext;

    public GlobalExceptionMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger<GlobalExceptionMiddleware>>();
        _environmentMock = new Mock<IWebHostEnvironment>();
        _httpContext = new DefaultHttpContext();
        _httpContext.Response.Body = new MemoryStream();

        _middleware = new GlobalExceptionMiddleware(
            _ => throw new InvalidOperationException("Test exception"),
            _loggerMock.Object,
            _environmentMock.Object);
    }

    [Fact]
    public async Task InvokeAsync_WhenDomainValidationExceptionThrown_ShouldReturn422WithErrors()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2" };
        var exception = new DomainValidationException("Validation failed", errors);
        var correlationId = "test-correlation-id";
        
        _httpContext.Request.Headers["X-Correlation-ID"] = correlationId;
        
        var middleware = new GlobalExceptionMiddleware(
            _ => throw exception,
            _loggerMock.Object,
            _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.UnprocessableEntity);
        _httpContext.Response.ContentType.Should().Be("application/json");

        _httpContext.Response.Body.Position = 0;
        var responseBody = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        errorResponse.Should().NotBeNull();
        errorResponse!.StatusCode.Should().Be((int)HttpStatusCode.UnprocessableEntity);
        errorResponse.Title.Should().Be("Validation Error");
        errorResponse.Detail.Should().Be("Validation failed");
        errorResponse.Instance.Should().Be(correlationId);
        errorResponse.Errors.Should().BeEquivalentTo(errors);
    }

    [Fact]
    public async Task InvokeAsync_WhenEntityNotFoundExceptionThrown_ShouldReturn404()
    {
        // Arrange
        var exception = new EntityNotFoundException("User", "123");
        var correlationId = "test-correlation-id";
        
        _httpContext.Request.Headers["X-Correlation-ID"] = correlationId;
        
        var middleware = new GlobalExceptionMiddleware(
            _ => throw exception,
            _loggerMock.Object,
            _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.NotFound);

        _httpContext.Response.Body.Position = 0;
        var responseBody = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        errorResponse.Should().NotBeNull();
        errorResponse!.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
        errorResponse.Title.Should().Be("Resource Not Found");
        errorResponse.Instance.Should().Be(correlationId);
    }

    [Fact]
    public async Task InvokeAsync_WhenBusinessRuleViolationExceptionThrown_ShouldReturn400WithRule()
    {
        // Arrange
        var rule = "UserMustBeActive";
        var exception = new BusinessRuleViolationException(rule, "User must be active to perform this action");
        var correlationId = "test-correlation-id";
        
        _httpContext.Request.Headers["X-Correlation-ID"] = correlationId;
        
        var middleware = new GlobalExceptionMiddleware(
            _ => throw exception,
            _loggerMock.Object,
            _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

        _httpContext.Response.Body.Position = 0;
        var responseBody = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        errorResponse.Should().NotBeNull();
        errorResponse!.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        errorResponse.Title.Should().Be("Business Rule Violation");
        errorResponse.Extensions.Should().ContainKey("rule");
        errorResponse.Extensions!["rule"].ToString().Should().Be(rule);
    }

    [Fact]
    public async Task InvokeAsync_WhenConflictExceptionThrown_ShouldReturn409WithConflictDetails()
    {
        // Arrange
        var resource = "Email";
        var conflictingValue = "test@example.com";
        var exception = new ConflictException(resource, conflictingValue, "Email already exists");
        var correlationId = "test-correlation-id";
        
        _httpContext.Request.Headers["X-Correlation-ID"] = correlationId;
        
        var middleware = new GlobalExceptionMiddleware(
            _ => throw exception,
            _loggerMock.Object,
            _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.Conflict);

        _httpContext.Response.Body.Position = 0;
        var responseBody = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        errorResponse.Should().NotBeNull();
        errorResponse!.StatusCode.Should().Be((int)HttpStatusCode.Conflict);
        errorResponse.Title.Should().Be("Resource Conflict");
        errorResponse.Extensions.Should().ContainKey("resource");
        errorResponse.Extensions.Should().ContainKey("conflictingValue");
    }

    [Fact]
    public async Task InvokeAsync_WhenFluentValidationExceptionThrown_ShouldReturn422WithValidationErrors()
    {
        // Arrange
        var validationFailures = new[]
        {
            new FluentValidation.Results.ValidationFailure("Name", "Name is required"),
            new FluentValidation.Results.ValidationFailure("Email", "Email is invalid")
        };
        var exception = new ValidationException(validationFailures);
        var correlationId = "test-correlation-id";
        
        _httpContext.Request.Headers["X-Correlation-ID"] = correlationId;
        
        var middleware = new GlobalExceptionMiddleware(
            _ => throw exception,
            _loggerMock.Object,
            _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.UnprocessableEntity);

        _httpContext.Response.Body.Position = 0;
        var responseBody = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        errorResponse.Should().NotBeNull();
        errorResponse!.StatusCode.Should().Be((int)HttpStatusCode.UnprocessableEntity);
        errorResponse.Title.Should().Be("Validation Error");
        errorResponse.Errors.Should().HaveCount(2);
        errorResponse.Errors.Should().Contain("Name is required");
        errorResponse.Errors.Should().Contain("Email is invalid");
    }

    [Fact]
    public async Task InvokeAsync_WhenUnauthorizedAccessExceptionThrown_ShouldReturn401()
    {
        // Arrange
        var exception = new UnauthorizedAccessException("Access denied");
        var correlationId = "test-correlation-id";
        
        _httpContext.Request.Headers["X-Correlation-ID"] = correlationId;
        
        var middleware = new GlobalExceptionMiddleware(
            _ => throw exception,
            _loggerMock.Object,
            _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);

        _httpContext.Response.Body.Position = 0;
        var responseBody = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        errorResponse.Should().NotBeNull();
        errorResponse!.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);
        errorResponse.Title.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task InvokeAsync_WhenSecurityExceptionThrown_ShouldReturn403()
    {
        // Arrange
        var exception = new SecurityException("Forbidden");
        var correlationId = "test-correlation-id";
        
        _httpContext.Request.Headers["X-Correlation-ID"] = correlationId;
        
        var middleware = new GlobalExceptionMiddleware(
            _ => throw exception,
            _loggerMock.Object,
            _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.Forbidden);

        _httpContext.Response.Body.Position = 0;
        var responseBody = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        errorResponse.Should().NotBeNull();
        errorResponse!.StatusCode.Should().Be((int)HttpStatusCode.Forbidden);
        errorResponse.Title.Should().Be("Forbidden");
    }

    [Fact]
    public async Task InvokeAsync_WhenTimeoutExceptionThrown_ShouldReturn408()
    {
        // Arrange
        var exception = new TimeoutException("Request timed out");
        var correlationId = "test-correlation-id";
        
        _httpContext.Request.Headers["X-Correlation-ID"] = correlationId;
        
        var middleware = new GlobalExceptionMiddleware(
            _ => throw exception,
            _loggerMock.Object,
            _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.RequestTimeout);

        _httpContext.Response.Body.Position = 0;
        var responseBody = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        errorResponse.Should().NotBeNull();
        errorResponse!.StatusCode.Should().Be((int)HttpStatusCode.RequestTimeout);
        errorResponse.Title.Should().Be("Request Timeout");
    }

    [Fact]
    public async Task InvokeAsync_WhenUnhandledExceptionInDevelopment_ShouldReturn500WithDetails()
    {
        // Arrange
        var exception = new Exception("Something went wrong");
        var correlationId = "test-correlation-id";
        
        _httpContext.Request.Headers["X-Correlation-ID"] = correlationId;
        _environmentMock.Setup(x => x.EnvironmentName).Returns(Environments.Development);
        
        var middleware = new GlobalExceptionMiddleware(
            _ => throw exception,
            _loggerMock.Object,
            _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

        _httpContext.Response.Body.Position = 0;
        var responseBody = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        errorResponse.Should().NotBeNull();
        errorResponse!.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
        errorResponse.Title.Should().Be("Internal Server Error");
        errorResponse.Detail.Should().Be("Something went wrong");
        errorResponse.Extensions.Should().ContainKey("stackTrace");
        errorResponse.Extensions.Should().ContainKey("exceptionType");
    }

    [Fact]
    public async Task InvokeAsync_WhenUnhandledExceptionInProduction_ShouldReturn500WithoutDetails()
    {
        // Arrange
        var exception = new Exception("Something went wrong");
        var correlationId = "test-correlation-id";
        
        _httpContext.Request.Headers["X-Correlation-ID"] = correlationId;
        _environmentMock.Setup(x => x.EnvironmentName).Returns(Environments.Production);
        
        var middleware = new GlobalExceptionMiddleware(
            _ => throw exception,
            _loggerMock.Object,
            _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

        _httpContext.Response.Body.Position = 0;
        var responseBody = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        errorResponse.Should().NotBeNull();
        errorResponse!.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
        errorResponse.Title.Should().Be("Internal Server Error");
        errorResponse.Detail.Should().Be("An unexpected error occurred. Please try again later.");
        errorResponse.Extensions.Should().BeNull();
    }

    [Fact]
    public async Task InvokeAsync_WhenNoCorrelationIdHeader_ShouldUseTraceIdentifier()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        _httpContext.TraceIdentifier = "trace-123";
        _environmentMock.Setup(x => x.EnvironmentName).Returns(Environments.Production);
        
        var middleware = new GlobalExceptionMiddleware(
            _ => throw exception,
            _loggerMock.Object,
            _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.Body.Position = 0;
        var responseBody = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        errorResponse.Should().NotBeNull();
        errorResponse!.Instance.Should().Be("trace-123");
    }

    [Fact]
    public async Task InvokeAsync_ShouldLogErrorWithCorrelationId()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var correlationId = "test-correlation-id";
        
        _httpContext.Request.Headers["X-Correlation-ID"] = correlationId;
        _httpContext.Request.Path = "/api/test";
        _httpContext.Request.Method = "POST";
        
        var middleware = new GlobalExceptionMiddleware(
            _ => throw exception,
            _loggerMock.Object,
            _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

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
}