using CleanArchTemplate.Application.Common.Messages;
using CleanArchTemplate.Infrastructure.Messaging.Handlers;
using Microsoft.Extensions.Logging;
using Moq;

namespace CleanArchTemplate.UnitTests.Infrastructure.Messaging;

public class UserMessageHandlerTests
{
    private readonly Mock<ILogger<UserMessageHandler>> _mockLogger;
    private readonly UserMessageHandler _handler;

    public UserMessageHandlerTests()
    {
        _mockLogger = new Mock<ILogger<UserMessageHandler>>();
        _handler = new UserMessageHandler(_mockLogger.Object);
    }

    [Fact]
    public async Task HandleUserCreatedAsync_ShouldProcessMessage_WhenValidMessageProvided()
    {
        // Arrange
        var message = new UserCreatedMessage
        {
            UserId = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            IsEmailVerified = false,
            Roles = new List<string> { "User" }
        };

        // Act
        await _handler.HandleUserCreatedAsync(message);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Processing user created message for user {message.UserId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Successfully processed user created message for user {message.UserId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleUserUpdatedAsync_ShouldProcessMessage_WhenValidMessageProvided()
    {
        // Arrange
        var message = new UserUpdatedMessage
        {
            UserId = Guid.NewGuid(),
            Email = "updated@example.com",
            FirstName = "Updated",
            LastName = "User",
            IsActive = true,
            UpdatedFields = new List<string> { "FirstName", "Email" }
        };

        // Act
        await _handler.HandleUserUpdatedAsync(message);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Processing user updated message for user {message.UserId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleUserDeletedAsync_ShouldProcessMessage_WhenValidMessageProvided()
    {
        // Arrange
        var message = new UserDeletedMessage
        {
            UserId = Guid.NewGuid(),
            Email = "deleted@example.com",
            DeletionReason = "Account closure requested"
        };

        // Act
        await _handler.HandleUserDeletedAsync(message);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Processing user deleted message for user {message.UserId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}

public class PermissionMessageHandlerTests
{
    private readonly Mock<ILogger<PermissionMessageHandler>> _mockLogger;
    private readonly PermissionMessageHandler _handler;

    public PermissionMessageHandlerTests()
    {
        _mockLogger = new Mock<ILogger<PermissionMessageHandler>>();
        _handler = new PermissionMessageHandler(_mockLogger.Object);
    }

    [Fact]
    public async Task HandlePermissionAssignedAsync_ShouldProcessMessage_WhenValidMessageProvided()
    {
        // Arrange
        var message = new PermissionAssignedMessage
        {
            UserId = Guid.NewGuid(),
            PermissionId = Guid.NewGuid(),
            PermissionName = "test.permission",
            AssignedByUserId = Guid.NewGuid(),
            Reason = "Testing purposes"
        };

        // Act
        await _handler.HandlePermissionAssignedAsync(message);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Processing permission assigned message. User: {message.UserId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandlePermissionRemovedAsync_ShouldProcessMessage_WhenValidMessageProvided()
    {
        // Arrange
        var message = new PermissionRemovedMessage
        {
            UserId = Guid.NewGuid(),
            PermissionId = Guid.NewGuid(),
            PermissionName = "test.permission",
            RemovedByUserId = Guid.NewGuid(),
            Reason = "Access revoked"
        };

        // Act
        await _handler.HandlePermissionRemovedAsync(message);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Processing permission removed message. User: {message.UserId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleBulkPermissionsAssignedAsync_ShouldProcessMessage_WhenValidMessageProvided()
    {
        // Arrange
        var message = new BulkPermissionsAssignedMessage
        {
            UserId = Guid.NewGuid(),
            PermissionIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
            PermissionNames = new List<string> { "permission1", "permission2" },
            AssignedByUserId = Guid.NewGuid(),
            Reason = "Bulk assignment"
        };

        // Act
        await _handler.HandleBulkPermissionsAssignedAsync(message);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Processing bulk permissions assigned message. User: {message.UserId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}