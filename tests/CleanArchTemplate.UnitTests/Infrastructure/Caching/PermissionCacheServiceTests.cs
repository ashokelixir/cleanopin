using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.Interfaces;
using CleanArchTemplate.Domain.Services;
using CleanArchTemplate.Domain.ValueObjects;
using CleanArchTemplate.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text;
using System.Text.Json;
using Xunit;

namespace CleanArchTemplate.UnitTests.Infrastructure.Caching;

public class PermissionCacheServiceTests
{
    private readonly Mock<IDistributedCache> _mockDistributedCache;
    private readonly Mock<IMemoryCache> _mockMemoryCache;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IRoleRepository> _mockRoleRepository;
    private readonly Mock<IPermissionRepository> _mockPermissionRepository;
    private readonly Mock<IPermissionEvaluationService> _mockPermissionEvaluationService;
    private readonly Mock<ILogger<PermissionCacheService>> _mockLogger;
    private readonly PermissionCacheOptions _options;
    private readonly PermissionCacheService _service;

    public PermissionCacheServiceTests()
    {
        _mockDistributedCache = new Mock<IDistributedCache>();
        _mockMemoryCache = new Mock<IMemoryCache>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockRoleRepository = new Mock<IRoleRepository>();
        _mockPermissionRepository = new Mock<IPermissionRepository>();
        _mockPermissionEvaluationService = new Mock<IPermissionEvaluationService>();
        _mockLogger = new Mock<ILogger<PermissionCacheService>>();
        
        _options = new PermissionCacheOptions
        {
            DefaultExpiry = TimeSpan.FromMinutes(15),
            L1CacheExpiry = TimeSpan.FromMinutes(5),
            EvaluationCacheExpiry = TimeSpan.FromMinutes(10),
            WarmUpCacheExpiry = TimeSpan.FromMinutes(30),
            CommonPermissions = new List<string> { "Users.Read", "Users.Create" }
        };

        var mockOptions = new Mock<IOptions<PermissionCacheOptions>>();
        mockOptions.Setup(x => x.Value).Returns(_options);

        _service = new PermissionCacheService(
            _mockDistributedCache.Object,
            _mockMemoryCache.Object,
            _mockUserRepository.Object,
            _mockRoleRepository.Object,
            _mockPermissionRepository.Object,
            _mockPermissionEvaluationService.Object,
            _mockLogger.Object,
            mockOptions.Object);
    }

    [Fact]
    public async Task GetUserPermissionsAsync_WhenCachedInL1_ReturnsFromL1Cache()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedPermissions = new List<string> { "Users.Read", "Users.Create" };
        var cacheKey = $"user_permissions:{userId}";

        object cachedValue = expectedPermissions;
        _mockMemoryCache.Setup(x => x.TryGetValue(cacheKey, out cachedValue))
            .Returns(true);

        // Act
        var result = await _service.GetUserPermissionsAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedPermissions, result);
        _mockDistributedCache.Verify(x => x.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetUserPermissionsAsync_WhenNotInL1ButInL2_ReturnsFromL2AndCachesInL1()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedPermissions = new List<string> { "Users.Read", "Users.Create" };
        var cacheKey = $"user_permissions:{userId}";
        var serializedPermissions = JsonSerializer.Serialize(expectedPermissions);

        object cachedValue = null!;
        _mockMemoryCache.Setup(x => x.TryGetValue(cacheKey, out cachedValue))
            .Returns(false);

        _mockDistributedCache.Setup(x => x.GetStringAsync(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serializedPermissions);

        // Act
        var result = await _service.GetUserPermissionsAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedPermissions, result);
        _mockMemoryCache.Verify(x => x.Set(cacheKey, It.IsAny<IEnumerable<string>>(), _options.L1CacheExpiry), Times.Once);
    }

    [Fact]
    public async Task GetUserPermissionsAsync_WhenNotCached_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cacheKey = $"user_permissions:{userId}";

        object cachedValue = null!;
        _mockMemoryCache.Setup(x => x.TryGetValue(cacheKey, out cachedValue))
            .Returns(false);

        _mockDistributedCache.Setup(x => x.GetStringAsync(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _service.GetUserPermissionsAsync(userId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SetUserPermissionsAsync_CachesInBothL1AndL2()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissions = new List<string> { "Users.Read", "Users.Create" };
        var cacheKey = $"user_permissions:{userId}";
        var serializedPermissions = JsonSerializer.Serialize(permissions);

        // Act
        await _service.SetUserPermissionsAsync(userId, permissions);

        // Assert
        _mockMemoryCache.Verify(x => x.Set(cacheKey, permissions, _options.L1CacheExpiry), Times.Once);
        _mockDistributedCache.Verify(x => x.SetStringAsync(
            cacheKey, 
            serializedPermissions, 
            It.IsAny<DistributedCacheEntryOptions>(), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetRolePermissionsAsync_WhenCachedInL1_ReturnsFromL1Cache()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var expectedPermissions = new List<string> { "Users.Read", "Users.Create" };
        var cacheKey = $"role_permissions:{roleId}";

        object cachedValue = expectedPermissions;
        _mockMemoryCache.Setup(x => x.TryGetValue(cacheKey, out cachedValue))
            .Returns(true);

        // Act
        var result = await _service.GetRolePermissionsAsync(roleId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedPermissions, result);
    }

    [Fact]
    public async Task GetPermissionEvaluationAsync_WhenCachedInL1_ReturnsFromL1Cache()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permission = "Users.Read";
        var expectedResult = true;
        var cacheKey = $"permission_eval:{userId}:{permission}";

        object cachedValue = expectedResult;
        _mockMemoryCache.Setup(x => x.TryGetValue(cacheKey, out cachedValue))
            .Returns(true);

        // Act
        var result = await _service.GetPermissionEvaluationAsync(userId, permission);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public async Task SetPermissionEvaluationAsync_CachesInBothL1AndL2()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permission = "Users.Read";
        var hasPermission = true;
        var cacheKey = $"permission_eval:{userId}:{permission}";
        var serializedResult = JsonSerializer.Serialize(hasPermission);

        // Act
        await _service.SetPermissionEvaluationAsync(userId, permission, hasPermission);

        // Assert
        _mockMemoryCache.Verify(x => x.Set(cacheKey, hasPermission, _options.L1CacheExpiry), Times.Once);
        _mockDistributedCache.Verify(x => x.SetStringAsync(
            cacheKey, 
            serializedResult, 
            It.IsAny<DistributedCacheEntryOptions>(), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvalidateUserPermissionsAsync_RemovesFromBothCaches()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cacheKey = $"user_permissions:{userId}";

        // Act
        await _service.InvalidateUserPermissionsAsync(userId);

        // Assert
        _mockMemoryCache.Verify(x => x.Remove(cacheKey), Times.Once);
        _mockDistributedCache.Verify(x => x.RemoveAsync(cacheKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvalidateRolePermissionsAsync_RemovesFromBothCaches()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var cacheKey = $"role_permissions:{roleId}";

        // Act
        await _service.InvalidateRolePermissionsAsync(roleId);

        // Assert
        _mockMemoryCache.Verify(x => x.Remove(cacheKey), Times.Once);
        _mockDistributedCache.Verify(x => x.RemoveAsync(cacheKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvalidateUsersWithRoleAsync_InvalidatesAllUsersWithRole()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var user1 = User.Create(Email.Create("user1@test.com"), "User1", "Test", "hashedPassword");
        var user2 = User.Create(Email.Create("user2@test.com"), "User2", "Test", "hashedPassword");
        var usersWithRole = new List<User> { user1, user2 };

        _mockUserRepository.Setup(x => x.GetUsersByRoleAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usersWithRole);

        // Act
        await _service.InvalidateUsersWithRoleAsync(roleId);

        // Assert
        _mockMemoryCache.Verify(x => x.Remove($"user_permissions:{user1.Id}"), Times.Once);
        _mockMemoryCache.Verify(x => x.Remove($"user_permissions:{user2.Id}"), Times.Once);
        _mockMemoryCache.Verify(x => x.Remove($"role_permissions:{roleId}"), Times.Once);
        
        _mockDistributedCache.Verify(x => x.RemoveAsync($"user_permissions:{user1.Id}", It.IsAny<CancellationToken>()), Times.Once);
        _mockDistributedCache.Verify(x => x.RemoveAsync($"user_permissions:{user2.Id}", It.IsAny<CancellationToken>()), Times.Once);
        _mockDistributedCache.Verify(x => x.RemoveAsync($"role_permissions:{roleId}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task WarmUpUserPermissionsAsync_WhenNotCached_LoadsAndCachesPermissions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissions = new List<string> { "Users.Read", "Users.Create" };
        var cacheKey = $"user_permissions:{userId}";

        // Setup cache miss
        object cachedValue = null!;
        _mockMemoryCache.Setup(x => x.TryGetValue(cacheKey, out cachedValue))
            .Returns(false);
        _mockDistributedCache.Setup(x => x.GetStringAsync(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Setup user and entities
        var user = User.Create(Email.Create("test@example.com"), "Test", "User", "hashedPassword");
        var roles = new List<Role>();
        var availablePermissions = new List<Permission>();
        
        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockRoleRepository.Setup(x => x.GetUserRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);
        _mockPermissionRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(availablePermissions);
        
        // Setup permission evaluation service
        _mockPermissionEvaluationService.Setup(x => x.GetUserPermissions(user, availablePermissions, roles))
            .Returns(permissions);
        
        _mockPermissionEvaluationService.Setup(x => x.HasPermission(user, "Users.Read", availablePermissions, roles))
            .Returns(true);
        _mockPermissionEvaluationService.Setup(x => x.HasPermission(user, "Users.Create", availablePermissions, roles))
            .Returns(true);

        // Act
        await _service.WarmUpUserPermissionsAsync(userId);

        // Assert
        _mockPermissionEvaluationService.Verify(x => x.GetUserPermissions(user, availablePermissions, roles), Times.Once);
        _mockMemoryCache.Verify(x => x.Set(cacheKey, permissions, _options.L1CacheExpiry), Times.Once);
        
        // Verify common permissions are pre-cached
        _mockPermissionEvaluationService.Verify(x => x.HasPermission(user, "Users.Read", availablePermissions, roles), Times.Once);
        _mockPermissionEvaluationService.Verify(x => x.HasPermission(user, "Users.Create", availablePermissions, roles), Times.Once);
    }

    [Fact]
    public async Task WarmUpUserPermissionsAsync_WhenAlreadyCached_DoesNotReload()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissions = new List<string> { "Users.Read", "Users.Create" };
        var cacheKey = $"user_permissions:{userId}";

        // Setup cache hit
        object cachedValue = permissions;
        _mockMemoryCache.Setup(x => x.TryGetValue(cacheKey, out cachedValue))
            .Returns(true);

        // Act
        await _service.WarmUpUserPermissionsAsync(userId);

        // Assert
        _mockUserRepository.Verify(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetCacheStatisticsAsync_ReturnsStatistics()
    {
        // Act
        var result = await _service.GetCacheStatisticsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.HitCount >= 0);
        Assert.True(result.MissCount >= 0);
        Assert.True(result.HitRatio >= 0.0 && result.HitRatio <= 1.0);
    }

    [Fact]
    public async Task InvalidatePermissionEvaluationAsync_RemovesFromBothCaches()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permission = "Users.Read";
        var cacheKey = $"permission_eval:{userId}:{permission}";

        // Act
        await _service.InvalidatePermissionEvaluationAsync(userId, permission);

        // Assert
        _mockMemoryCache.Verify(x => x.Remove(cacheKey), Times.Once);
        _mockDistributedCache.Verify(x => x.RemoveAsync(cacheKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetUserPermissionsAsync_WithCustomExpiry_UsesCustomExpiry()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissions = new List<string> { "Users.Read" };
        var customExpiry = TimeSpan.FromMinutes(30);
        var cacheKey = $"user_permissions:{userId}";

        // Act
        await _service.SetUserPermissionsAsync(userId, permissions, customExpiry);

        // Assert
        _mockDistributedCache.Verify(x => x.SetStringAsync(
            cacheKey,
            It.IsAny<string>(),
            It.Is<DistributedCacheEntryOptions>(opts => opts.AbsoluteExpirationRelativeToNow == customExpiry),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetUserPermissionsAsync_WhenExceptionOccurs_ReturnsNullAndLogsError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cacheKey = $"user_permissions:{userId}";

        _mockMemoryCache.Setup(x => x.TryGetValue(cacheKey, out It.Ref<object>.IsAny))
            .Throws(new Exception("Cache error"));

        // Act
        var result = await _service.GetUserPermissionsAsync(userId);

        // Assert
        Assert.Null(result);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error retrieving user permissions from cache")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}