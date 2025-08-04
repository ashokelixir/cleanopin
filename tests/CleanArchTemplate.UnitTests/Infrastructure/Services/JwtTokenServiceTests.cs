using CleanArchTemplate.Infrastructure.Services;
using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.ValueObjects;
using CleanArchTemplate.TestUtilities.Common;
using CleanArchTemplate.TestUtilities.Builders;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Moq;
using Xunit;

namespace CleanArchTemplate.UnitTests.Infrastructure.Services;

public class JwtTokenServiceTests : BaseTest
{
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly JwtTokenService _jwtTokenService;

    public JwtTokenServiceTests()
    {
        _configurationMock = new Mock<IConfiguration>();

        // Setup configuration
        _configurationMock.Setup(x => x["Jwt:SecretKey"]).Returns("this-is-a-very-long-secret-key-for-jwt-token-generation-that-is-at-least-256-bits");
        _configurationMock.Setup(x => x["Jwt:Issuer"]).Returns("CleanArchTemplate");
        _configurationMock.Setup(x => x["Jwt:Audience"]).Returns("CleanArchTemplate");
        _configurationMock.Setup(x => x["Jwt:AccessTokenExpirationMinutes"]).Returns("60");

        _jwtTokenService = new JwtTokenService(_configurationMock.Object);
    }

    [Fact]
    public async Task GenerateAccessTokenAsync_WithValidUser_ShouldReturnToken()
    {
        // Arrange
        var email = Email.Create("test@example.com")!;
        var user = User.Create(email, "John", "Doe", "hashedPassword");

        // Act
        var token = await _jwtTokenService.GenerateAccessTokenAsync(user);

        // Assert
        token.Should().NotBeNullOrEmpty();
        
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        
        jwtToken.Claims.Should().Contain(c => c.Type == "nameid" && c.Value == user.Id.ToString());
        jwtToken.Claims.Should().Contain(c => c.Type == "email" && c.Value == user.Email.Value);
        jwtToken.Claims.Should().Contain(c => c.Type == "unique_name" && c.Value == user.FullName);
    }

    [Fact]
    public async Task GenerateAccessTokenAsync_ShouldCreateValidJwtToken()
    {
        // Arrange
        var email = Email.Create("test@example.com")!;
        var user = User.Create(email, "John", "Doe", "hashedPassword");

        // Act
        var token = await _jwtTokenService.GenerateAccessTokenAsync(user);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);

        jwtToken.Claims.Should().Contain(c => c.Type == "nameid" && c.Value == user.Id.ToString());
        jwtToken.Claims.Should().Contain(c => c.Type == "email" && c.Value == user.Email.Value);
        jwtToken.Claims.Should().Contain(c => c.Type == "unique_name" && c.Value == user.FullName);
        jwtToken.Claims.Should().Contain(c => c.Type == "email_verified" && c.Value == user.IsEmailVerified.ToString());
        jwtToken.Claims.Should().Contain(c => c.Type == "is_active" && c.Value == user.IsActive.ToString());
        
        jwtToken.Issuer.Should().Be("CleanArchTemplate");
        jwtToken.Audiences.Should().Contain("CleanArchTemplate");
    }

    [Fact]
    public async Task ValidateAccessToken_WithValidToken_ShouldReturnTrue()
    {
        // Arrange
        var email = Email.Create("test@example.com")!;
        var user = User.Create(email, "John", "Doe", "hashedPassword");
        var token = await _jwtTokenService.GenerateAccessTokenAsync(user);

        // Act
        var isValid = _jwtTokenService.ValidateAccessToken(token);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateAccessToken_WithInvalidToken_ShouldReturnFalse()
    {
        // Arrange
        var invalidToken = "invalid.jwt.token";

        // Act
        var isValid = _jwtTokenService.ValidateAccessToken(invalidToken);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task GetUserIdFromToken_WithValidToken_ShouldReturnUserId()
    {
        // Arrange
        var email = Email.Create("test@example.com")!;
        var user = User.Create(email, "John", "Doe", "hashedPassword");
        var token = await _jwtTokenService.GenerateAccessTokenAsync(user);

        // Act
        var userId = _jwtTokenService.GetUserIdFromToken(token);

        // Assert
        userId.Should().Be(user.Id);
    }

    [Fact]
    public void GetUserIdFromToken_WithInvalidToken_ShouldReturnNull()
    {
        // Arrange
        var invalidToken = "invalid.jwt.token";

        // Act
        var userId = _jwtTokenService.GetUserIdFromToken(invalidToken);

        // Assert
        userId.Should().BeNull();
    }

    [Fact]
    public async Task GenerateRefreshTokenAsync_ShouldReturnBase64String()
    {
        // Act
        var refreshToken = await _jwtTokenService.GenerateRefreshTokenAsync();

        // Assert
        refreshToken.Should().NotBeNullOrEmpty();
        refreshToken.Length.Should().BeGreaterThan(0);
        
        // Should be valid base64
        var act = () => Convert.FromBase64String(refreshToken);
        act.Should().NotThrow();
    }
}