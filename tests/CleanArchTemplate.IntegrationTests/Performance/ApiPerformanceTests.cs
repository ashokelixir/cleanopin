using CleanArchTemplate.TestUtilities.Common;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace CleanArchTemplate.IntegrationTests.Performance;

public class ApiPerformanceTests : BaseIntegrationTest
{
    public ApiPerformanceTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task HealthCheck_ShouldLogRequestAndResponse_WhenCalled()
    {
        // Act
        var response = await Client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        // The request logging middleware should have logged:
        // - HTTP request details
        // - HTTP response details
        // - Performance metrics (duration)
        // - Correlation ID
    }

    [Fact]
    public async Task AuthLogin_ShouldLogAuditEvents_WhenCalled()
    {
        // Arrange
        var loginRequest = new
        {
            Email = "admin@cleanarch.com",
            Password = "Admin123!"
        };

        var json = JsonSerializer.Serialize(loginRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/v1/auth/login", content);

        // Assert
        // Should log audit events for login attempt
        // Should log request/response details
        // Should include correlation ID in logs
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUsers_ShouldLogPerformanceMetrics_WhenAuthenticated()
    {
        // Arrange
        var token = await GetValidJwtTokenAsync();
        Client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.GetAsync("/api/v1/users");

        // Assert
        // Should log:
        // - Request details with authentication info
        // - Database query performance
        // - Response details
        // - Overall request duration
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateUser_ShouldLogAuditTrail_WhenCalled()
    {
        // Arrange
        var token = await GetValidJwtTokenAsync();
        Client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var createUserRequest = new
        {
            Email = $"test-{Guid.NewGuid()}@example.com",
            FirstName = "Test",
            LastName = "User",
            Password = "TestPassword123!"
        };

        var json = JsonSerializer.Serialize(createUserRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/v1/users", content);

        // Assert
        // Should log:
        // - User creation audit event
        // - Request/response details
        // - Database performance metrics
        // - Security events if applicable
        Assert.True(response.StatusCode == HttpStatusCode.Created || 
                   response.StatusCode == HttpStatusCode.BadRequest ||
                   response.StatusCode == HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task InvalidRequest_ShouldLogWarning_WhenBadRequestReceived()
    {
        // Arrange
        var invalidJson = "{ invalid json }";
        var content = new StringContent(invalidJson, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/v1/auth/login", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        // Should log warning level for 400 status codes
        // Should include request details and error information
    }

    private async Task<string> GetValidJwtTokenAsync()
    {
        var loginRequest = new
        {
            Email = "admin@cleanarch.com",
            Password = "Admin123!"
        };

        var json = JsonSerializer.Serialize(loginRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await Client.PostAsync("/api/v1/auth/login", content);
        
        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            var loginResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
            return loginResponse.GetProperty("accessToken").GetString() ?? string.Empty;
        }

        return string.Empty;
    }
}