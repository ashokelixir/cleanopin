using CleanArchTemplate.API.Middleware;
using CleanArchTemplate.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Diagnostics;
using Xunit;
using FluentAssertions;

namespace CleanArchTemplate.UnitTests.API.Middleware;

public class TelemetryMiddlewareTests
{
    private readonly Mock<ITelemetryService> _mockTelemetryService;
    private readonly Mock<ILogger<TelemetryMiddleware>> _mockLogger;
    private readonly Mock<RequestDelegate> _mockNext;
    private readonly TelemetryMiddleware _middleware;

    public TelemetryMiddlewareTests()
    {
        _mockTelemetryService = new Mock<ITelemetryService>();
        _mockLogger = new Mock<ILogger<TelemetryMiddleware>>();
        _mockNext = new Mock<RequestDelegate>();
        _middleware = new TelemetryMiddleware(_mockNext.Object, _mockTelemetryService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task InvokeAsync_ShouldStartActivity_WhenRequestProcessed()
    {
        // Arrange
        var context = CreateHttpContext();
        var mockActivity = new Mock<Activity>("test");
        
        _mockTelemetryService
            .Setup(x => x.StartActivity(It.IsAny<string>(), ActivityKind.Server))
            .Returns(mockActivity.Object);

        _mockNext
            .Setup(x => x(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _mockTelemetryService.Verify(
            x => x.StartActivity(It.Is<string>(s => s.Contains("GET") && s.Contains("/test")), ActivityKind.Server),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldAddRequestTags_WhenRequestProcessed()
    {
        // Arrange
        var context = CreateHttpContext();
        var mockActivity = new Mock<Activity>("test");
        
        _mockTelemetryService
            .Setup(x => x.StartActivity(It.IsAny<string>(), ActivityKind.Server))
            .Returns(mockActivity.Object);

        _mockNext
            .Setup(x => x(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _mockTelemetryService.Verify(x => x.AddTag("http.method", "GET"), Times.Once);
        _mockTelemetryService.Verify(x => x.AddTag("http.path", "/test"), Times.Once);
        _mockTelemetryService.Verify(x => x.AddTag("http.scheme", "https"), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldRecordMetrics_WhenRequestCompleted()
    {
        // Arrange
        var context = CreateHttpContext();
        var mockActivity = new Mock<Activity>("test");
        
        _mockTelemetryService
            .Setup(x => x.StartActivity(It.IsAny<string>(), ActivityKind.Server))
            .Returns(mockActivity.Object);

        _mockNext
            .Setup(x => x(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _mockTelemetryService.Verify(
            x => x.RecordCounter("http_requests_total", 1, It.IsAny<KeyValuePair<string, object?>[]>()),
            Times.Once);
            
        _mockTelemetryService.Verify(
            x => x.RecordHistogram("http_request_duration_seconds", It.IsAny<double>(), It.IsAny<KeyValuePair<string, object?>[]>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldRecordException_WhenExceptionThrown()
    {
        // Arrange
        var context = CreateHttpContext();
        var mockActivity = new Mock<Activity>("test");
        var exception = new InvalidOperationException("Test exception");
        
        _mockTelemetryService
            .Setup(x => x.StartActivity(It.IsAny<string>(), ActivityKind.Server))
            .Returns(mockActivity.Object);

        _mockNext
            .Setup(x => x(It.IsAny<HttpContext>()))
            .ThrowsAsync(exception);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _middleware.InvokeAsync(context));
        
        _mockTelemetryService.Verify(
            x => x.RecordException(exception, It.IsAny<KeyValuePair<string, object?>[]>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldAddUserTags_WhenUserAuthenticated()
    {
        // Arrange
        var context = CreateHttpContext();
        context.User = CreateAuthenticatedUser();
        
        var mockActivity = new Mock<Activity>("test");
        
        _mockTelemetryService
            .Setup(x => x.StartActivity(It.IsAny<string>(), ActivityKind.Server))
            .Returns(mockActivity.Object);

        _mockNext
            .Setup(x => x(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _mockTelemetryService.Verify(x => x.AddTag("user.id", "test-user-id"), Times.Once);
        _mockTelemetryService.Verify(x => x.AddTag("user.name", "testuser"), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldAddCorrelationId_WhenHeaderPresent()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Headers["X-Correlation-ID"] = "test-correlation-id";
        
        var mockActivity = new Mock<Activity>("test");
        
        _mockTelemetryService
            .Setup(x => x.StartActivity(It.IsAny<string>(), ActivityKind.Server))
            .Returns(mockActivity.Object);

        _mockNext
            .Setup(x => x(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _mockTelemetryService.Verify(x => x.AddTag("correlation.id", "test-correlation-id"), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldRecordBusinessMetrics_ForAuthEndpoints()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Path = "/api/auth/login";
        
        var mockActivity = new Mock<Activity>("test");
        
        _mockTelemetryService
            .Setup(x => x.StartActivity(It.IsAny<string>(), ActivityKind.Server))
            .Returns(mockActivity.Object);

        _mockNext
            .Setup(x => x(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _mockTelemetryService.Verify(
            x => x.RecordCounter("auth_operations_total", 1, It.IsAny<KeyValuePair<string, object?>[]>()),
            Times.Once);
    }

    private static HttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/test";
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("localhost");
        context.Response.StatusCode = 200;
        return context;
    }

    private static System.Security.Claims.ClaimsPrincipal CreateAuthenticatedUser()
    {
        var claims = new[]
        {
            new System.Security.Claims.Claim("sub", "test-user-id"),
            new System.Security.Claims.Claim("name", "testuser")
        };
        
        var identity = new System.Security.Claims.ClaimsIdentity(claims, "test");
        return new System.Security.Claims.ClaimsPrincipal(identity);
    }
}