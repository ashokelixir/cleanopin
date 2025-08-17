using CleanArchTemplate.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Security.Claims;
using Xunit;

namespace CleanArchTemplate.UnitTests.Infrastructure.Services;

public class CurrentUserServiceTests
{
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly CurrentUserService _service;

    public CurrentUserServiceTests()
    {
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _service = new CurrentUserService(_mockHttpContextAccessor.Object);
    }

    [Fact]
    public void UserId_WhenUserIsAuthenticated_ShouldReturnUserId()
    {
        // Arrange
        var userId = "user123";
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId)
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };

        _mockHttpContextAccessor
            .Setup(x => x.HttpContext)
            .Returns(httpContext);

        // Act
        var result = _service.UserId;

        // Assert
        result.Should().Be(userId);
    }

    [Fact]
    public void UserEmail_WhenUserIsAuthenticated_ShouldReturnUserEmail()
    {
        // Arrange
        var userEmail = "user@test.com";
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, userEmail)
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };

        _mockHttpContextAccessor
            .Setup(x => x.HttpContext)
            .Returns(httpContext);

        // Act
        var result = _service.UserEmail;

        // Assert
        result.Should().Be(userEmail);
    }

    [Fact]
    public void UserName_WhenUserIsAuthenticated_ShouldReturnUserName()
    {
        // Arrange
        var userName = "Test User";
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, userName)
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };

        _mockHttpContextAccessor
            .Setup(x => x.HttpContext)
            .Returns(httpContext);

        // Act
        var result = _service.UserName;

        // Assert
        result.Should().Be(userName);
    }

    [Fact]
    public void IsAuthenticated_WhenUserIsAuthenticated_ShouldReturnTrue()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "user123")
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };

        _mockHttpContextAccessor
            .Setup(x => x.HttpContext)
            .Returns(httpContext);

        // Act
        var result = _service.IsAuthenticated;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAuthenticated_WhenUserIsNotAuthenticated_ShouldReturnFalse()
    {
        // Arrange
        var identity = new ClaimsIdentity(); // No authentication type
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };

        _mockHttpContextAccessor
            .Setup(x => x.HttpContext)
            .Returns(httpContext);

        // Act
        var result = _service.IsAuthenticated;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAuthenticated_WhenHttpContextIsNull_ShouldReturnFalse()
    {
        // Arrange
        _mockHttpContextAccessor
            .Setup(x => x.HttpContext)
            .Returns((HttpContext?)null);

        // Act
        var result = _service.IsAuthenticated;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetAuditIdentifier_WhenUserIsAuthenticated_ShouldReturnUserEmail()
    {
        // Arrange
        var userEmail = "user@test.com";
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, userEmail),
            new(ClaimTypes.NameIdentifier, "user123")
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };

        _mockHttpContextAccessor
            .Setup(x => x.HttpContext)
            .Returns(httpContext);

        // Act
        var result = _service.GetAuditIdentifier();

        // Assert
        result.Should().Be(userEmail);
    }

    [Fact]
    public void GetAuditIdentifier_WhenUserIsAuthenticatedButNoEmail_ShouldReturnUserId()
    {
        // Arrange
        var userId = "user123";
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId)
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };

        _mockHttpContextAccessor
            .Setup(x => x.HttpContext)
            .Returns(httpContext);

        // Act
        var result = _service.GetAuditIdentifier();

        // Assert
        result.Should().Be(userId);
    }

    [Fact]
    public void GetAuditIdentifier_WhenUserIsAuthenticatedButNoEmailOrUserId_ShouldReturnUserName()
    {
        // Arrange
        var userName = "Test User";
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, userName)
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };

        _mockHttpContextAccessor
            .Setup(x => x.HttpContext)
            .Returns(httpContext);

        // Act
        var result = _service.GetAuditIdentifier();

        // Assert
        result.Should().Be(userName);
    }

    [Fact]
    public void GetAuditIdentifier_WhenUserIsAuthenticatedButNoClaims_ShouldReturnAuthenticatedUser()
    {
        // Arrange
        var identity = new ClaimsIdentity("test"); // Authenticated but no claims
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };

        _mockHttpContextAccessor
            .Setup(x => x.HttpContext)
            .Returns(httpContext);

        // Act
        var result = _service.GetAuditIdentifier();

        // Assert
        result.Should().Be("authenticated-user");
    }

    [Fact]
    public void GetAuditIdentifier_WhenUserIsNotAuthenticated_ShouldReturnSystem()
    {
        // Arrange
        var identity = new ClaimsIdentity(); // No authentication type
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };

        _mockHttpContextAccessor
            .Setup(x => x.HttpContext)
            .Returns(httpContext);

        // Act
        var result = _service.GetAuditIdentifier();

        // Assert
        result.Should().Be("system");
    }

    [Fact]
    public void GetAuditIdentifier_WhenHttpContextIsNull_ShouldReturnSystem()
    {
        // Arrange
        _mockHttpContextAccessor
            .Setup(x => x.HttpContext)
            .Returns((HttpContext?)null);

        // Act
        var result = _service.GetAuditIdentifier();

        // Assert
        result.Should().Be("system");
    }

    [Fact]
    public void Properties_WhenHttpContextIsNull_ShouldReturnNull()
    {
        // Arrange
        _mockHttpContextAccessor
            .Setup(x => x.HttpContext)
            .Returns((HttpContext?)null);

        // Act & Assert
        _service.UserId.Should().BeNull();
        _service.UserEmail.Should().BeNull();
        _service.UserName.Should().BeNull();
    }
}