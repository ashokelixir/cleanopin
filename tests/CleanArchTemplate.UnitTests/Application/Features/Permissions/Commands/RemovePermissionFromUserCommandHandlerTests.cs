using FluentAssertions;
using MediatR;
using Moq;
using CleanArchTemplate.Application.Features.Permissions.Commands.RemovePermissionFromUser;
using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.Interfaces;
using CleanArchTemplate.Domain.Events;
using CleanArchTemplate.Domain.Enums;
using CleanArchTemplate.Application.Common.Interfaces;

namespace CleanArchTemplate.UnitTests.Application.Features.Permissions.Commands;

public class RemovePermissionFromUserCommandHandlerTests
{
    private readonly Mock<IUserPermissionRepository> _userPermissionRepositoryMock;
    private readonly Mock<IPermissionRepository> _permissionRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPublisher> _publisherMock;
    private readonly Mock<IPermissionCacheService> _cacheServiceMock;
    private readonly RemovePermissionFromUserCommandHandler _handler;

    public RemovePermissionFromUserCommandHandlerTests()
    {
        _userPermissionRepositoryMock = new Mock<IUserPermissionRepository>();
        _permissionRepositoryMock = new Mock<IPermissionRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _publisherMock = new Mock<IPublisher>();
        _cacheServiceMock = new Mock<IPermissionCacheService>();
        _handler = new RemovePermissionFromUserCommandHandler(
            _userPermissionRepositoryMock.Object,
            _permissionRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _publisherMock.Object,
            _cacheServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldRemovePermissionFromUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();
        var userPermission = UserPermission.Create(userId, permissionId, PermissionState.Grant, "Test assignment");
        var permission = Permission.Create("Users", "Create", "Create users", "UserManagement");
        var command = new RemovePermissionFromUserCommand
        {
            UserId = userId,
            PermissionId = permissionId,
            Reason = "No longer needed"
        };

        _userPermissionRepositoryMock.Setup(x => x.GetByUserAndPermissionAsync(userId, permissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userPermission);
        _permissionRepositoryMock.Setup(x => x.GetByIdAsync(permissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permission);
        _userPermissionRepositoryMock.Setup(x => x.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _userPermissionRepositoryMock.Verify(x => x.DeleteAsync(userPermission.Id, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cacheServiceMock.Verify(x => x.InvalidateUserPermissionsAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(x => x.Publish(It.IsAny<UserPermissionRemovedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UserPermissionNotFound_ShouldThrowException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();
        var command = new RemovePermissionFromUserCommand
        {
            UserId = userId,
            PermissionId = permissionId,
            Reason = "No longer needed"
        };

        _userPermissionRepositoryMock.Setup(x => x.GetByUserAndPermissionAsync(userId, permissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPermission?)null);

        // Act & Assert
        await _handler.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage($"User permission assignment not found for User ID {userId} and Permission ID {permissionId}");
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldPublishCorrectEvent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();
        var userPermission = UserPermission.Create(userId, permissionId, PermissionState.Deny, "Security restriction");
        var permission = Permission.Create("Users", "Delete", "Delete users", "UserManagement");
        var command = new RemovePermissionFromUserCommand
        {
            UserId = userId,
            PermissionId = permissionId,
            Reason = "Policy change"
        };

        UserPermissionRemovedEvent? publishedEvent = null;
        _publisherMock.Setup(x => x.Publish(It.IsAny<UserPermissionRemovedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<INotification, CancellationToken>((evt, _) => publishedEvent = evt as UserPermissionRemovedEvent);

        _userPermissionRepositoryMock.Setup(x => x.GetByUserAndPermissionAsync(userId, permissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userPermission);
        _permissionRepositoryMock.Setup(x => x.GetByIdAsync(permissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permission);
        _userPermissionRepositoryMock.Setup(x => x.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        publishedEvent.Should().NotBeNull();
        publishedEvent!.UserId.Should().Be(userId);
        publishedEvent.PermissionId.Should().Be(permissionId);
        publishedEvent.PermissionName.Should().Be("Users.Delete");
        publishedEvent.State.Should().Be(PermissionState.Deny);
        publishedEvent.Reason.Should().Be("Policy change");
    }

    [Fact]
    public async Task Handle_PermissionNotFound_ShouldUseUnknownPermissionName()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();
        var userPermission = UserPermission.Create(userId, permissionId, PermissionState.Grant, "Test assignment");
        var command = new RemovePermissionFromUserCommand
        {
            UserId = userId,
            PermissionId = permissionId
        };

        UserPermissionRemovedEvent? publishedEvent = null;
        _publisherMock.Setup(x => x.Publish(It.IsAny<UserPermissionRemovedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<INotification, CancellationToken>((evt, _) => publishedEvent = evt as UserPermissionRemovedEvent);

        _userPermissionRepositoryMock.Setup(x => x.GetByUserAndPermissionAsync(userId, permissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userPermission);
        _permissionRepositoryMock.Setup(x => x.GetByIdAsync(permissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Permission?)null);
        _userPermissionRepositoryMock.Setup(x => x.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        publishedEvent.Should().NotBeNull();
        publishedEvent!.PermissionName.Should().Be("Unknown");
    }
}

