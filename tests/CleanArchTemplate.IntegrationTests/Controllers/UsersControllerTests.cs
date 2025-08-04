using CleanArchTemplate.Application.Features.Users.Commands.CreateUser;
using CleanArchTemplate.Application.Common.Models;
using CleanArchTemplate.TestUtilities.Common;
using System.Net;
using System.Net.Http.Json;
using Xunit.Abstractions;

namespace CleanArchTemplate.IntegrationTests.Controllers;

public class UsersControllerTests : BaseIntegrationTest
{
    public UsersControllerTests(ITestOutputHelper output) : base(output)
    {
    }
    [Fact]
    public async Task CreateUser_WithValidData_ShouldReturnCreatedUser()
    {
        // Arrange
        await ResetDatabaseAsync();
        
        var command = new CreateUserCommand(
            Email: "test@example.com",
            FirstName: "Test",
            LastName: "User",
            Password: "SecurePassword123!"
        );

        // Act
        var response = await Client.PostAsJsonAsync("/api/users", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var userDto = await response.Content.ReadFromJsonAsync<UserDto>();
        userDto.Should().NotBeNull();
        userDto!.Email.Should().Be(command.Email);
        userDto.FirstName.Should().Be(command.FirstName);
        userDto.LastName.Should().Be(command.LastName);
        userDto.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateUser_WithDuplicateEmail_ShouldReturnConflict()
    {
        // Arrange
        await ResetDatabaseAsync();
        
        var command = new CreateUserCommand(
            Email: "duplicate@example.com",
            FirstName: "Duplicate",
            LastName: "User",
            Password: "SecurePassword123!"
        );

        // Create user first time
        await Client.PostAsJsonAsync("/api/users", command);

        // Act - Try to create the same user again
        var response = await Client.PostAsJsonAsync("/api/users", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateUser_WithInvalidEmail_ShouldReturnValidationError()
    {
        // Arrange
        await ResetDatabaseAsync();
        
        var command = new CreateUserCommand(
            Email: "invalid-email",
            FirstName: "Invalid",
            LastName: "User",
            Password: "SecurePassword123!"
        );

        // Act
        var response = await Client.PostAsJsonAsync("/api/users", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GetUserById_WithValidId_ShouldReturnUser()
    {
        // Arrange
        await ResetDatabaseAsync();
        
        var createCommand = new CreateUserCommand(
            Email: "gettest@example.com",
            FirstName: "Get",
            LastName: "Test",
            Password: "SecurePassword123!"
        );

        var createResponse = await Client.PostAsJsonAsync("/api/users", createCommand);
        var createdUser = await createResponse.Content.ReadFromJsonAsync<UserDto>();

        // Act
        var response = await Client.GetAsync($"/api/users/{createdUser!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var userDto = await response.Content.ReadFromJsonAsync<UserDto>();
        userDto.Should().NotBeNull();
        userDto!.Id.Should().Be(createdUser.Id);
        userDto.Email.Should().Be(createCommand.Email);
    }

    [Fact]
    public async Task GetUserById_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        await ResetDatabaseAsync();
        var invalidId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/users/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAllUsers_ShouldReturnUsersList()
    {
        // Arrange
        await ResetDatabaseAsync();
        
        var user1Command = new CreateUserCommand(
            Email: "user1@example.com",
            FirstName: "User",
            LastName: "One",
            Password: "SecurePassword123!"
        );
        
        var user2Command = new CreateUserCommand(
            Email: "user2@example.com",
            FirstName: "User",
            LastName: "Two",
            Password: "SecurePassword123!"
        );

        await Client.PostAsJsonAsync("/api/users", user1Command);
        await Client.PostAsJsonAsync("/api/users", user2Command);

        // Act
        var response = await Client.GetAsync("/api/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var users = await response.Content.ReadFromJsonAsync<List<UserDto>>();
        users.Should().NotBeNull();
        users!.Should().HaveCount(2);
        users.Should().Contain(u => u.Email == user1Command.Email);
        users.Should().Contain(u => u.Email == user2Command.Email);
    }
}