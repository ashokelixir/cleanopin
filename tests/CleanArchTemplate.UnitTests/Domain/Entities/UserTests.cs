using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.ValueObjects;
using CleanArchTemplate.TestUtilities.Common;
using CleanArchTemplate.TestUtilities.Builders;

namespace CleanArchTemplate.UnitTests.Domain.Entities;

public class UserTests : BaseTest
{
    [Fact]
    public void User_Creation_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var email = Email.Create("test@example.com");
        var firstName = "John";
        var lastName = "Doe";
        var passwordHash = "hashedpassword";

        // Act
        var user = User.Create(email, firstName, lastName, passwordHash);

        // Assert
        user.Email.Should().Be(email);
        user.FirstName.Should().Be(firstName);
        user.LastName.Should().Be(lastName);
        user.PasswordHash.Should().Be(passwordHash);
        user.IsEmailVerified.Should().BeFalse();
        user.IsActive.Should().BeTrue();
        user.LastLoginAt.Should().BeNull();
    }

    [Fact]
    public void User_FullName_ShouldCombineFirstAndLastName()
    {
        // Arrange
        var user = new UserBuilder()
            .WithName("John", "Doe")
            .Build();

        // Act
        var fullName = user.FullName;

        // Assert
        fullName.Should().Be("John Doe");
    }

    [Fact]
    public void User_WithRoles_ShouldMaintainRoleCollection()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("test@example.com")
            .WithName("John", "Doe")
            .Build();

        var role = new RoleBuilder()
            .WithName("Admin")
            .Build();

        // Act
        user.AddRole(role);

        // Assert
        user.UserRoles.Should().HaveCount(1);
        user.UserRoles.First().RoleId.Should().Be(role.Id);
        // Note: In unit tests, navigation properties aren't automatically populated like in EF Core
        // So we test the RoleId instead of Role.Name
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void User_IsActive_ShouldSetCorrectly(bool isActive)
    {
        // Arrange & Act
        var userBuilder = new UserBuilder();
        if (!isActive)
        {
            userBuilder.AsInactive();
        }
        var user = userBuilder.Build();

        // Assert
        user.IsActive.Should().Be(isActive);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void User_IsEmailVerified_ShouldSetCorrectly(bool isVerified)
    {
        // Arrange & Act
        var userBuilder = new UserBuilder();
        if (!isVerified)
        {
            userBuilder.AsUnverified();
        }
        var user = userBuilder.Build();

        // Assert
        user.IsEmailVerified.Should().Be(isVerified);
    }
}