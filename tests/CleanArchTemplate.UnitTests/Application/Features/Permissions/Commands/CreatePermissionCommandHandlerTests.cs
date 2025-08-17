using FluentAssertions;
using MediatR;
using Moq;
using CleanArchTemplate.Application.Features.Permissions.Commands.CreatePermission;
using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.Interfaces;
using CleanArchTemplate.Domain.Events;
using CleanArchTemplate.Application.Common.Interfaces;

namespace CleanArchTemplate.UnitTests.Application.Features.Permissions.Commands;

public class CreatePermissionCommandHandlerTests
{
    private readonly Mock<IPermissionRepository> _permissionRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPublisher> _publisherMock;
    private readonly CreatePermissionCommandHandler _handler;

    public CreatePermissionCommandHandlerTests()
    {
        _permissionRepositoryMock = new Mock<IPermissionRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _publisherMock = new Mock<IPublisher>();
        _handler = new CreatePermissionCommandHandler(
            _permissionRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _publisherMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreatePermissionAndReturnId()
    {
        // Arrange
        var command = new CreatePermissionCommand
        {
            Resource = "Users",
            Action = "Create",
            Description = "Create new users",
            Category = "UserManagement"
        };

        _permissionRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Permission>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Permission p, CancellationToken ct) => p);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _permissionRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Permission>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(x => x.Publish(It.IsAny<PermissionCreatedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCommandWithParent_ShouldCreatePermissionWithParent()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var command = new CreatePermissionCommand
        {
            Resource = "Users",
            Action = "Create",
            Description = "Create new users",
            Category = "UserManagement",
            ParentPermissionId = parentId
        };

        _permissionRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Permission>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Permission p, CancellationToken ct) => p);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _permissionRepositoryMock.Verify(x => x.AddAsync(
            It.Is<Permission>(p => p.ParentPermissionId == parentId), 
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(x => x.Publish(It.IsAny<PermissionCreatedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldPublishCorrectEvent()
    {
        // Arrange
        var command = new CreatePermissionCommand
        {
            Resource = "Users",
            Action = "Create",
            Description = "Create new users",
            Category = "UserManagement"
        };

        PermissionCreatedEvent? publishedEvent = null;
        _publisherMock.Setup(x => x.Publish(It.IsAny<PermissionCreatedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<INotification, CancellationToken>((evt, _) => publishedEvent = evt as PermissionCreatedEvent);

        _permissionRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Permission>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Permission p, CancellationToken ct) => p);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        publishedEvent.Should().NotBeNull();
        publishedEvent!.PermissionId.Should().Be(result);
        publishedEvent.Name.Should().Be("Users.Create");
    }
}