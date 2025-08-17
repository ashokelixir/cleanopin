using AutoMapper;
using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Application.Common.Messages;
using CleanArchTemplate.Application.Common.Models;
using CleanArchTemplate.Application.Features.Users.Commands.CreateUser;
using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.Interfaces;
using CleanArchTemplate.Domain.ValueObjects;
using CleanArchTemplate.TestUtilities.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CleanArchTemplate.UnitTests.Application.Features.Users.Commands;

public class CreateUserCommandHandlerTests : BaseTest
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IPasswordService> _mockPasswordService;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly Mock<IMessagePublisher> _mockMessagePublisher;
    private readonly Mock<ILogger<CreateUserCommandHandler>> _mockLogger;
    private readonly CreateUserCommandHandler _handler;

    public CreateUserCommandHandlerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockPasswordService = new Mock<IPasswordService>();
        _mockAuditLogService = new Mock<IAuditLogService>();
        _mockMessagePublisher = new Mock<IMessagePublisher>();
        _mockLogger = new Mock<ILogger<CreateUserCommandHandler>>();

        _mockUnitOfWork.Setup(x => x.Users).Returns(_mockUserRepository.Object);

        _handler = new CreateUserCommandHandler(
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockPasswordService.Object,
            _mockAuditLogService.Object,
            _mockMessagePublisher.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateUser()
    {
        // Arrange
        var command = new CreateUserCommand(
            Email: "test@example.com",
            FirstName: "John",
            LastName: "Doe",
            Password: "SecurePassword123!"
        );

        var hashedPassword = "hashed_password";
        var email = Email.Create(command.Email)!;
        var user = User.Create(email, command.FirstName, command.LastName, hashedPassword);
        var userDto = new UserDto
        {
            Id = user.Id,
            Email = command.Email,
            FirstName = command.FirstName,
            LastName = command.LastName,
            IsActive = true,
            IsEmailVerified = false
        };

        _mockPasswordService
            .Setup(x => x.HashPassword(command.Password))
            .Returns(hashedPassword);

        _mockUserRepository
            .Setup(x => x.IsEmailExistsAsync(It.IsAny<Email>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockUserRepository
            .Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockMapper
            .Setup(x => x.Map<UserDto>(It.IsAny<User>()))
            .Returns(userDto);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Email.Should().Be(command.Email);
        result.Value!.FirstName.Should().Be(command.FirstName);
        result.Value!.LastName.Should().Be(command.LastName);

        _mockPasswordService.Verify(x => x.HashPassword(command.Password), Times.Once);
        _mockUserRepository.Verify(x => x.IsEmailExistsAsync(It.IsAny<Email>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUserRepository.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(x => x.Map<UserDto>(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithExistingEmail_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateUserCommand(
            Email: "existing@example.com",
            FirstName: "John",
            LastName: "Doe",
            Password: "SecurePassword123!"
        );

        _mockUserRepository
            .Setup(x => x.IsEmailExistsAsync(It.IsAny<Email>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Message.Should().Contain("already exists");

        _mockUserRepository.Verify(x => x.IsEmailExistsAsync(It.IsAny<Email>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUserRepository.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithInvalidEmail_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateUserCommand(
            Email: "invalid-email",
            FirstName: "John",
            LastName: "Doe",
            Password: "SecurePassword123!"
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Invalid email address format");

        _mockUserRepository.Verify(x => x.IsEmailExistsAsync(It.IsAny<Email>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUserRepository.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Handle_WithInvalidFirstName_ShouldReturnFailure(string firstName)
    {
        // Arrange
        var command = new CreateUserCommand(
            Email: "test@example.com",
            FirstName: firstName,
            LastName: "Doe",
            Password: "SecurePassword123!"
        );

        var hashedPassword = "hashed_password";

        _mockPasswordService
            .Setup(x => x.HashPassword(command.Password))
            .Returns(hashedPassword);

        _mockUserRepository
            .Setup(x => x.IsEmailExistsAsync(It.IsAny<Email>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Handle_WithInvalidLastName_ShouldReturnFailure(string lastName)
    {
        // Arrange
        var command = new CreateUserCommand(
            Email: "test@example.com",
            FirstName: "John",
            LastName: lastName,
            Password: "SecurePassword123!"
        );

        var hashedPassword = "hashed_password";

        _mockPasswordService
            .Setup(x => x.HashPassword(command.Password))
            .Returns(hashedPassword);

        _mockUserRepository
            .Setup(x => x.IsEmailExistsAsync(It.IsAny<Email>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldHashPassword()
    {
        // Arrange
        var command = new CreateUserCommand(
            Email: "test@example.com",
            FirstName: "John",
            LastName: "Doe",
            Password: "SecurePassword123!"
        );

        var hashedPassword = "hashed_password_123";
        var userDto = new UserDto
        {
            Id = Guid.NewGuid(),
            Email = command.Email,
            FirstName = command.FirstName,
            LastName = command.LastName,
            IsActive = true,
            IsEmailVerified = false
        };

        _mockPasswordService
            .Setup(x => x.HashPassword(command.Password))
            .Returns(hashedPassword);

        _mockUserRepository
            .Setup(x => x.IsEmailExistsAsync(It.IsAny<Email>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockUserRepository
            .Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockMapper
            .Setup(x => x.Map<UserDto>(It.IsAny<User>()))
            .Returns(userDto);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockPasswordService.Verify(x => x.HashPassword(command.Password), Times.Once);
    }
}
