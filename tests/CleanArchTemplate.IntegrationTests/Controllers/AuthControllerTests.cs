using CleanArchTemplate.Application.Features.Authentication.Commands.Login;
using CleanArchTemplate.Application.Features.Users.Commands.CreateUser;
using CleanArchTemplate.Application.Common.Models;
using CleanArchTemplate.TestUtilities.Common;
using System.Net;
using System.Net.Http.Json;
using Xunit.Abstractions;

namespace CleanArchTemplate.IntegrationTests.Controllers;

public class AuthControllerTests : BaseIntegrationTest
{
    public AuthControllerTests(ITestOutputHelper output) : base(output)
    {
    }
    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnToken()
    {
        // Arrange
        await ResetDatabaseAsync();
        
        var createUserCommand = new CreateUserCommand(
            Email: "test@example.com",
            FirstName: "Test",
            LastName: "User",
            Password: "SecurePassword123!"
        );

        await Client.PostAsJsonAsync("/api/users", createUserCommand);

        var loginCommand = new LoginCommand(
            Email: "test@example.com",
            Password: "SecurePassword123!"
        );

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var tokenResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        tokenResponse.Should().NotBeNull();
        tokenResponse!.AccessToken.Should().NotBeNullOrEmpty();
        tokenResponse.RefreshToken.Should().NotBeNullOrEmpty();
        tokenResponse.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
    {
        // Arrange
        await ResetDatabaseAsync();
        
        var loginCommand = new LoginCommand(
            Email: "nonexistent@example.com",
            Password: "WrongPassword123!"
        );

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithInvalidEmail_ShouldReturnValidationError()
    {
        // Arrange
        await ResetDatabaseAsync();
        
        var loginCommand = new LoginCommand(
            Email: "invalid-email",
            Password: "SecurePassword123!"
        );

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Login_WithEmptyPassword_ShouldReturnValidationError(string password)
    {
        // Arrange
        await ResetDatabaseAsync();
        
        var loginCommand = new LoginCommand(
            Email: "test@example.com",
            Password: password
        );

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }
}