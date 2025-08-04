using CleanArchTemplate.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CleanArchTemplate.UnitTests.Infrastructure.Services;

public class AuditLogServiceTests
{
    private readonly Mock<ILogger<AuditLogService>> _mockLogger;
    private readonly AuditLogService _auditLogService;

    public AuditLogServiceTests()
    {
        _mockLogger = new Mock<ILogger<AuditLogService>>();
        _auditLogService = new AuditLogService(_mockLogger.Object);
    }

    [Fact]
    public async Task LogUserActionAsync_ShouldLogInformation_WhenCalled()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var action = "UserCreated";
        var details = "User created successfully";
        var additionalData = new { Email = "test@example.com" };

        // Act
        await _auditLogService.LogUserActionAsync(action, userId, details, additionalData);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("User action performed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task LogSecurityEventAsync_ShouldLogWarning_WhenCalled()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var eventType = "LoginFailure";
        var description = "Invalid password attempt";
        var ipAddress = "192.168.1.1";
        var additionalData = new { Email = "test@example.com" };

        // Act
        await _auditLogService.LogSecurityEventAsync(eventType, description, userId, ipAddress, additionalData);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Security event")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task LogRoleActionAsync_ShouldLogInformation_WhenCalled()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var performedByUserId = Guid.NewGuid();
        var action = "RoleCreated";
        var details = "New role created";
        var additionalData = new { RoleName = "Admin" };

        // Act
        await _auditLogService.LogRoleActionAsync(action, roleId, performedByUserId, details, additionalData);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Role action performed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task LogDataAccessAsync_ShouldLogInformation_WhenCalled()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var operation = "Update";
        var entityType = "User";
        var changes = new { FirstName = "John", LastName = "Doe" };

        // Act
        await _auditLogService.LogDataAccessAsync(operation, entityType, entityId, userId, changes);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Data access")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}