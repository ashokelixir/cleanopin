using FluentAssertions;
using MediatR;
using Moq;
using CleanArchTemplate.Application.Features.Permissions.Commands.AssignPermissionToUser;
using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.Interfaces;
using CleanArchTemplate.Domain.Events;
using CleanArchTemplate.Domain.Enums;
using CleanArchTemplate.Application.Common.Interfaces;

namespace CleanArchTemplate.UnitTests.Application.Features.Permissions.Commands;

public class AssignPermissionToUserCommandHandlerTests
{
    private readonly Mock<IUserPermissionRepository> _userPermissionRepositoryMock;
    private readonly Mock<IPermissionRepository> _permissionRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPublisher> _publisherMock;
    private readonly Mock<IPermissionCacheService> _cacheServiceMock;
    private readonly AssignPermissionToUserCommandHandler _handler;

    public AssignPermissionToUserCommandHandlerTests()
    {
        _userPermissionRepositoryMock = new Mock<IUserPermissionRepository>();
        _permissionRepositoryMock = new Mock<IPermissionRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _publisherMock = new Mock<IPublisher>();
        _cacheServiceMock = new Mock<IPermissionCacheService>();
        _handler = new AssignPermissionToUserCommandHandler(
            _userPermissionRepositoryMock.Object,
            _permissionRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _publisherMock.Object,
            _cacheServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldAssignPermissionToUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();
        var permission = Permission.Create("Users", "Create", "Create users", "UserManagement");
        var command = new AssignPermissionToUserCommand
        {
            UserId = userId,
            PermissionId = permissionId,
            State = PermissionState.Grant,
            Reason = "Test assignment"
        };

        _permissionRepositoryMock.Setup(x => x.GetByIdAsync(permissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permission);
        _userPermissionRepositoryMock.Setup(x => x.AddAsync(It.IsAny<UserPermission>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPermission up, CancellationToken ct) => up);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _userPermissionRepositoryMock.Verify(x => x.AddAsync(It.IsAny<UserPermission>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cacheServiceMock.Verify(x => x.InvalidateUserPermissionsAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(x => x.Publish(It.IsAny<UserPermissionAssignedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PermissionNotFound_ShouldThrowException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();
        var command = new AssignPermissionToUserCommand
        {
            UserId = userId,
            PermissionId = permissionId,
            State = PermissionState.Grant
        };

        _permissionRepositoryMock.Setup(x => x.GetByIdAsync(permissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Permission?)null);

        // Act & Assert
        await _handler.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage($"Permission with ID {permissionId} not found");
    }

    [Fact]
    public async Task Handle_ValidCommandWithExpiration_ShouldAssignPermissionWithExpiration()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();
        var expiresAt = DateTime.UtcNow.AddDays(30);
        var permission = Permission.Create("Users", "Create", "Create users", "UserManagement");
        var command = new AssignPermissionToUserCommand
        {
            UserId = userId,
            PermissionId = permissionId,
            State = PermissionState.Grant,
            Reason = "Temporary access",
            ExpiresAt = expiresAt
        };

        _permissionRepositoryMock.Setup(x => x.GetByIdAsync(permissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permission);
        _userPermissionRepositoryMock.Setup(x => x.AddAsync(It.IsAny<UserPermission>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPermission up, CancellationToken ct) => up);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _userPermissionRepositoryMock.Verify(x => x.AddAsync(
            It.Is<UserPermission>(up => up.ExpiresAt == expiresAt), 
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cacheServiceMock.Verify(x => x.InvalidateUserPermissionsAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DenyPermission_ShouldAssignDenyPermission()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();
        var permission = Permission.Create("Users", "Delete", "Delete users", "UserManagement");
        var command = new AssignPermissionToUserCommand
        {
            UserId = userId,
            PermissionId = permissionId,
            State = PermissionState.Deny,
            Reason = "Security restriction"
        };

        _permissionRepositoryMock.Setup(x => x.GetByIdAsync(permissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permission);
        _userPermissionRepositoryMock.Setup(x => x.AddAsync(It.IsAny<UserPermission>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPermission up, CancellationToken ct) => up);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _userPermissionRepositoryMock.Verify(x => x.AddAsync(
            It.Is<UserPermission>(up => up.State == PermissionState.Deny), 
            It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(x => x.Publish(
            It.Is<UserPermissionAssignedEvent>(e => e.State == PermissionState.Deny), 
            It.IsAny<CancellationToken>()), Times.Once);
    }
}

