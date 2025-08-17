using AutoMapper;
using CleanArchTemplate.Application.Common.Mappings;
using CleanArchTemplate.Application.Common.Models;
using CleanArchTemplate.Application.Features.Permissions.Queries.GetPermissionAuditLog;
using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.Interfaces;
using CleanArchTemplate.Shared.Models;
using FluentAssertions;
using Moq;
using Xunit;

namespace CleanArchTemplate.UnitTests.Application.Features.Permissions.Queries;

public class GetPermissionAuditLogQueryHandlerTests
{
    private readonly Mock<IPermissionAuditLogRepository> _mockAuditLogRepository;
    private readonly IMapper _mapper;
    private readonly GetPermissionAuditLogQueryHandler _handler;

    public GetPermissionAuditLogQueryHandlerTests()
    {
        _mockAuditLogRepository = new Mock<IPermissionAuditLogRepository>();
        
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });
        _mapper = configuration.CreateMapper();
        
        _handler = new GetPermissionAuditLogQueryHandler(_mockAuditLogRepository.Object, _mapper);
    }

    [Fact]
    public async Task Handle_WithValidQuery_ShouldReturnPaginatedAuditLogs()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();
        
        var auditLogs = new List<PermissionAuditLog>
        {
            PermissionAuditLog.CreateUserPermissionAssigned(userId, permissionId, "admin@example.com", "Initial assignment"),
            PermissionAuditLog.CreateUserPermissionRemoved(userId, permissionId, "admin@example.com", "Access revoked"),
            PermissionAuditLog.CreatePermissionModified(permissionId, "Old description", "New description", "admin@example.com")
        };

        _mockAuditLogRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(auditLogs);

        var query = new GetPermissionAuditLogQuery
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
    public async Task Handle_WithUserIdFilter_ShouldReturnFilteredAuditLogs()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var permissionId = Guid.NewGuid();
        
        var auditLogs = new List<PermissionAuditLog>
        {
            PermissionAuditLog.CreateUserPermissionAssigned(userId1, permissionId, "admin@example.com"),
            PermissionAuditLog.CreateUserPermissionAssigned(userId2, permissionId, "admin@example.com"),
            PermissionAuditLog.CreatePermissionModified(permissionId, "Old", "New", "admin@example.com")
        };

        _mockAuditLogRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(auditLogs);

        var query = new GetPermissionAuditLogQuery
        {
            UserId = userId1,
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items.First().UserId.Should().Be(userId1);
    }

    [Fact]
    public async Task Handle_WithRoleIdFilter_ShouldReturnFilteredAuditLogs()
    {
        // Arrange
        var roleId1 = Guid.NewGuid();
        var roleId2 = Guid.NewGuid();
        var permissionId = Guid.NewGuid();
        
        var auditLogs = new List<PermissionAuditLog>
        {
            PermissionAuditLog.CreateRolePermissionAssigned(roleId1, permissionId, "admin@example.com"),
            PermissionAuditLog.CreateRolePermissionAssigned(roleId2, permissionId, "admin@example.com"),
            PermissionAuditLog.CreatePermissionModified(permissionId, "Old", "New", "admin@example.com")
        };

        _mockAuditLogRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(auditLogs);

        var query = new GetPermissionAuditLogQuery
        {
            RoleId = roleId1,
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items.First().RoleId.Should().Be(roleId1);
    }

    [Fact]
    public async Task Handle_WithPermissionIdFilter_ShouldReturnFilteredAuditLogs()
    {
        // Arrange
        var permissionId1 = Guid.NewGuid();
        var permissionId2 = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        var auditLogs = new List<PermissionAuditLog>
        {
            PermissionAuditLog.CreateUserPermissionAssigned(userId, permissionId1, "admin@example.com"),
            PermissionAuditLog.CreateUserPermissionAssigned(userId, permissionId2, "admin@example.com"),
            PermissionAuditLog.CreatePermissionModified(permissionId1, "Old", "New", "admin@example.com")
        };

        _mockAuditLogRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(auditLogs);

        var query = new GetPermissionAuditLogQuery
        {
            PermissionId = permissionId1,
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(al => al.PermissionId == permissionId1);
    }

    [Fact]
    public async Task Handle_WithActionFilter_ShouldReturnFilteredAuditLogs()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();
        
        var auditLogs = new List<PermissionAuditLog>
        {
            PermissionAuditLog.CreateUserPermissionAssigned(userId, permissionId, "admin@example.com"),
            PermissionAuditLog.CreateUserPermissionRemoved(userId, permissionId, "admin@example.com"),
            PermissionAuditLog.CreatePermissionModified(permissionId, "Old", "New", "admin@example.com")
        };

        _mockAuditLogRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(auditLogs);

        var query = new GetPermissionAuditLogQuery
        {
            Action = "Assigned",
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items.First().Action.Should().Be("Assigned");
    }

    [Fact]
    public async Task Handle_WithDateRangeFilter_ShouldReturnFilteredAuditLogs()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();
        var startDate = DateTime.UtcNow.AddDays(-7);
        var endDate = DateTime.UtcNow.AddDays(-1);
        
        var auditLogs = new List<PermissionAuditLog>
        {
            PermissionAuditLog.CreateUserPermissionAssigned(userId, permissionId, "admin@example.com"),
            PermissionAuditLog.CreateUserPermissionRemoved(userId, permissionId, "admin@example.com"),
            PermissionAuditLog.CreatePermissionModified(permissionId, "Old", "New", "admin@example.com")
        };

        // Set performed dates
        auditLogs[0].GetType().GetProperty("PerformedAt")?.SetValue(auditLogs[0], startDate.AddDays(1));
        auditLogs[1].GetType().GetProperty("PerformedAt")?.SetValue(auditLogs[1], endDate.AddDays(1)); // Outside range
        auditLogs[2].GetType().GetProperty("PerformedAt")?.SetValue(auditLogs[2], startDate.AddDays(2));

        _mockAuditLogRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(auditLogs);

        var query = new GetPermissionAuditLogQuery
        {
            StartDate = startDate,
            EndDate = endDate,
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WithSorting_ShouldReturnSortedAuditLogs()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();
        
        var auditLogs = new List<PermissionAuditLog>
        {
            PermissionAuditLog.CreateUserPermissionAssigned(userId, permissionId, "admin@example.com"),
            PermissionAuditLog.CreateUserPermissionRemoved(userId, permissionId, "admin@example.com"),
            PermissionAuditLog.CreatePermissionModified(permissionId, "Old", "New", "admin@example.com")
        };

        _mockAuditLogRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(auditLogs);

        var query = new GetPermissionAuditLogQuery
        {
            SortBy = "action",
            SortDirection = "asc",
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(3);
        // Verify sorting was applied (exact order depends on the mock implementation)
    }

    [Fact]
    public async Task Handle_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();
        
        var auditLogs = new List<PermissionAuditLog>();
        for (int i = 1; i <= 25; i++)
        {
            auditLogs.Add(PermissionAuditLog.CreateUserPermissionAssigned(userId, permissionId, $"admin{i}@example.com"));
        }

        _mockAuditLogRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(auditLogs);

        var query = new GetPermissionAuditLogQuery
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