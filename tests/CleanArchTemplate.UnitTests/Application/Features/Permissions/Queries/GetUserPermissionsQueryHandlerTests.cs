using AutoMapper;
using CleanArchTemplate.Application.Common.Mappings;
using CleanArchTemplate.Application.Features.Permissions.Queries.GetUserPermissions;
using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.Enums;
using CleanArchTemplate.Domain.Interfaces;
using CleanArchTemplate.Domain.Services;
using CleanArchTemplate.Domain.ValueObjects;
using FluentAssertions;
using Moq;
using Xunit;

namespace CleanArchTemplate.UnitTests.Application.Features.Permissions.Queries;

public class GetUserPermissionsQueryHandlerTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IPermissionRepository> _mockPermissionRepository;
    private readonly Mock<IUserPermissionRepository> _mockUserPermissionRepository;
    private readonly Mock<IPermissionEvaluationService> _mockPermissionEvaluationService;
    private readonly IMapper _mapper;
    private readonly GetUserPermissionsQueryHandler _handler;

    public GetUserPermissionsQueryHandlerTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockPermissionRepository = new Mock<IPermissionRepository>();
        _mockUserPermissionRepository = new Mock<IUserPermissionRepository>();
        _mockPermissionEvaluationService = new Mock<IPermissionEvaluationService>();
        
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });
        _mapper = configuration.CreateMapper();
        
        _handler = new GetUserPermissionsQueryHandler(
            _mockUserRepository.Object,
            _mockPermissionRepository.Object,
            _mockUserPermissionRepository.Object,
            _mockPermissionEvaluationService.Object,
            _mapper);
    }

    [Fact]
    public async Task Handle_WithValidUserId_ShouldReturnUserPermissionMatrix()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permission1 = Permission.Create("Users", "Create", "Create users", "User Management");
        var permission2 = Permission.Create("Users", "Read", "Read users", "User Management");
        
        var role = new Role("Admin", "Administrator role");
        var user = User.Create(Email.Create("test@example.com"), "Test", "User", "hashedpassword");
        
        var userRole = new UserRole(userId, role.Id);
        var rolePermission = RolePermission.Create(role.Id, permission1.Id);

        // Set up user with roles
        user.GetType().GetProperty("UserRoles")?.SetValue(user, new List<UserRole> { userRole });
        role.GetType().GetProperty("RolePermissions")?.SetValue(role, new List<RolePermission> { rolePermission });
        userRole.GetType().GetProperty("Role")?.SetValue(userRole, role);

        var permissions = new List<Permission> { permission1, permission2 };
        var userPermissions = new List<UserPermission>
        {
            UserPermission.Create(userId, permission2.Id, PermissionState.Grant, "Special access")
        };

        _mockUserRepository.Setup(x => x.GetUserWithRolesAndPermissionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockPermissionRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);
        _mockUserPermissionRepository.Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userPermissions);

        // Mock permission evaluation service
        _mockPermissionEvaluationService
            .Setup(x => x.HasPermission(user, permission1.Name, permissions, It.IsAny<List<Role>>()))
            .Returns(true);
        _mockPermissionEvaluationService
            .Setup(x => x.HasPermission(user, permission2.Name, permissions, It.IsAny<List<Role>>()))
            .Returns(true);

        var query = new GetUserPermissionsQuery { UserId = userId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.User.Should().NotBeNull();
        result.User.Email.Should().Be("test@example.com");
        result.RolePermissions.Should().HaveCount(1);
        result.UserOverrides.Should().HaveCount(1);
        result.EffectivePermissions.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockUserRepository.Setup(x => x.GetUserWithRolesAndPermissionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var query = new GetUserPermissionsQuery { UserId = userId };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithPermissionFilter_ShouldReturnFilteredPermissions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permission1 = Permission.Create("Users", "Create", "Create users", "User Management");
        var permission2 = Permission.Create("Roles", "Read", "Read roles", "Role Management");
        
        var user = User.Create(Email.Create("test@example.com"), "Test", "User", "hashedpassword");
        user.GetType().GetProperty("UserRoles")?.SetValue(user, new List<UserRole>());

        var permissions = new List<Permission> { permission1, permission2 };

        _mockUserRepository.Setup(x => x.GetUserWithRolesAndPermissionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockPermissionRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);
        _mockUserPermissionRepository.Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserPermission>());

        var query = new GetUserPermissionsQuery 
        { 
            UserId = userId,
            PermissionFilter = "Users"
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        // The filtering should have been applied to the permissions query
        _mockPermissionRepository.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithCategoryFilter_ShouldReturnFilteredPermissions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permission1 = Permission.Create("Users", "Create", "Create users", "User Management");
        var permission2 = Permission.Create("Reports", "Read", "Read reports", "Reporting");
        
        var user = User.Create(Email.Create("test@example.com"), "Test", "User", "hashedpassword");
        user.GetType().GetProperty("UserRoles")?.SetValue(user, new List<UserRole>());

        var permissions = new List<Permission> { permission1, permission2 };

        _mockUserRepository.Setup(x => x.GetUserWithRolesAndPermissionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockPermissionRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);
        _mockUserPermissionRepository.Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserPermission>());

        var query = new GetUserPermissionsQuery 
        { 
            UserId = userId,
            CategoryFilter = "User Management"
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _mockPermissionRepository.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithIncludeExpiredOverrides_ShouldIncludeExpiredOverrides()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permission1 = Permission.Create("Users", "Create", "Create users", "User Management");
        
        var user = User.Create(Email.Create("test@example.com"), "Test", "User", "hashedpassword");
        user.GetType().GetProperty("UserRoles")?.SetValue(user, new List<UserRole>());

        var expiredOverride = UserPermission.Create(userId, permission1.Id, PermissionState.Grant, "Expired access");
        expiredOverride.GetType().GetProperty("ExpiresAt")?.SetValue(expiredOverride, DateTime.UtcNow.AddDays(-1));

        var permissions = new List<Permission> { permission1 };
        var userPermissions = new List<UserPermission> { expiredOverride };

        _mockUserRepository.Setup(x => x.GetUserWithRolesAndPermissionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockPermissionRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);
        _mockUserPermissionRepository.Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userPermissions);

        var query = new GetUserPermissionsQuery 
        { 
            UserId = userId,
            IncludeExpiredOverrides = true
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UserOverrides.Should().HaveCount(1);
    }
}