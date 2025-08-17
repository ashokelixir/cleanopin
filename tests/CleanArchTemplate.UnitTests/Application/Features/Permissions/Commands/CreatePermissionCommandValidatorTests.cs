using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;
using CleanArchTemplate.Application.Features.Permissions.Commands.CreatePermission;
using CleanArchTemplate.Domain.Interfaces;
using CleanArchTemplate.Domain.Entities;

namespace CleanArchTemplate.UnitTests.Application.Features.Permissions.Commands;

public class CreatePermissionCommandValidatorTests
{
    private readonly Mock<IPermissionRepository> _permissionRepositoryMock;
    private readonly CreatePermissionCommandValidator _validator;

    public CreatePermissionCommandValidatorTests()
    {
        _permissionRepositoryMock = new Mock<IPermissionRepository>();
        _validator = new CreatePermissionCommandValidator(_permissionRepositoryMock.Object);
    }

    [Fact]
    public async Task Validate_ValidCommand_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var command = new CreatePermissionCommand
        {
            Resource = "Users",
            Action = "Create",
            Description = "Create new users",
            Category = "UserManagement"
        };

        _permissionRepositoryMock.Setup(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("", "Resource is required")]
    [InlineData(null, "Resource is required")]
    public async Task Validate_InvalidResource_ShouldHaveValidationError(string? resource, string expectedMessage)
    {
        // Arrange
        var command = new CreatePermissionCommand
        {
            Resource = resource!,
            Action = "Create",
            Description = "Create new users",
            Category = "UserManagement"
        };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Resource)
            .WithErrorMessage(expectedMessage);
    }

    [Theory]
    [InlineData("", "Action is required")]
    [InlineData(null, "Action is required")]
    public async Task Validate_InvalidAction_ShouldHaveValidationError(string? action, string expectedMessage)
    {
        // Arrange
        var command = new CreatePermissionCommand
        {
            Resource = "Users",
            Action = action!,
            Description = "Create new users",
            Category = "UserManagement"
        };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Action)
            .WithErrorMessage(expectedMessage);
    }

    [Fact]
    public async Task Validate_ResourceTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreatePermissionCommand
        {
            Resource = new string('A', 101), // 101 characters
            Action = "Create",
            Description = "Create new users",
            Category = "UserManagement"
        };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Resource)
            .WithErrorMessage("Resource must not exceed 100 characters");
    }

    [Fact]
    public async Task Validate_ActionTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreatePermissionCommand
        {
            Resource = "Users",
            Action = new string('A', 101), // 101 characters
            Description = "Create new users",
            Category = "UserManagement"
        };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Action)
            .WithErrorMessage("Action must not exceed 100 characters");
    }

    [Fact]
    public async Task Validate_DescriptionTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreatePermissionCommand
        {
            Resource = "Users",
            Action = "Create",
            Description = new string('A', 501), // 501 characters
            Category = "UserManagement"
        };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description must not exceed 500 characters");
    }

    [Theory]
    [InlineData("123Invalid", "Resource must start with a letter and contain only letters, numbers, and underscores")]
    [InlineData("Invalid-Resource", "Resource must start with a letter and contain only letters, numbers, and underscores")]
    [InlineData("Invalid Resource", "Resource must start with a letter and contain only letters, numbers, and underscores")]
    public async Task Validate_InvalidResourceFormat_ShouldHaveValidationError(string resource, string expectedMessage)
    {
        // Arrange
        var command = new CreatePermissionCommand
        {
            Resource = resource,
            Action = "Create",
            Description = "Create new users",
            Category = "UserManagement"
        };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Resource)
            .WithErrorMessage(expectedMessage);
    }

    [Fact]
    public async Task Validate_DuplicatePermission_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreatePermissionCommand
        {
            Resource = "Users",
            Action = "Create",
            Description = "Create new users",
            Category = "UserManagement"
        };

        _permissionRepositoryMock.Setup(x => x.ExistsAsync("Users", "Create", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("A permission with this resource and action combination already exists");
    }

    [Fact]
    public async Task Validate_InvalidParentPermission_ShouldHaveValidationError()
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

        _permissionRepositoryMock.Setup(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _permissionRepositoryMock.Setup(x => x.GetByIdAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Permission?)null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ParentPermissionId)
            .WithErrorMessage("Parent permission does not exist");
    }
}