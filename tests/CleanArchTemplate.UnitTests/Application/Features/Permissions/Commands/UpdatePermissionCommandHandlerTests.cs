using FluentAssertions;
using MediatR;
using Moq;
using CleanArchTemplate.Application.Features.Permissions.Commands.UpdatePermission;
using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.Interfaces;
using CleanArchTemplate.Domain.Events;
using CleanArchTemplate.Domain.Exceptions;
using CleanArchTemplate.Application.Common.Interfaces;

namespace CleanArchTemplate.UnitTests.Application.Features.Permissions.Commands;

public class UpdatePermissionCommandHandlerTests
{
    private readonly Mock<IPermissionRepository> _permissionRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPublisher> _publisherMock;
    private readonly UpdatePermissionCommandHandler _handler;

    public UpdatePermissionCommandHandlerTests()
    {
        _permissionRepositoryMock = new Mock<IPermissionRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _publisherMock = new Mock<IPublisher>();
        _handler = new UpdatePermissionCommandHandler(
            _permissionRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _publisherMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldUpdatePermission()
    {
        // Arrange
        var permissionId = Guid.NewGuid();
        var permission = Permission.Create("Users", "Create", "Old description", "OldCategory");
        var command = new UpdatePermissionCommand
        {
            Id = permissionId,
            Description = "New description",
            Category = "NewCategory",
            IsActive = true
        };

        _permissionRepositoryMock.Setup(x => x.GetByIdAsync(permissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permission);
        _permissionRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Permission>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _permissionRepositoryMock.Verify(x => x.UpdateAsync(permission, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(x => x.Publish(It.IsAny<PermissionUpdatedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PermissionNotFound_ShouldThrowException()
    {
        // Arrange
        var permissionId = Guid.NewGuid();
        var command = new UpdatePermissionCommand
        {
            Id = permissionId,
            Description = "New description",
            Category = "NewCategory",
            IsActive = true
        };

        _permissionRepositoryMock.Setup(x => x.GetByIdAsync(permissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Permission?)null);

        // Act & Assert
        await _handler.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<PermissionNotFoundException>()
            .WithMessage($"Permission with ID {permissionId}");
    }

    [Fact]
    public async Task Handle_ValidCommandWithParent_ShouldUpdatePermissionWithParent()
    {
        // Arrange
        var permissionId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var permission = Permission.Create("Users", "Create", "Old description", "OldCategory");
        var command = new UpdatePermissionCommand
        {
            Id = permissionId,
            Description = "New description",
            Category = "NewCategory",
            ParentPermissionId = parentId,
            IsActive = true
        };

        _permissionRepositoryMock.Setup(x => x.GetByIdAsync(permissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permission);
        _permissionRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Permission>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _permissionRepositoryMock.Verify(x => x.UpdateAsync(permission, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(x => x.Publish(It.IsAny<PermissionUpdatedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}