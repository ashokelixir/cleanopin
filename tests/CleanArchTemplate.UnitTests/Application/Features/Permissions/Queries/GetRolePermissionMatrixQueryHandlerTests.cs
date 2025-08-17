using AutoMapper;
using CleanArchTemplate.Application.Common.Mappings;
using CleanArchTemplate.Application.Features.Permissions.Queries.GetRolePermissionMatrix;
using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace CleanArchTemplate.UnitTests.Application.Features.Permissions.Queries;

public class GetRolePermissionMatrixQueryHandlerTests
{
    private readonly Mock<IRoleRepository> _mockRoleRepository;
    private readonly Mock<IPermissionRepository> _mockPermissionRepository;
    private readonly IMapper _mapper;
    private readonly GetRolePermissionMatrixQueryHandler _handler;

    public GetRolePermissionMatrixQueryHandlerTests()
    {
        _mockRoleRepository = new Mock<IRoleRepository>();
        _mockPermissionRepository = new Mock<IPermissionRepository>();
        
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });
        _mapper = configuration.CreateMapper();
        
        _handler = new GetRolePermissionMatrixQueryHandler(
            _mockRoleRepository.Object, 
            _mockPermissionRepository.Object, 
            _mapper);
    }

    [Fact]
    public async Task Handle_WithValidQuery_ShouldReturnPermissionMatrix()
    {
        // Arrange
        var permission1 = Permission.Create("Users", "Create", "Create users", "User Management");
        var permission2 = Permission.Create("Users", "Read", "Read users", "User Management");
        
        var role1 = new Role("Admin", "Administrator role");
        var role2 = new Role("User", "Regular user role");

        // Simulate role permissions
        var rolePermissions = new List<RolePermission>
        {
            new RolePermission(role1.Id, permission1.Id),
            new RolePermission(role1.Id, permission2.Id),
            new RolePermission(role2.Id, permission2.Id)
        };

        role1.GetType().GetProperty("RolePermissions")?.SetValue(role1, rolePermissions.Where(rp => rp.RoleId == role1.Id).ToList());
        role2.GetType().GetProperty("RolePermissions")?.SetValue(role2, rolePermissions.Where(rp => rp.RoleId == role2.Id).ToList());

        var roles = new List<Role> { role1, role2 };
        var permissions = new List<Permission> { permission1, permission2 };

        _mockRoleRepository.Setup(x => x.GetRolesWithPermissionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);
        _mockPermissionRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);

        var query = new GetRolePermissionMatrixQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Roles.Should().HaveCount(2);
        result.Permissions.Should().HaveCount(2);
        result.Assignments.Should().HaveCount(4); // 2 roles × 2 permissions
        
        // Check specific assignments
        var adminCreateAssignment = result.Assignments.FirstOrDefault(a => 
            a.RoleId == role1.Id && a.PermissionId == permission1.Id);
        adminCreateAssignment.Should().NotBeNull();
        adminCreateAssignment!.IsAssigned.Should().BeTrue();

        var userCreateAssignment = result.Assignments.FirstOrDefault(a => 
            a.RoleId == role2.Id && a.PermissionId == permission1.Id);
        userCreateAssignment.Should().NotBeNull();
        userCreateAssignment!.IsAssigned.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithRoleFilter_ShouldReturnFilteredMatrix()
    {
        // Arrange
        var permission1 = Permission.Create("Users", "Create", "Create users", "User Management");
        
        var role1 = new Role("Admin", "Administrator role");
        var role2 = new Role("User", "Regular user role");

        var roles = new List<Role> { role1, role2 };
        var permissions = new List<Permission> { permission1 };

        _mockRoleRepository.Setup(x => x.GetRolesWithPermissionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);
        _mockPermissionRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);

        var query = new GetRolePermissionMatrixQuery
        {
            RoleFilter = "Admin"
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Roles.Should().HaveCount(1);
        result.Roles.First().Name.Should().Be("Admin");
    }

    [Fact]
    public async Task Handle_WithPermissionFilter_ShouldReturnFilteredMatrix()
    {
        // Arrange
        var permission1 = Permission.Create("Users", "Create", "Create users", "User Management");
        var permission2 = Permission.Create("Users", "Read", "Read users", "User Management");
        
        var role1 = new Role("Admin", "Administrator role");

        var roles = new List<Role> { role1 };
        var permissions = new List<Permission> { permission1, permission2 };

        _mockRoleRepository.Setup(x => x.GetRolesWithPermissionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);
        _mockPermissionRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);

        var query = new GetRolePermissionMatrixQuery
        {
            PermissionFilter = "Create"
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Permissions.Should().HaveCount(1);
        result.Permissions.First().Action.Should().Be("Create");
    }

    [Fact]
    public async Task Handle_WithCategoryFilter_ShouldReturnFilteredMatrix()
    {
        // Arrange
        var permission1 = Permission.Create("Users", "Create", "Create users", "User Management");
        var permission2 = Permission.Create("Reports", "Read", "Read reports", "Reporting");
        
        var role1 = new Role("Admin", "Administrator role");

        var roles = new List<Role> { role1 };
        var permissions = new List<Permission> { permission1, permission2 };

        _mockRoleRepository.Setup(x => x.GetRolesWithPermissionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);
        _mockPermissionRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);

        var query = new GetRolePermissionMatrixQuery
        {
            CategoryFilter = "User Management"
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Permissions.Should().HaveCount(1);
        result.Permissions.First().Category.Should().Be("User Management");
    }

    [Fact]
    public async Task Handle_WithIncludeInactiveRoles_ShouldIncludeInactiveRoles()
    {
        // Arrange
        var permission1 = Permission.Create("Users", "Create", "Create users", "User Management");
        
        var role1 = new Role("Admin", "Administrator role");
        var role2 = new Role("Inactive", "Inactive role");
        role2.Deactivate();

        var roles = new List<Role> { role1, role2 };
        var permissions = new List<Permission> { permission1 };

        _mockRoleRepository.Setup(x => x.GetRolesWithPermissionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);
        _mockPermissionRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);

        var query = new GetRolePermissionMatrixQuery
        {
            IncludeInactiveRoles = true
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Roles.Should().HaveCount(2);
    }
}