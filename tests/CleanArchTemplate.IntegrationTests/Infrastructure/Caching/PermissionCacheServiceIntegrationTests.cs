using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.Interfaces;
using CleanArchTemplate.Domain.Services;
using CleanArchTemplate.Domain.ValueObjects;
using CleanArchTemplate.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace CleanArchTemplate.IntegrationTests.Infrastructure.Caching;

[Collection("Integration")]
public class PermissionCacheServiceIntegrationTests : IAsyncDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly PermissionCacheService _service;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IRoleRepository> _mockRoleRepository;
    private readonly Mock<IPermissionRepository> _mockPermissionRepository;
    private readonly Mock<IPermissionEvaluationService> _mockPermissionEvaluationService;

    public PermissionCacheServiceIntegrationTests()
    {
        // Setup Redis connection for testing
        var connectionString = "localhost:6379"; // Adjust for your test environment
        _redis = ConnectionMultiplexer.Connect(connectionString);
        _database = _redis.GetDatabase();

        // Setup service collection
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole());
        
        // Add memory cache
        services.AddMemoryCache();
        
        // Add Redis distributed cache
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = connectionString;
        });

        // Configure cache options
        var cacheOptions = new PermissionCacheOptions
        {
            DefaultExpiry = TimeSpan.FromMinutes(15),
            L1CacheExpiry = TimeSpan.FromMinutes(5),
            EvaluationCacheExpiry = TimeSpan.FromMinutes(10),
            WarmUpCacheExpiry = TimeSpan.FromMinutes(30),
            CommonPermissions = new List<string> { "Users.Read", "Users.Create", "Users.Update" }
        };
        services.Configure<PermissionCacheOptions>(opts =>
        {
            opts.DefaultExpiry = cacheOptions.DefaultExpiry;
            opts.L1CacheExpiry = cacheOptions.L1CacheExpiry;
            opts.EvaluationCacheExpiry = cacheOptions.EvaluationCacheExpiry;
            opts.WarmUpCacheExpiry = cacheOptions.WarmUpCacheExpiry;
            opts.CommonPermissions = cacheOptions.CommonPermissions;
        });

        // Setup mocks
        _mockUserRepository = new Mock<IUserRepository>();
        _mockRoleRepository = new Mock<IRoleRepository>();
        _mockPermissionRepository = new Mock<IPermissionRepository>();
        _mockPermissionEvaluationService = new Mock<IPermissionEvaluationService>();

        services.AddSingleton(_mockUserRepository.Object);
        services.AddSingleton(_mockRoleRepository.Object);
        services.AddSingleton(_mockPermissionRepository.Object);
        services.AddSingleton(_mockPermissionEvaluationService.Object);

        // Add the cache service
        services.AddScoped<PermissionCacheService>();

        _serviceProvider = services.BuildServiceProvider();
        _service = _serviceProvider.GetRequiredService<PermissionCacheService>();

        // Clear any existing test data
        ClearTestData().Wait();
    }

    [Fact]
    public async Task UserPermissions_EndToEndCaching_WorksCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissions = new List<string> { "Users.Read", "Users.Create", "Users.Update" };

        // Act & Assert - Cache miss initially
        var cachedPermissions = await _service.GetUserPermissionsAsync(userId);
        Assert.Null(cachedPermissions);

        // Set permissions in cache
        await _service.SetUserPermissionsAsync(userId, permissions);

        // Verify L1 cache hit
        var l1CachedPermissions = await _service.GetUserPermissionsAsync(userId);
        Assert.NotNull(l1CachedPermissions);
        Assert.Equal(permissions.Count, l1CachedPermissions.Count());
        Assert.All(permissions, p => Assert.Contains(p, l1CachedPermissions));

        // Clear L1 cache to test L2 cache
        var memoryCache = _serviceProvider.GetRequiredService<IMemoryCache>();
        memoryCache.Remove($"user_permissions:{userId}");

        // Verify L2 cache hit
        var l2CachedPermissions = await _service.GetUserPermissionsAsync(userId);
        Assert.NotNull(l2CachedPermissions);
        Assert.Equal(permissions.Count, l2CachedPermissions.Count());
        Assert.All(permissions, p => Assert.Contains(p, l2CachedPermissions));

        // Invalidate cache
        await _service.InvalidateUserPermissionsAsync(userId);

        // Verify cache is cleared
        var clearedPermissions = await _service.GetUserPermissionsAsync(userId);
        Assert.Null(clearedPermissions);
    }

    [Fact]
    public async Task RolePermissions_EndToEndCaching_WorksCorrectly()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var permissions = new List<string> { "Roles.Read", "Roles.Create" };

        // Act & Assert - Cache miss initially
        var cachedPermissions = await _service.GetRolePermissionsAsync(roleId);
        Assert.Null(cachedPermissions);

        // Set permissions in cache
        await _service.SetRolePermissionsAsync(roleId, permissions);

        // Verify cache hit
        var retrievedPermissions = await _service.GetRolePermissionsAsync(roleId);
        Assert.NotNull(retrievedPermissions);
        Assert.Equal(permissions.Count, retrievedPermissions.Count());
        Assert.All(permissions, p => Assert.Contains(p, retrievedPermissions));

        // Invalidate cache
        await _service.InvalidateRolePermissionsAsync(roleId);

        // Verify cache is cleared
        var clearedPermissions = await _service.GetRolePermissionsAsync(roleId);
        Assert.Null(clearedPermissions);
    }

    [Fact]
    public async Task PermissionEvaluation_EndToEndCaching_WorksCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permission = "Users.Read";
        var hasPermission = true;

        // Act & Assert - Cache miss initially
        var cachedEvaluation = await _service.GetPermissionEvaluationAsync(userId, permission);
        Assert.Null(cachedEvaluation);

        // Set evaluation in cache
        await _service.SetPermissionEvaluationAsync(userId, permission, hasPermission);

        // Verify cache hit
        var retrievedEvaluation = await _service.GetPermissionEvaluationAsync(userId, permission);
        Assert.NotNull(retrievedEvaluation);
        Assert.Equal(hasPermission, retrievedEvaluation.Value);

        // Invalidate cache
        await _service.InvalidatePermissionEvaluationAsync(userId, permission);

        // Verify cache is cleared
        var clearedEvaluation = await _service.GetPermissionEvaluationAsync(userId, permission);
        Assert.Null(clearedEvaluation);
    }

    [Fact]
    public async Task WarmUpUserPermissions_WithRealCache_WorksCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissions = new List<string> { "Users.Read", "Users.Create", "Users.Update" };

        // Setup user and entities
        var user = User.Create(Email.Create("test@example.com"), "Test", "User", "hashedPassword");
        var roles = new List<CleanArchTemplate.Domain.Entities.Role>();
        var availablePermissions = new List<Permission>();
        
        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockRoleRepository.Setup(x => x.GetUserRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<CleanArchTemplate.Domain.Entities.Role>)roles);
        _mockPermissionRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(availablePermissions);

        _mockPermissionEvaluationService
            .Setup(x => x.GetUserPermissions(user, availablePermissions, (IEnumerable<CleanArchTemplate.Domain.Entities.Role>)roles))
            .Returns(permissions);

        _mockPermissionEvaluationService
            .Setup(x => x.HasPermission(user, "Users.Read", availablePermissions, (IEnumerable<CleanArchTemplate.Domain.Entities.Role>)roles))
            .Returns(true);

        _mockPermissionEvaluationService
            .Setup(x => x.HasPermission(user, "Users.Create", availablePermissions, (IEnumerable<CleanArchTemplate.Domain.Entities.Role>)roles))
            .Returns(true);

        _mockPermissionEvaluationService
            .Setup(x => x.HasPermission(user, "Users.Update", availablePermissions, (IEnumerable<CleanArchTemplate.Domain.Entities.Role>)roles))
            .Returns(false);

        // Act
        await _service.WarmUpUserPermissionsAsync(userId);

        // Assert - Verify permissions are cached
        var cachedPermissions = await _service.GetUserPermissionsAsync(userId);
        Assert.NotNull(cachedPermissions);
        Assert.Equal(permissions.Count, cachedPermissions.Count());

        // Verify common permission evaluations are cached
        var readEvaluation = await _service.GetPermissionEvaluationAsync(userId, "Users.Read");
        Assert.NotNull(readEvaluation);
        Assert.True(readEvaluation.Value);

        var createEvaluation = await _service.GetPermissionEvaluationAsync(userId, "Users.Create");
        Assert.NotNull(createEvaluation);
        Assert.True(createEvaluation.Value);

        var updateEvaluation = await _service.GetPermissionEvaluationAsync(userId, "Users.Update");
        Assert.NotNull(updateEvaluation);
        Assert.False(updateEvaluation.Value);
    }

    [Fact]
    public async Task InvalidateUsersWithRole_WithRealCache_WorksCorrectly()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var user1 = User.Create(Email.Create("user1@test.com"), "User1", "Test", "hashedPassword");
        var user2 = User.Create(Email.Create("user2@test.com"), "User2", "Test", "hashedPassword");
        var usersWithRole = new List<User> { user1, user2 };

        _mockUserRepository
            .Setup(x => x.GetUsersByRoleAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usersWithRole);

        // Cache permissions for both users and the role
        await _service.SetUserPermissionsAsync(user1.Id, new[] { "Users.Read" });
        await _service.SetUserPermissionsAsync(user2.Id, new[] { "Users.Create" });
        await _service.SetRolePermissionsAsync(roleId, new[] { "Roles.Read" });

        // Verify permissions are cached
        Assert.NotNull(await _service.GetUserPermissionsAsync(user1.Id));
        Assert.NotNull(await _service.GetUserPermissionsAsync(user2.Id));
        Assert.NotNull(await _service.GetRolePermissionsAsync(roleId));

        // Act
        await _service.InvalidateUsersWithRoleAsync(roleId);

        // Assert - All caches should be cleared
        Assert.Null(await _service.GetUserPermissionsAsync(user1.Id));
        Assert.Null(await _service.GetUserPermissionsAsync(user2.Id));
        Assert.Null(await _service.GetRolePermissionsAsync(roleId));
    }

    [Fact]
    public async Task CacheExpiry_WorksCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissions = new List<string> { "Users.Read" };
        var shortExpiry = TimeSpan.FromMilliseconds(100);

        // Act
        await _service.SetUserPermissionsAsync(userId, permissions, shortExpiry);

        // Verify cache hit immediately
        var cachedPermissions = await _service.GetUserPermissionsAsync(userId);
        Assert.NotNull(cachedPermissions);

        // Wait for expiry
        await Task.Delay(200);

        // Clear L1 cache to test L2 expiry
        var memoryCache = _serviceProvider.GetRequiredService<IMemoryCache>();
        memoryCache.Remove($"user_permissions:{userId}");

        // Verify cache miss after expiry
        var expiredPermissions = await _service.GetUserPermissionsAsync(userId);
        Assert.Null(expiredPermissions);
    }

    [Fact]
    public async Task ConcurrentAccess_HandledCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissions = new List<string> { "Users.Read", "Users.Create" };

        // Act - Concurrent cache operations
        var tasks = new List<Task>();
        
        // Set permissions concurrently
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_service.SetUserPermissionsAsync(userId, permissions));
        }

        // Get permissions concurrently
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                var result = await _service.GetUserPermissionsAsync(userId);
                // Result can be null or the permissions, both are valid during concurrent operations
            }));
        }

        // Wait for all operations to complete
        await Task.WhenAll(tasks);

        // Assert - Final state should be consistent
        var finalPermissions = await _service.GetUserPermissionsAsync(userId);
        Assert.NotNull(finalPermissions);
        Assert.Equal(permissions.Count, finalPermissions.Count());
    }

    private async Task ClearTestData()
    {
        try
        {
            // Clear Redis test data
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var keys = server.Keys(pattern: "*").ToArray();
            if (keys.Length > 0)
            {
                await _database.KeyDeleteAsync(keys);
            }
        }
        catch
        {
            // Ignore errors during cleanup
        }
    }

    public async ValueTask DisposeAsync()
    {
        await ClearTestData();
        await _redis.DisposeAsync();
        await _serviceProvider.DisposeAsync();
    }
}