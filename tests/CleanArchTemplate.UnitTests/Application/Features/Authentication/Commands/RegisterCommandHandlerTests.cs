using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Application.Features.Authentication.Commands.Register;
using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.Events;
using CleanArchTemplate.Domain.Interfaces;
using CleanArchTemplate.Domain.ValueObjects;
using CleanArchTemplate.TestUtilities.Common;
using FluentAssertions;
using Moq;
using Xunit;

namespace CleanArchTemplate.UnitTests.Application.Features.Authentication.Commands;

public class RegisterCommandHandlerTests : BaseTest
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly RegisterCommandHandler _handler;

    public RegisterCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _userRepositoryMock = new Mock<IUserRepository>();
        
        _unitOfWorkMock.Setup(x => x.Users).Returns(_userRepositoryMock.Object);
        
        _handler = new RegisterCommandHandler(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldCreateUserWithDomainEvent_WhenValidRequest()
    {
        // Arrange
        var command = new RegisterCommand(
            "test@example.com",
            "Password123!",
            "John",
            "Doe"
        );

        var email = Email.Create(command.Email);
        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        User? capturedUser = null;
        _userRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => capturedUser = user)
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Email.Should().Be(command.Email);
        result.Value.FullName.Should().Be($"{command.FirstName} {command.LastName}");

        // Verify user was created with domain event
        capturedUser.Should().NotBeNull();
        capturedUser!.Email.Value.Should().Be(command.Email);
        capturedUser.FirstName.Should().Be(command.FirstName);
        capturedUser.LastName.Should().Be(command.LastName);
        
        // Verify domain event was added
        capturedUser.DomainEvents.Should().HaveCount(1);
        capturedUser.DomainEvents.First().Should().BeOfType<UserCreatedEvent>();
        
        var userCreatedEvent = (UserCreatedEvent)capturedUser.DomainEvents.First();
        userCreatedEvent.UserId.Should().Be(capturedUser.Id);
        userCreatedEvent.Email.Should().Be(command.Email);
        userCreatedEvent.FullName.Should().Be($"{command.FirstName} {command.LastName}");

        // Verify repository calls
        _userRepositoryMock.Verify(x => x.GetByEmailAsync(email, It.IsAny<CancellationToken>()), Times.Once);
        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnBadRequest_WhenUserAlreadyExists()
    {
        // Arrange
        var command = new RegisterCommand(
            "existing@example.com",
            "Password123!",
            "John",
            "Doe"
        );

        var email = Email.Create(command.Email);
        var existingUser = User.Create(email, "Existing", "User", "hashedpassword");
        
        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("User with this email already exists");

        // Verify no user was added
        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}