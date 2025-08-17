using FluentAssertions;
using MediatR;
using Moq;
using CleanArchTemplate.Application.Features.Permissions.Commands.BulkAssignPermissions;
using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.Interfaces;
using CleanArchTemplate.Domain.Events;
using CleanArchTemplate.Domain.Enums;
using CleanArchTemplate.Application.Common.Interfaces;

namespace CleanArchTemplate.UnitTests.Application.Features.Permissions.Commands;

public class BulkAssignPermissionsCommandHandlerTests
{
    private readonly Mock<IUserPermissionRepository> _userPermissionRepositoryMock;
    private readonly Mock<IRolePermissionRepository> _rolePermissionRepositoryMock;
    private readonly Mock<IPermissionRepository> _permissionRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPublisher> _publisherMock;
    private readonly Mock<IPermissionCacheService> _cacheServiceMock;
    private readonly BulkAssignPermissionsCommandHandler _handler;

    public BulkAssignPermissionsCommandHandlerTests()
    {
        _userPermissionRepositoryMock = new Mock<IUserPermissionRepository>();
        _rolePermissionRepositoryMock = new Mock<IRolePermissionRepository>();
        _permissionRepositoryMock = new Mock<IPermissionRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _publisherMock = new Mock<IPublisher>();
        _cacheServiceMock = new Mock<IPermissionCacheService>();
        _handler = new BulkAssignPermissionsCommandHandler(
            _userPermissionRepositoryMock.Object,
            _rolePermissionRepositoryMock.Object,
            _permissionRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _publisherMock.Object,
            _cacheServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ValidUserPermissions_ShouldAssignAllPermissions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permission1Id = Guid.NewGuid();
        var permission2Id = Guid.NewGuid();
        var permission1 = Permission.Create("Users", "Create", "Create users", "UserManagement");
        var permission2 = Permission.Create("Users", "Read", "Read users", "UserManagement");

        var command = new BulkAssignPermissionsCommand
        {
            UserId = userId,
            Permissions = new[]
            {
                new PermissionAssignmentDto { PermissionId = permission1Id, State = PermissionState.Grant },
                new PermissionAssignmentDto { PermissionId = permission2Id, State = PermissionState.Grant }
            },
            Reason = "Bulk assignment test"
        };

        _permissionRepositoryMock.Setup(x => x.GetByIdAsync(permission1Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permission1);
        _permissionRepositoryMock.Setup(x => x.GetByIdAsync(permission2Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permission2);
        _userPermissionRepositoryMock.Setup(x => x.GetByUserAndPermissionAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPermission?)null);
        _userPermissionRepositoryMock.Setup(x => x.AddAsync(It.IsAny<UserPermission>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPermission up, CancellationToken ct) => up);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.SuccessfulAssignments.Should().Be(2);
        result.FailedAssignments.Should().Be(0);
        result.Errors.Should().BeEmpty();
        _userPermissionRepositoryMock.Verify(x => x.AddAsync(It.IsAny<UserPermission>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cacheServiceMock.Verify(x => x.InvalidateUserPermissionsAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(x => x.Publish(It.IsAny<UserPermissionAssignedEvent>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_ValidRolePermissions_ShouldAssignAllPermissions()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var permission1Id = Guid.NewGuid();
        var permission2Id = Guid.NewGuid();
        var permission1 = Permission.Create("Users", "Create", "Create users", "UserManagement");
        var permission2 = Permission.Create("Users", "Read", "Read users", "UserManagement");

        var command = new BulkAssignPermissionsCommand
        {
            RoleId = roleId,
            Permissions = new[]
            {
                new PermissionAssignmentDto { PermissionId = permission1Id, State = PermissionState.Grant },
                new PermissionAssignmentDto { PermissionId = permission2Id, State = PermissionState.Grant }
            },
            Reason = "Bulk role assignment test"
        };

        _permissionRepositoryMock.Setup(x => x.GetByIdAsync(permission1Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permission1);
        _permissionRepositoryMock.Setup(x => x.GetByIdAsync(permission2Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permission2);
        _rolePermissionRepositoryMock.Setup(x => x.GetByRoleAndPermissionAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RolePermission?)null);
        _rolePermissionRepositoryMock.Setup(x => x.AddAsync(It.IsAny<RolePermission>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RolePermission rp, CancellationToken ct) => rp);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.SuccessfulAssignments.Should().Be(2);
        result.FailedAssignments.Should().Be(0);
        result.Errors.Should().BeEmpty();
        _rolePermissionRepositoryMock.Verify(x => x.AddAsync(It.IsAny<RolePermission>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cacheServiceMock.Verify(x => x.InvalidateRolePermissionsAsync(roleId, It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(x => x.Publish(It.IsAny<RolePermissionAssignedEvent>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_SomePermissionsNotFound_ShouldReturnPartialSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permission1Id = Guid.NewGuid();
        var permission2Id = Guid.NewGuid();
        var permission1 = Permission.Create("Users", "Create", "Create users", "UserManagement");

        var command = new BulkAssignPermissionsCommand
        {
            UserId = userId,
            Permissions = new[]
            {
                new PermissionAssignmentDto { PermissionId = permission1Id, State = PermissionState.Grant },
                new PermissionAssignmentDto { PermissionId = permission2Id, State = PermissionState.Grant }
            }
        };

        _permissionRepositoryMock.Setup(x => x.GetByIdAsync(permission1Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permission1);
        _permissionRepositoryMock.Setup(x => x.GetByIdAsync(permission2Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Permission?)null);
        _userPermissionRepositoryMock.Setup(x => x.GetByUserAndPermissionAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPermission?)null);
        _userPermissionRepositoryMock.Setup(x => x.AddAsync(It.IsAny<UserPermission>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPermission up, CancellationToken ct) => up);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.SuccessfulAssignments.Should().Be(1);
        result.FailedAssignments.Should().Be(1);
        result.Errors.Should().HaveCount(1);
        result.Errors.First().Should().Contain($"Permission with ID {permission2Id} not found");
        _userPermissionRepositoryMock.Verify(x => x.AddAsync(It.IsAny<UserPermission>(), It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(x => x.Publish(It.IsAny<UserPermissionAssignedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateUserPermission_ShouldReturnError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();
        var permission = Permission.Create("Users", "Create", "Create users", "UserManagement");
        var existingUserPermission = UserPermission.Create(userId, permissionId, PermissionState.Grant);

        var command = new BulkAssignPermissionsCommand
        {
            UserId = userId,
            Permissions = new[]
            {
                new PermissionAssignmentDto { PermissionId = permissionId, State = PermissionState.Grant }
            }
        };

        _permissionRepositoryMock.Setup(x => x.GetByIdAsync(permissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permission);
        _userPermissionRepositoryMock.Setup(x => x.GetByUserAndPermissionAsync(userId, permissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUserPermission);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.SuccessfulAssignments.Should().Be(0);
        result.FailedAssignments.Should().Be(1);
        result.Errors.Should().HaveCount(1);
        result.Errors.First().Should().Contain($"User already has permission {permissionId} assigned");
        _userPermissionRepositoryMock.Verify(x => x.AddAsync(It.IsAny<UserPermission>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithExpirationDates_ShouldAssignPermissionsWithExpiration()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();
        var permission = Permission.Create("Users", "Create", "Create users", "UserManagement");
        var expiresAt = DateTime.UtcNow.AddDays(30);

        var command = new BulkAssignPermissionsCommand
        {
            UserId = userId,
            Permissions = new[]
            {
                new PermissionAssignmentDto 
                { 
                    PermissionId = permissionId, 
                    State = PermissionState.Grant,
                    ExpiresAt = expiresAt
                }
            }
        };

        _permissionRepositoryMock.Setup(x => x.GetByIdAsync(permissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permission);
        _userPermissionRepositoryMock.Setup(x => x.GetByUserAndPermissionAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPermission?)null);
        _userPermissionRepositoryMock.Setup(x => x.AddAsync(It.IsAny<UserPermission>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPermission up, CancellationToken ct) => up);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.SuccessfulAssignments.Should().Be(1);
        result.FailedAssignments.Should().Be(0);
        _userPermissionRepositoryMock.Verify(x => x.AddAsync(
            It.Is<UserPermission>(up => up.ExpiresAt == expiresAt), 
            It.IsAny<CancellationToken>()), Times.Once);
    }
}

