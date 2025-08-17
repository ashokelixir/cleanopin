using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Application.Features.Permissions.EventHandlers;
using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.Enums;
using CleanArchTemplate.Domain.Events;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CleanArchTemplate.UnitTests.Application.Features.Permissions.EventHandlers;

public class PermissionAuditEventHandlerTests
{
    private readonly Mock<IPermissionAuditService> _mockAuditService;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<ILogger<PermissionAuditEventHandler>> _mockLogger;
    private readonly PermissionAuditEventHandler _handler;

    public PermissionAuditEventHandlerTests()
    {
        _mockAuditService = new Mock<IPermissionAuditService>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockLogger = new Mock<ILogger<PermissionAuditEventHandler>>();

        _handler = new PermissionAuditEventHandler(
            _mockAuditService.Object,
            _mockCurrentUserService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_UserPermissionAssignedEvent_ShouldLogAuditEntry()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();
        var permissionName = "Users.Read";
        var reason = "User needs read access";
        var performedBy = "admin@test.com";

        var domainEvent = new UserPermissionAssignedEvent(
            userId, 
            permissionId, 
            permissionName, 
            PermissionState.Grant, 
            reason);

        var auditLog = new PermissionAuditLog(
            userId, 
            null, 
            permissionId, 
            "Assigned", 
            null, 
            "Granted", 
            reason, 
            performedBy);

        _mockCurrentUserService
            .Setup(x => x.GetAuditIdentifier())
            .Returns(performedBy);

        _mockAuditService
            .Setup(x => x.LogUserPermissionAssignedAsync(
                userId, 
                permissionId, 
                performedBy, 
                reason, 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(auditLog);

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _mockCurrentUserService.Verify(x => x.GetAuditIdentifier(), Times.Once);
        _mockAuditService.Verify(x => x.LogUserPermissionAssignedAsync(
            userId, 
            permissionId, 
            performedBy, 
            reason, 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UserPermissionRemovedEvent_ShouldLogAuditEntry()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();
        var permissionName = "Users.Read";
        var reason = "User no longer needs access";
        var performedBy = "admin@test.com";

        var domainEvent = new UserPermissionRemovedEvent(
            userId, 
            permissionId, 
            permissionName, 
            PermissionState.Grant, 
            reason);

        var auditLog = new PermissionAuditLog(
            userId, 
            null, 
            permissionId, 
            "Removed", 
            "Granted", 
            null, 
            reason, 
            performedBy);

        _mockCurrentUserService
            .Setup(x => x.GetAuditIdentifier())
            .Returns(performedBy);

        _mockAuditService
            .Setup(x => x.LogUserPermissionRemovedAsync(
                userId, 
                permissionId, 
                performedBy, 
                reason, 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(auditLog);

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _mockCurrentUserService.Verify(x => x.GetAuditIdentifier(), Times.Once);
        _mockAuditService.Verify(x => x.LogUserPermissionRemovedAsync(
            userId, 
            permissionId, 
            performedBy, 
            reason, 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PermissionUpdatedEvent_ShouldLogAuditEntry()
    {
        // Arrange
        var permissionId = Guid.NewGuid();
        var performedBy = "admin@test.com";

        var domainEvent = new PermissionUpdatedEvent(
            permissionId,
            "Users", "Reports",
            "Read", "ReadWrite",
            "Users.Read", "Reports.ReadWrite",
            "Read user data", "Read and write report data",
            "User Management", "Reporting");

        var auditLog = new PermissionAuditLog(
            null, 
            null, 
            permissionId, 
            "Modified", 
            "Resource: Users, Action: Read, Description: Read user data", 
            "Resource: Reports, Action: ReadWrite, Description: Read and write report data", 
            "Permission details updated", 
            performedBy);

        _mockCurrentUserService
            .Setup(x => x.GetAuditIdentifier())
            .Returns(performedBy);

        _mockAuditService
            .Setup(x => x.LogPermissionModifiedAsync(
                permissionId,
                "Resource: Users, Action: Read, Description: Read user data",
                "Resource: Reports, Action: ReadWrite, Description: Read and write report data",
                performedBy,
                "Permission details updated",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(auditLog);

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _mockCurrentUserService.Verify(x => x.GetAuditIdentifier(), Times.Once);
        _mockAuditService.Verify(x => x.LogPermissionModifiedAsync(
            permissionId,
            "Resource: Users, Action: Read, Description: Read user data",
            "Resource: Reports, Action: ReadWrite, Description: Read and write report data",
            performedBy,
            "Permission details updated",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PermissionCreatedEvent_ShouldLogAuditEntry()
    {
        // Arrange
        var permissionId = Guid.NewGuid();
        var performedBy = "admin@test.com";

        var domainEvent = new PermissionCreatedEvent(
            permissionId,
            "Users",
            "Create",
            "Users.Create",
            "Create new users",
            "User Management",
            null);

        var auditLog = new PermissionAuditLog(
            null, 
            null, 
            permissionId, 
            "Modified", 
            string.Empty, 
            "Resource: Users, Action: Create, Description: Create new users", 
            "Permission created", 
            performedBy);

        _mockCurrentUserService
            .Setup(x => x.GetAuditIdentifier())
            .Returns(performedBy);

        _mockAuditService
            .Setup(x => x.LogPermissionModifiedAsync(
                permissionId,
                string.Empty,
                "Resource: Users, Action: Create, Description: Create new users",
                performedBy,
                "Permission created",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(auditLog);

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _mockCurrentUserService.Verify(x => x.GetAuditIdentifier(), Times.Once);
        _mockAuditService.Verify(x => x.LogPermissionModifiedAsync(
            permissionId,
            string.Empty,
            "Resource: Users, Action: Create, Description: Create new users",
            performedBy,
            "Permission created",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UserPermissionAssignedEvent_WhenAuditServiceThrows_ShouldNotRethrow()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();
        var domainEvent = new UserPermissionAssignedEvent(
            userId, 
            permissionId, 
            "Users.Read", 
            PermissionState.Grant, 
            null);

        _mockCurrentUserService
            .Setup(x => x.GetAuditIdentifier())
            .Returns("admin@test.com");

        _mockAuditService
            .Setup(x => x.LogUserPermissionAssignedAsync(
                It.IsAny<Guid>(), 
                It.IsAny<Guid>(), 
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Audit service error"));

        // Act & Assert
        var act = async () => await _handler.Handle(domainEvent, CancellationToken.None);
        await act.Should().NotThrowAsync();

        // Verify that the error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to log user permission assignment audit entry")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_PermissionUpdatedEvent_WhenAuditServiceThrows_ShouldNotRethrow()
    {
        // Arrange
        var permissionId = Guid.NewGuid();
        var domainEvent = new PermissionUpdatedEvent(
            permissionId,
            "Users", "Reports",
            "Read", "ReadWrite",
            "Users.Read", "Reports.ReadWrite",
            "Read user data", "Read and write report data",
            "User Management", "Reporting");

        _mockCurrentUserService
            .Setup(x => x.GetAuditIdentifier())
            .Returns("admin@test.com");

        _mockAuditService
            .Setup(x => x.LogPermissionModifiedAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Audit service error"));

        // Act & Assert
        var act = async () => await _handler.Handle(domainEvent, CancellationToken.None);
        await act.Should().NotThrowAsync();

        // Verify that the error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to log permission modification audit entry")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}