using AutoMapper;
using CleanArchTemplate.Application.Common.Mappings;
using CleanArchTemplate.Application.Common.Models;
using CleanArchTemplate.Application.Features.Permissions.Queries.GetPermissions;
using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.Interfaces;
using CleanArchTemplate.Shared.Models;
using FluentAssertions;
using Moq;
using Xunit;

namespace CleanArchTemplate.UnitTests.Application.Features.Permissions.Queries;

public class GetPermissionsQueryHandlerTests
{
    private readonly Mock<IPermissionRepository> _mockPermissionRepository;
    private readonly IMapper _mapper;
    private readonly GetPermissionsQueryHandler _handler;

    public GetPermissionsQueryHandlerTests()
    {
        _mockPermissionRepository = new Mock<IPermissionRepository>();
        
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });
        _mapper = configuration.CreateMapper();
        
        _handler = new GetPermissionsQueryHandler(_mockPermissionRepository.Object, _mapper);
    }

    [Fact]
    public async Task Handle_WithValidQuery_ShouldReturnPaginatedPermissions()
    {
        // Arrange
        var permissions = new List<Permission>
        {
            Permission.Create("Users", "Create", "Create users", "User Management"),
            Permission.Create("Users", "Read", "Read users", "User Management"),
            Permission.Create("Roles", "Create", "Create roles", "Role Management")
        };

        _mockPermissionRepository.Setup(x => x.GetPermissionsWithHierarchyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);

        var query = new GetPermissionsQuery
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithResourceFilter_ShouldReturnFilteredPermissions()
    {
        // Arrange
        var permissions = new List<Permission>
        {
            Permission.Create("Users", "Create", "Create users", "User Management"),
            Permission.Create("Users", "Read", "Read users", "User Management"),
            Permission.Create("Roles", "Create", "Create roles", "Role Management")
        };

        _mockPermissionRepository.Setup(x => x.GetPermissionsWithHierarchyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);

        var query = new GetPermissionsQuery
        {
            Resource = "Users",
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(p => p.Resource == "Users");
    }

    [Fact]
    public async Task Handle_WithActionFilter_ShouldReturnFilteredPermissions()
    {
        // Arrange
        var permissions = new List<Permission>
        {
            Permission.Create("Users", "Create", "Create users", "User Management"),
            Permission.Create("Users", "Read", "Read users", "User Management"),
            Permission.Create("Roles", "Create", "Create roles", "Role Management")
        };

        _mockPermissionRepository.Setup(x => x.GetPermissionsWithHierarchyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);

        var query = new GetPermissionsQuery
        {
            Action = "Create",
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(p => p.Action == "Create");
    }

    [Fact]
    public async Task Handle_WithSearchTerm_ShouldReturnFilteredPermissions()
    {
        // Arrange
        var permissions = new List<Permission>
        {
            Permission.Create("Users", "Create", "Create users", "User Management"),
            Permission.Create("Users", "Read", "Read users", "User Management"),
            Permission.Create("Roles", "Create", "Create roles", "Role Management")
        };

        _mockPermissionRepository.Setup(x => x.GetPermissionsWithHierarchyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);

        var query = new GetPermissionsQuery
        {
            SearchTerm = "users",
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(p => p.Resource == "Users");
    }

    [Fact]
    public async Task Handle_WithSorting_ShouldReturnSortedPermissions()
    {
        // Arrange
        var permissions = new List<Permission>
        {
            Permission.Create("Users", "Create", "Create users", "User Management"),
            Permission.Create("Roles", "Create", "Create roles", "Role Management"),
            Permission.Create("Admin", "Create", "Create admin", "Admin Management")
        };

        _mockPermissionRepository.Setup(x => x.GetPermissionsWithHierarchyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);

        var query = new GetPermissionsQuery
        {
            SortBy = "resource",
            SortDirection = "asc",
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(3);
        result.Items.First().Resource.Should().Be("Admin");
        result.Items.Last().Resource.Should().Be("Users");
    }

    [Fact]
    public async Task Handle_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var permissions = new List<Permission>();
        for (int i = 1; i <= 25; i++)
        {
            permissions.Add(Permission.Create($"Resource{i}", "Create", $"Create resource {i}", "Management"));
        }

        _mockPermissionRepository.Setup(x => x.GetPermissionsWithHierarchyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);

        var query = new GetPermissionsQuery
        {
            PageNumber = 2,
            PageSize = 10
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(10);
        result.TotalCount.Should().Be(25);
        result.PageNumber.Should().Be(2);
        result.TotalPages.Should().Be(3);
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeTrue();
    }
}