using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Application.Common.Messages;
using CleanArchTemplate.Application.Features.Permissions.EventHandlers;
using CleanArchTemplate.Domain.Events;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CleanArchTemplate.UnitTests.Application.Features.Permissions.EventHandlers;

public class PermissionMessagingEventHandlerTests
{
    private readonly Mock<IMessagePublisher> _mockMessagePublisher;
    private readonly Mock<ILogger<PermissionMessagingEventHandler>> _mockLogger;
    private readonly PermissionMessagingEventHandler _handler;

    public PermissionMessagingEventHandlerTests()
    {
        _mockMessagePublisher = new Mock<IMessagePublisher>();
        _mockLogger = new Mock<ILogger<PermissionMessagingEventHandler>>();
        _handler = new PermissionMessagingEventHandler(_mockMessagePublisher.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_PermissionCreatedEvent_PublishesMessage()
    {
        // Arrange
        var permissionId = Guid.NewGuid();
        var domainEvent = new PermissionCreatedEvent(
            permissionId,
            "User",
            "Read",
            "User.Read",
            "Read user information",
            "User Management",
            null);

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _mockMessagePublisher.Verify(
            x => x.PublishAsync(
                It.Is<PermissionCreatedMessage>(m => 
                    m.PermissionId == permissionId &&
                    m.Resource == "User" &&
                    m.Action == "Read" &&
                    m.Name == "User.Read"),
                "permission-events",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_UserPermissionAssignedEvent_PublishesMessage()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();
        var domainEvent = new UserPermissionAssignedEvent(
            userId,
            permissionId,
            "User.Read",
            CleanArchTemplate.Domain.Enums.PermissionState.Grant,
            "Test assignment");

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _mockMessagePublisher.Verify(
            x => x.PublishAsync(
                It.Is<UserPermissionAssignedMessage>(m => 
                    m.UserId == userId &&
                    m.PermissionId == permissionId &&
                    m.PermissionName == "User.Read" &&
                    m.State == "Grant"),
                "user-permission-events",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_MessagePublisherThrows_DoesNotRethrow()
    {
        // Arrange
        var domainEvent = new PermissionCreatedEvent(
            Guid.NewGuid(),
            "User",
            "Read",
            "User.Read",
            "Read user information",
            "User Management",
            null);

        _mockMessagePublisher
            .Setup(x => x.PublishAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("SQS error"));

        // Act & Assert - Should not throw
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to publish PermissionCreated message")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}