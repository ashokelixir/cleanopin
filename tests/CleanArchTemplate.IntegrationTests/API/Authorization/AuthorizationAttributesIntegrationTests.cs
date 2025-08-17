using System.Net;
using System.Net.Http.Headers;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using FluentAssertions;
using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.ValueObjects;
using CleanArchTemplate.Domain.Enums;
using CleanArchTemplate.Infrastructure.Data.Contexts;
using CleanArchTemplate.TestUtilities.Common;
using Xunit.Abstractions;

namespace CleanArchTemplate.IntegrationTests.API.Authorization;

/// <summary>
/// Integration tests for authorization attributes
/// </summary>
public class AuthorizationAttributesIntegrationTests : BaseIntegrationTest
{
    public AuthorizationAttributesIntegrationTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task RequirePermissionAttribute_WithValidPermission_ShouldAllowAccess()
    {
        // Arrange
        var user = await CreateTestUserWithPermissionsAsync(new[] { "Users.Read" });
        var token = await GenerateJwtTokenAsync(user);
        
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.GetAsync("/api/test/authorization/require-permission");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Access granted with RequirePermission attribute");
    }

    [Fact]
    public async Task RequirePermissionAttribute_WithoutPermission_ShouldDenyAccess()
    {
        // Arrange
        var user = await CreateTestUserWithPermissionsAsync(new[] { "Users.Create" }); // Different permission
        var token = await GenerateJwtTokenAsync(user);
        
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.GetAsync("/api/test/authorization/require-permission");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RequirePermissionAttribute_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/test/authorization/require-permission");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RequireResourceActionAttribute_WithValidResourceAction_ShouldAllowAccess()
    {
        // Arrange
        var user = await CreateTestUserWithPermissionsAsync(new[] { "Users.Create" });
        var token = await GenerateJwtTokenAsync(user);
        
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.GetAsync("/api/test/authorization/require-resource-action");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Access granted with RequireResourceAction attribute");
    }

    [Fact]
    public async Task RequireResourceActionAttribute_WithoutPermission_ShouldDenyAccess()
    {
        // Arrange
        var user = await CreateTestUserWithPermissionsAsync(new[] { "Users.Read" }); // Different permission
        var token = await GenerateJwtTokenAsync(user);
        
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.GetAsync("/api/test/authorization/require-resource-action");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RequireAnyPermissionAttribute_WithOneValidPermission_ShouldAllowAccess()
    {
        // Arrange
        var user = await CreateTestUserWithPermissionsAsync(new[] { "Users.Update" }); // One of the required permissions
        var token = await GenerateJwtTokenAsync(user);
        
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.GetAsync("/api/test/authorization/require-any-permission");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Access granted with RequireAnyPermission attribute");
    }

    [Fact]
    public async Task RequireAnyPermissionAttribute_WithMultipleValidPermissions_ShouldAllowAccess()
    {
        // Arrange
        var user = await CreateTestUserWithPermissionsAsync(new[] { "Users.Create", "Users.Update", "Users.Delete" });
        var token = await GenerateJwtTokenAsync(user);
        
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.GetAsync("/api/test/authorization/require-any-permission");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Access granted with RequireAnyPermission attribute");
    }

    [Fact]
    public async Task RequireAnyPermissionAttribute_WithoutAnyPermission_ShouldDenyAccess()
    {
        // Arrange
        var user = await CreateTestUserWithPermissionsAsync(new[] { "Users.Read" }); // None of the required permissions
        var token = await GenerateJwtTokenAsync(user);
        
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.GetAsync("/api/test/authorization/require-any-permission");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task MultipleAuthorizationAttributes_WithAllPermissions_ShouldAllowAccess()
    {
        // Arrange
        var user = await CreateTestUserWithPermissionsAsync(new[] { "Users.Read", "Roles.Read" });
        var token = await GenerateJwtTokenAsync(user);
        
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.GetAsync("/api/test/authorization/multiple-attributes");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Access granted with multiple authorization attributes");
    }

    [Fact]
    public async Task MultipleAuthorizationAttributes_WithPartialPermissions_ShouldDenyAccess()
    {
        // Arrange
        var user = await CreateTestUserWithPermissionsAsync(new[] { "Users.Read" }); // Missing Roles.Read
        var token = await GenerateJwtTokenAsync(user);
        
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.GetAsync("/api/test/authorization/multiple-attributes");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task NoAuthorizationEndpoint_ShouldAllowAccessWithoutAuthentication()
    {
        // Act
        var response = await Client.GetAsync("/api/test/authorization/no-authorization");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("No authorization required");
    }

    [Fact]
    public async Task InvalidPermission_ShouldDenyAccess()
    {
        // Arrange
        var user = await CreateTestUserWithPermissionsAsync(new[] { "Users.Read", "Users.Create", "Users.Update" });
        var token = await GenerateJwtTokenAsync(user);
        
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.GetAsync("/api/test/authorization/invalid-permission");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UserPermissionOverride_GrantPermission_ShouldAllowAccess()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        
        // Create permission
        var permission = await CreatePermissionAsync("Users", "Read");
        
        // Grant permission directly to user (override)
        await CreateUserPermissionAsync(user.Id, permission.Id, PermissionState.Grant);
        
        var token = await GenerateJwtTokenAsync(user);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.GetAsync("/api/test/authorization/require-permission");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UserPermissionOverride_DenyPermission_ShouldDenyAccess()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        
        // Create role with permission
        var role = await CreateRoleAsync("TestRole");
        var permission = await CreatePermissionAsync("Users", "Read");
        await AssignPermissionToRoleAsync(role.Id, permission.Id);
        await AssignRoleToUserAsync(user.Id, role.Id);
        
        // Deny permission directly to user (override)
        await CreateUserPermissionAsync(user.Id, permission.Id, PermissionState.Deny);
        
        var token = await GenerateJwtTokenAsync(user);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.GetAsync("/api/test/authorization/require-permission");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ExpiredToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var user = await CreateTestUserWithPermissionsAsync(new[] { "Users.Read" });
        var expiredToken = await GenerateExpiredJwtTokenAsync(user);
        
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

        // Act
        var response = await Client.GetAsync("/api/test/authorization/require-permission");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task InvalidToken_ShouldReturnUnauthorized()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

        // Act
        var response = await Client.GetAsync("/api/test/authorization/require-permission");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task InactiveUser_ShouldDenyAccess()
    {
        // Arrange
        var user = await CreateTestUserWithPermissionsAsync(new[] { "Users.Read" });
        
        // Deactivate user
        using var scope = Factory.Services.CreateScope();
        var context = GetDbContext(scope);
        var userEntity = await context.Users.FindAsync(user.Id);
        userEntity!.Deactivate();
        await context.SaveChangesAsync();
        
        var token = await GenerateJwtTokenAsync(user);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.GetAsync("/api/test/authorization/require-permission");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// Helper method to create a test user with specific permissions
    /// </summary>
    private async Task<User> CreateTestUserWithPermissionsAsync(string[] permissions)
    {
        var user = await CreateTestUserAsync();
        var role = await CreateRoleAsync("TestRole");
        
        foreach (var permissionName in permissions)
        {
            var parts = permissionName.Split('.');
            var permission = await CreatePermissionAsync(parts[0], parts[1]);
            await AssignPermissionToRoleAsync(role.Id, permission.Id);
        }
        
        await AssignRoleToUserAsync(user.Id, role.Id);
        return user;
    }

    /// <summary>
    /// Creates a test user
    /// </summary>
    private async Task<User> CreateTestUserAsync()
    {
        return await ExecuteDbContextAsync(async context =>
        {
            var user = User.Create(
                Email.Create($"test{Guid.NewGuid():N}@example.com"),
                "Test",
                "User",
                "hashedPassword");
            
            context.Users.Add(user);
            await context.SaveChangesAsync();
            return user;
        });
    }

    /// <summary>
    /// Creates a test role
    /// </summary>
    private async Task<Role> CreateRoleAsync(string name)
    {
        return await ExecuteDbContextAsync(async context =>
        {
            var role = new Role($"{name}{Guid.NewGuid():N}", $"{name} Description");
            context.Roles.Add(role);
            await context.SaveChangesAsync();
            return role;
        });
    }

    /// <summary>
    /// Creates a permission
    /// </summary>
    private async Task<Permission> CreatePermissionAsync(string resource, string action)
    {
        return await ExecuteDbContextAsync(async context =>
        {
            var existingPermission = context.Permissions
                .FirstOrDefault(p => p.Resource == resource && p.Action == action);
            
            if (existingPermission != null)
                return existingPermission;

            var permission = Permission.Create(resource, action, $"{resource} {action} permission", "Test");
            context.Permissions.Add(permission);
            await context.SaveChangesAsync();
            return permission;
        });
    }

    /// <summary>
    /// Assigns a permission to a role
    /// </summary>
    private async Task AssignPermissionToRoleAsync(Guid roleId, Guid permissionId)
    {
        await ExecuteDbContextAsync(async context =>
        {
            var role = await context.Roles.FindAsync(roleId);
            var permission = await context.Permissions.FindAsync(permissionId);
            
            if (role != null && permission != null)
            {
                role.AddPermission(permission);
                await context.SaveChangesAsync();
            }
        });
    }

    /// <summary>
    /// Assigns a role to a user
    /// </summary>
    private async Task AssignRoleToUserAsync(Guid userId, Guid roleId)
    {
        await ExecuteDbContextAsync(async context =>
        {
            var user = await context.Users.FindAsync(userId);
            var role = await context.Roles.FindAsync(roleId);
            
            if (user != null && role != null)
            {
                user.AddRole(role);
                await context.SaveChangesAsync();
            }
        });
    }

    /// <summary>
    /// Creates a user permission override
    /// </summary>
    private async Task CreateUserPermissionAsync(Guid userId, Guid permissionId, PermissionState state)
    {
        await ExecuteDbContextAsync(async context =>
        {
            var userPermission = UserPermission.Create(userId, permissionId, state, "Test override");
            context.UserPermissions.Add(userPermission);
            await context.SaveChangesAsync();
        });
    }

    /// <summary>
    /// Generates a JWT token for a user
    /// </summary>
    private async Task<string> GenerateJwtTokenAsync(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes("your-super-secret-key-that-is-at-least-32-characters-long");
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email.Value),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}")
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = "CleanArchTemplate",
            Audience = "CleanArchTemplate",
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Generates an expired JWT token for a user
    /// </summary>
    private async Task<string> GenerateExpiredJwtTokenAsync(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes("your-super-secret-key-that-is-at-least-32-characters-long");
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email.Value),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}")
            }),
            Expires = DateTime.UtcNow.AddHours(-1), // Expired token
            Issuer = "CleanArchTemplate",
            Audience = "CleanArchTemplate",
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Gets the database context from a scope
    /// </summary>
    private ApplicationDbContext GetDbContext(IServiceScope scope)
    {
        return scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }
}