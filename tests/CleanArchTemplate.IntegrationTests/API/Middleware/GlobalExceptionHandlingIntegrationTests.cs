using CleanArchTemplate.API.Models;
using CleanArchTemplate.Application.Common.Exceptions;
using CleanArchTemplate.Domain.Exceptions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace CleanArchTemplate.IntegrationTests.API.Middleware;

public class GlobalExceptionHandlingIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public GlobalExceptionHandlingIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GlobalExceptionMiddleware_WhenValidationErrorOccurs_ShouldReturnStandardizedErrorResponse()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        _client.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);

        // Create an invalid request that will trigger validation
        var invalidRequest = new
        {
            Email = "invalid-email", // Invalid email format
            Password = "123", // Too short
            FirstName = "", // Empty
            LastName = "" // Empty
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", invalidRequest);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        errorResponse.Should().NotBeNull();
        errorResponse!.StatusCode.Should().BeOneOf(400, 422);
        errorResponse.Title.Should().NotBeNullOrEmpty();
        errorResponse.Detail.Should().NotBeNullOrEmpty();
        errorResponse.Instance.Should().Be(correlationId);
        errorResponse.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task GlobalExceptionMiddleware_WhenResourceNotFound_ShouldReturn404WithStandardizedResponse()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        _client.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);

        // Act - Try to get a non-existent user
        var response = await _client.GetAsync($"/api/users/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        errorResponse.Should().NotBeNull();
        errorResponse!.StatusCode.Should().Be(404);
        errorResponse.Title.Should().NotBeNullOrEmpty();
        errorResponse.Instance.Should().Be(correlationId);
        errorResponse.Type.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GlobalExceptionMiddleware_WhenUnauthorized_ShouldReturn401WithStandardizedResponse()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        _client.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);

        // Act - Try to access a protected endpoint without authentication
        var response = await _client.GetAsync("/api/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        errorResponse.Should().NotBeNull();
        errorResponse!.StatusCode.Should().Be(401);
        errorResponse.Title.Should().Be("Unauthorized");
        errorResponse.Instance.Should().Be(correlationId);
        errorResponse.Type.Should().Contain("rfc7235");
    }

    [Fact]
    public async Task GlobalExceptionMiddleware_WhenNoCorrelationId_ShouldUseTraceIdentifier()
    {
        // Arrange - Don't add correlation ID header

        // Act
        var response = await _client.GetAsync($"/api/users/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        errorResponse.Should().NotBeNull();
        errorResponse!.Instance.Should().NotBeNullOrEmpty();
        errorResponse.Instance.Should().NotBe("unknown");
    }

    [Fact]
    public async Task GlobalExceptionMiddleware_ShouldIncludeTimestamp()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        _client.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);

        // Act
        var response = await _client.GetAsync($"/api/users/{Guid.NewGuid()}");

        // Assert
        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        errorResponse.Should().NotBeNull();
        errorResponse!.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task GlobalExceptionMiddleware_ShouldIncludeProperRFC7807Fields()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        _client.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);

        // Act
        var response = await _client.GetAsync($"/api/users/{Guid.NewGuid()}");

        // Assert
        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        errorResponse.Should().NotBeNull();
        
        // RFC 7807 Problem Details fields
        errorResponse!.StatusCode.Should().BeGreaterThan(0);
        errorResponse.Title.Should().NotBeNullOrEmpty();
        errorResponse.Detail.Should().NotBeNullOrEmpty();
        errorResponse.Type.Should().NotBeNullOrEmpty();
        errorResponse.Instance.Should().NotBeNullOrEmpty();
        
        // Custom fields
        errorResponse.Timestamp.Should().NotBe(default(DateTime));
    }

    [Theory]
    [InlineData("application/json")]
    [InlineData("application/xml")]
    [InlineData("text/plain")]
    public async Task GlobalExceptionMiddleware_ShouldAlwaysReturnJsonContentType(string acceptHeader)
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        _client.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);
        _client.DefaultRequestHeaders.Add("Accept", acceptHeader);

        // Act
        var response = await _client.GetAsync($"/api/users/{Guid.NewGuid()}");

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task GlobalExceptionMiddleware_WithLargeCorrelationId_ShouldHandleGracefully()
    {
        // Arrange
        var largeCorrelationId = new string('x', 1000); // Very large correlation ID
        _client.DefaultRequestHeaders.Add("X-Correlation-ID", largeCorrelationId);

        // Act
        var response = await _client.GetAsync($"/api/users/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        errorResponse.Should().NotBeNull();
        errorResponse!.Instance.Should().Be(largeCorrelationId);
    }

    [Fact]
    public async Task GlobalExceptionMiddleware_WithSpecialCharactersInCorrelationId_ShouldHandleGracefully()
    {
        // Arrange
        var specialCorrelationId = "test-123_@#$%^&*()";
        _client.DefaultRequestHeaders.Add("X-Correlation-ID", specialCorrelationId);

        // Act
        var response = await _client.GetAsync($"/api/users/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        errorResponse.Should().NotBeNull();
        errorResponse!.Instance.Should().Be(specialCorrelationId);
    }
}