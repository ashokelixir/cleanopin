using CleanArchTemplate.TestUtilities.Common;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace CleanArchTemplate.IntegrationTests.API.HealthChecks;

[Collection("Integration")]
public class HealthCheckIntegrationTests : BaseIntegrationTest
{
    public HealthCheckIntegrationTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task HealthCheck_Basic_ShouldReturnHealthy()
    {
        // Act
        var response = await Client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeEmpty();
        
        // Verify it's valid JSON
        var healthReport = JsonSerializer.Deserialize<JsonElement>(content);
        healthReport.GetProperty("status").GetString().Should().Be("Healthy");
    }

    [Fact]
    public async Task HealthCheck_Detailed_ShouldReturnDetailedInformation()
    {
        // Act
        var response = await Client.GetAsync("/health/detailed");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeEmpty();
        
        // Verify it's valid JSON with detailed information
        var healthReport = JsonSerializer.Deserialize<JsonElement>(content);
        healthReport.GetProperty("status").GetString().Should().BeOneOf("Healthy", "Degraded");
        healthReport.GetProperty("checks").Should().NotBeNull();
        healthReport.GetProperty("timestamp").Should().NotBeNull();
        healthReport.GetProperty("environment").Should().NotBeNull();
        healthReport.GetProperty("version").Should().NotBeNull();
        healthReport.GetProperty("machineName").Should().NotBeNull();
    }

    [Fact]
    public async Task HealthCheck_Ready_ShouldReturnReadinessStatus()
    {
        // Act
        var response = await Client.GetAsync("/health/ready");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeEmpty();
    }

    [Fact]
    public async Task HealthCheck_Live_ShouldReturnLivenessStatus()
    {
        // Act
        var response = await Client.GetAsync("/health/live");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeEmpty();
    }

    [Fact]
    public async Task HealthCheck_Database_ShouldBeIncluded()
    {
        // Act
        var response = await Client.GetAsync("/health/detailed");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var healthReport = JsonSerializer.Deserialize<JsonElement>(content);
        
        var checks = healthReport.GetProperty("checks");
        var checkNames = checks.EnumerateArray().Select(c => c.GetProperty("name").GetString()).ToList();
        
        checkNames.Should().Contain("database");
    }

    [Fact]
    public async Task HealthCheck_Memory_ShouldBeIncluded()
    {
        // Act
        var response = await Client.GetAsync("/health/detailed");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var healthReport = JsonSerializer.Deserialize<JsonElement>(content);
        
        var checks = healthReport.GetProperty("checks");
        var checkNames = checks.EnumerateArray().Select(c => c.GetProperty("name").GetString()).ToList();
        
        checkNames.Should().Contain("memory");
    }

    [Fact]
    public async Task HealthCheck_DiskSpace_ShouldBeIncluded()
    {
        // Act
        var response = await Client.GetAsync("/health/detailed");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var healthReport = JsonSerializer.Deserialize<JsonElement>(content);
        
        var checks = healthReport.GetProperty("checks");
        var checkNames = checks.EnumerateArray().Select(c => c.GetProperty("name").GetString()).ToList();
        
        checkNames.Should().Contain("disk_space");
    }

    [Fact]
    public async Task HealthCheck_Application_ShouldBeIncluded()
    {
        // Act
        var response = await Client.GetAsync("/health/detailed");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var healthReport = JsonSerializer.Deserialize<JsonElement>(content);
        
        var checks = healthReport.GetProperty("checks");
        var checkNames = checks.EnumerateArray().Select(c => c.GetProperty("name").GetString()).ToList();
        
        checkNames.Should().Contain("application");
    }

    [Fact]
    public async Task HealthCheck_ShouldIncludePerformanceMetrics()
    {
        // Act
        var response = await Client.GetAsync("/health/detailed");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var healthReport = JsonSerializer.Deserialize<JsonElement>(content);
        
        healthReport.GetProperty("totalDuration").Should().NotBeNull();
        
        var checks = healthReport.GetProperty("checks");
        foreach (var check in checks.EnumerateArray())
        {
            check.GetProperty("duration").Should().NotBeNull();
        }
    }

    [Fact]
    public async Task HealthCheck_ShouldHaveCorrectContentType()
    {
        // Act
        var response = await Client.GetAsync("/health");

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task HealthCheck_Detailed_ShouldHaveCorrectContentType()
    {
        // Act
        var response = await Client.GetAsync("/health/detailed");

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        response.Content.Headers.ContentType?.CharSet.Should().Be("utf-8");
    }
}