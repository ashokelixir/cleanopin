using CleanArchTemplate.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.Diagnostics;
using Xunit;
using FluentAssertions;

namespace CleanArchTemplate.UnitTests.Infrastructure.Services;

public class TelemetryServiceTests : IDisposable
{
    private readonly Mock<ILogger<TelemetryService>> _mockLogger;
    private readonly TelemetryService _telemetryService;

    public TelemetryServiceTests()
    {
        _mockLogger = new Mock<ILogger<TelemetryService>>();
        _telemetryService = new TelemetryService(_mockLogger.Object);
    }

    [Fact]
    public void StartActivity_ShouldCreateActivity_WhenCalled()
    {
        // Arrange
        var activityName = "TestActivity";
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        // Act
        using var activity = _telemetryService.StartActivity(activityName);

        // Assert
        activity.Should().NotBeNull();
        activity!.DisplayName.Should().Be(activityName);
    }

    [Fact]
    public void RecordMetric_ShouldNotThrow_WhenCalled()
    {
        // Arrange
        var metricName = "test_metric";
        var value = 42.0;
        var tags = new[] { new KeyValuePair<string, object?>("tag1", "value1") };

        // Act & Assert
        var act = () => _telemetryService.RecordMetric(metricName, value, tags);
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordCounter_ShouldNotThrow_WhenCalled()
    {
        // Arrange
        var counterName = "test_counter";
        var increment = 5L;
        var tags = new[] { new KeyValuePair<string, object?>("tag1", "value1") };

        // Act & Assert
        var act = () => _telemetryService.RecordCounter(counterName, increment, tags);
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordHistogram_ShouldNotThrow_WhenCalled()
    {
        // Arrange
        var histogramName = "test_histogram";
        var value = 123.45;
        var tags = new[] { new KeyValuePair<string, object?>("tag1", "value1") };

        // Act & Assert
        var act = () => _telemetryService.RecordHistogram(histogramName, value, tags);
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordException_ShouldNotThrow_WhenCalled()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var tags = new[] { new KeyValuePair<string, object?>("tag1", "value1") };

        // Act & Assert
        var act = () => _telemetryService.RecordException(exception, tags);
        act.Should().NotThrow();
    }

    [Fact]
    public void AddTag_ShouldNotThrow_WhenActivityExists()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);
        
        using var activity = _telemetryService.StartActivity("TestActivity");
        var key = "test_key";
        var value = "test_value";

        // Act & Assert
        var act = () => _telemetryService.AddTag(key, value);
        act.Should().NotThrow();
    }

    [Fact]
    public void AddEvent_ShouldNotThrow_WhenActivityExists()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);
        
        using var activity = _telemetryService.StartActivity("TestActivity");
        var eventName = "test_event";
        var tags = new[] { new KeyValuePair<string, object?>("tag1", "value1") };

        // Act & Assert
        var act = () => _telemetryService.AddEvent(eventName, tags);
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordDatabaseOperation_ShouldNotThrow_WhenCalled()
    {
        // Arrange
        var operation = "SELECT";
        var duration = TimeSpan.FromMilliseconds(100);
        var success = true;
        var tableName = "Users";

        // Act & Assert
        var act = () => _telemetryService.RecordDatabaseOperation(operation, duration, success, tableName);
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordCacheOperation_ShouldNotThrow_WhenCalled()
    {
        // Arrange
        var operation = "GET";
        var hit = true;
        var duration = TimeSpan.FromMilliseconds(50);

        // Act & Assert
        var act = () => _telemetryService.RecordCacheOperation(operation, hit, duration);
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordExternalServiceCall_ShouldNotThrow_WhenCalled()
    {
        // Arrange
        var serviceName = "external-api";
        var operation = "GET /users";
        var duration = TimeSpan.FromMilliseconds(200);
        var success = true;
        var statusCode = 200;

        // Act & Assert
        var act = () => _telemetryService.RecordExternalServiceCall(serviceName, operation, duration, success, statusCode);
        act.Should().NotThrow();
    }

    [Fact]
    public void StartActivity_WithKind_ShouldCreateActivityWithCorrectKind()
    {
        // Arrange
        var activityName = "TestActivity";
        var kind = ActivityKind.Client;
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        // Act
        using var activity = _telemetryService.StartActivity(activityName, kind);

        // Assert
        activity.Should().NotBeNull();
        activity!.Kind.Should().Be(kind);
    }

    [Fact]
    public void RecordDatabaseOperation_WithoutTableName_ShouldNotThrow()
    {
        // Arrange
        var operation = "SELECT";
        var duration = TimeSpan.FromMilliseconds(100);
        var success = true;

        // Act & Assert
        var act = () => _telemetryService.RecordDatabaseOperation(operation, duration, success);
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordCacheOperation_WithoutHit_ShouldNotThrow()
    {
        // Arrange
        var operation = "SET";
        var duration = TimeSpan.FromMilliseconds(50);

        // Act & Assert
        var act = () => _telemetryService.RecordCacheOperation(operation, null, duration);
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordExternalServiceCall_WithoutStatusCode_ShouldNotThrow()
    {
        // Arrange
        var serviceName = "external-api";
        var operation = "GET /users";
        var duration = TimeSpan.FromMilliseconds(200);
        var success = true;

        // Act & Assert
        var act = () => _telemetryService.RecordExternalServiceCall(serviceName, operation, duration, success);
        act.Should().NotThrow();
    }

    public void Dispose()
    {
        _telemetryService?.Dispose();
    }
}