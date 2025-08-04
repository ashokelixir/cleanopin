using CleanArchTemplate.Domain.ValueObjects;
using CleanArchTemplate.TestUtilities.Common;

namespace CleanArchTemplate.UnitTests.Domain.ValueObjects;

public class EmailTests : BaseTest
{
    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@domain.co.uk")]
    [InlineData("test+tag@example.org")]
    public void Create_WithValidEmail_ShouldReturnSuccess(string validEmail)
    {
        // Act
        var result = Email.Create(validEmail);

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().Be(validEmail.ToLowerInvariant());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("test@")]
    [InlineData("test.example.com")]
    public void Create_WithInvalidEmail_ShouldReturnFailure(string invalidEmail)
    {
        // Act & Assert
        var act = () => Email.Create(invalidEmail);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNull_ShouldReturnFailure()
    {
        // Act & Assert
        var act = () => Email.Create(null!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Equals_WithSameEmail_ShouldReturnTrue()
    {
        // Arrange
        var email1 = Email.Create("test@example.com");
        var email2 = Email.Create("TEST@EXAMPLE.COM");

        // Act & Assert
        email1.Should().Be(email2);
        (email1 == email2).Should().BeTrue();
        email1.GetHashCode().Should().Be(email2.GetHashCode());
    }

    [Fact]
    public void Equals_WithDifferentEmail_ShouldReturnFalse()
    {
        // Arrange
        var email1 = Email.Create("test1@example.com");
        var email2 = Email.Create("test2@example.com");

        // Act & Assert
        email1.Should().NotBe(email2);
        (email1 != email2).Should().BeTrue();
    }

    [Fact]
    public void ToString_ShouldReturnEmailValue()
    {
        // Arrange
        var emailValue = "test@example.com";
        var email = Email.Create(emailValue);

        // Act
        var result = email.ToString();

        // Assert
        result.Should().Be(emailValue.ToLowerInvariant());
    }

    [Fact]
    public void ImplicitConversion_ToStringShouldWork()
    {
        // Arrange
        var email = Email.Create("test@example.com");

        // Act
        string emailString = email;

        // Assert
        emailString.Should().Be("test@example.com");
    }
}