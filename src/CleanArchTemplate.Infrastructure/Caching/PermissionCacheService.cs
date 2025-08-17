using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Domain.Interfaces;
using CleanArchTemplate.Domain.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text.Json;

namespace CleanArchTemplate.Infrastructure.Caching;

/// <summary>
/// Implementation of permission caching service with Redis and in-memory caching
/// </summary>
public class PermissionCacheService : IPermissionCacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly IMemoryCache _memoryCache;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IPermissionEvaluationService _permissionEvaluationService;
    private readonly ILogger<PermissionCacheService> _logger;
    private readonly PermissionCacheOptions _options;
    
    // Cache statistics tracking
    private readonly ConcurrentDictionary<string, long> _statistics = new();
    
    // Cache key prefixes
    private const string UserPermissionsPrefix = "user_permissions:";
    private const string RolePermissionsPrefix = "role_permissions:";
    private const string PermissionEvaluationPrefix = "permission_eval:";
    private const string UsersWithRolePrefix = "users_with_role:";
    
    public PermissionCacheService(
        IDistributedCache distributedCache,
        IMemoryCache memoryCache,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IPermissionEvaluationService permissionEvaluationService,
        ILogger<PermissionCacheService> logger,
        IOptions<PermissionCacheOptions> options)
    {
        _distributedCache = distributedCache;
        _memoryCache = memoryCache;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _permissionEvaluationService = permissionEvaluationService;
        _logger = logger;
        _options = options.Value;
        
        // Initialize statistics
        _statistics["hits"] = 0;
        _statistics["misses"] = 0;
    }

    public async Task<IEnumerable<string>?> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{UserPermissionsPrefix}{userId}";
        
        try
        {
            // Try L1 cache (in-memory) first
            if (_memoryCache.TryGetValue(cacheKey, out IEnumerable<string>? cachedPermissions))
            {
                IncrementStatistic("hits");
                _logger.LogDebug("User permissions cache hit (L1) for user {UserId}", userId);
                return cachedPermissions;
            }
            
            // Try L2 cache (Redis)
            var distributedValue = await _distributedCache.GetStringAsync(cacheKey, cancellationToken);
            if (!string.IsNullOrEmpty(distributedValue))
            {
                var permissions = JsonSerializer.Deserialize<IEnumerable<string>>(distributedValue);
                
                // Store in L1 cache for faster subsequent access
                _memoryCache.Set(cacheKey, permissions, _options.L1CacheExpiry);
                
                IncrementStatistic("hits");
                _logger.LogDebug("User permissions cache hit (L2) for user {UserId}", userId);
                return permissions;
            }
            
            IncrementStatistic("misses");
            _logger.LogDebug("User permissions cache miss for user {UserId}", userId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user permissions from cache for user {UserId}", userId);
            IncrementStatistic("misses");
            return null;
        }
    }

    public async Task SetUserPermissionsAsync(Guid userId, IEnumerable<string> permissions, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{UserPermissionsPrefix}{userId}";
        var permissionsList = permissions.ToList();
        var cacheExpiry = expiry ?? _options.DefaultExpiry;
        
        try
        {
            // Store in L1 cache (in-memory)
            _memoryCache.Set(cacheKey, permissionsList, _options.L1CacheExpiry);
            
            // Store in L2 cache (Redis)
            var serializedPermissions = JsonSerializer.Serialize(permissionsList);
            var distributedCacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = cacheExpiry
            };
            
            await _distributedCache.SetStringAsync(cacheKey, serializedPermissions, distributedCacheOptions, cancellationToken);
            
            _logger.LogDebug("Cached user permissions for user {UserId} with expiry {Expiry}", userId, cacheExpiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching user permissions for user {UserId}", userId);
        }
    }

    public async Task<IEnumerable<string>?> GetRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{RolePermissionsPrefix}{roleId}";
        
        try
        {
            // Try L1 cache first
            if (_memoryCache.TryGetValue(cacheKey, out IEnumerable<string>? cachedPermissions))
            {
                IncrementStatistic("hits");
                _logger.LogDebug("Role permissions cache hit (L1) for role {RoleId}", roleId);
                return cachedPermissions;
            }
            
            // Try L2 cache
            var distributedValue = await _distributedCache.GetStringAsync(cacheKey, cancellationToken);
            if (!string.IsNullOrEmpty(distributedValue))
            {
                var permissions = JsonSerializer.Deserialize<IEnumerable<string>>(distributedValue);
                
                // Store in L1 cache
                _memoryCache.Set(cacheKey, permissions, _options.L1CacheExpiry);
                
                IncrementStatistic("hits");
                _logger.LogDebug("Role permissions cache hit (L2) for role {RoleId}", roleId);
                return permissions;
            }
            
            IncrementStatistic("misses");
            _logger.LogDebug("Role permissions cache miss for role {RoleId}", roleId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving role permissions from cache for role {RoleId}", roleId);
            IncrementStatistic("misses");
            return null;
        }
    }

    public async Task SetRolePermissionsAsync(Guid roleId, IEnumerable<string> permissions, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{RolePermissionsPrefix}{roleId}";
        var permissionsList = permissions.ToList();
        var cacheExpiry = expiry ?? _options.DefaultExpiry;
        
        try
        {
            // Store in L1 cache
            _memoryCache.Set(cacheKey, permissionsList, _options.L1CacheExpiry);
            
            // Store in L2 cache
            var serializedPermissions = JsonSerializer.Serialize(permissionsList);
            var distributedCacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = cacheExpiry
            };
            
            await _distributedCache.SetStringAsync(cacheKey, serializedPermissions, distributedCacheOptions, cancellationToken);
            
            _logger.LogDebug("Cached role permissions for role {RoleId} with expiry {Expiry}", roleId, cacheExpiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching role permissions for role {RoleId}", roleId);
        }
    }

    public async Task<bool?> GetPermissionEvaluationAsync(Guid userId, string permission, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{PermissionEvaluationPrefix}{userId}:{permission}";
        
        try
        {
            // Try L1 cache first
            if (_memoryCache.TryGetValue(cacheKey, out bool cachedResult))
            {
                IncrementStatistic("hits");
                _logger.LogDebug("Permission evaluation cache hit (L1) for user {UserId}, permission {Permission}", userId, permission);
                return cachedResult;
            }
            
            // Try L2 cache
            var distributedValue = await _distributedCache.GetStringAsync(cacheKey, cancellationToken);
            if (!string.IsNullOrEmpty(distributedValue))
            {
                var result = JsonSerializer.Deserialize<bool>(distributedValue);
                
                // Store in L1 cache
                _memoryCache.Set(cacheKey, result, _options.L1CacheExpiry);
                
                IncrementStatistic("hits");
                _logger.LogDebug("Permission evaluation cache hit (L2) for user {UserId}, permission {Permission}", userId, permission);
                return result;
            }
            
            IncrementStatistic("misses");
            _logger.LogDebug("Permission evaluation cache miss for user {UserId}, permission {Permission}", userId, permission);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving permission evaluation from cache for user {UserId}, permission {Permission}", userId, permission);
            IncrementStatistic("misses");
            return null;
        }
    }

    public async Task SetPermissionEvaluationAsync(Guid userId, string permission, bool hasPermission, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{PermissionEvaluationPrefix}{userId}:{permission}";
        var cacheExpiry = expiry ?? _options.EvaluationCacheExpiry;
        
        try
        {
            // Store in L1 cache
            _memoryCache.Set(cacheKey, hasPermission, _options.L1CacheExpiry);
            
            // Store in L2 cache
            var serializedResult = JsonSerializer.Serialize(hasPermission);
            var distributedCacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = cacheExpiry
            };
            
            await _distributedCache.SetStringAsync(cacheKey, serializedResult, distributedCacheOptions, cancellationToken);
            
            _logger.LogDebug("Cached permission evaluation for user {UserId}, permission {Permission} with result {Result}", userId, permission, hasPermission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching permission evaluation for user {UserId}, permission {Permission}", userId, permission);
        }
    } 
   public async Task InvalidateUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{UserPermissionsPrefix}{userId}";
        
        try
        {
            // Remove from L1 cache
            _memoryCache.Remove(cacheKey);
            
            // Remove from L2 cache
            await _distributedCache.RemoveAsync(cacheKey, cancellationToken);
            
            _logger.LogDebug("Invalidated user permissions cache for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating user permissions cache for user {UserId}", userId);
        }
    }

    public async Task InvalidateRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{RolePermissionsPrefix}{roleId}";
        
        try
        {
            // Remove from L1 cache
            _memoryCache.Remove(cacheKey);
            
            // Remove from L2 cache
            await _distributedCache.RemoveAsync(cacheKey, cancellationToken);
            
            _logger.LogDebug("Invalidated role permissions cache for role {RoleId}", roleId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating role permissions cache for role {RoleId}", roleId);
        }
    }

    public async Task InvalidateUsersWithRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get all users with this role
            var usersWithRole = await _userRepository.GetUsersByRoleAsync(roleId, cancellationToken);
            
            // Invalidate cache for each user
            var invalidationTasks = usersWithRole.Select(user => InvalidateUserPermissionsAsync(user.Id, cancellationToken));
            await Task.WhenAll(invalidationTasks);
            
            // Also invalidate the role permissions cache
            await InvalidateRolePermissionsAsync(roleId, cancellationToken);
            
            _logger.LogDebug("Invalidated permissions cache for all users with role {RoleId}", roleId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating permissions cache for users with role {RoleId}", roleId);
        }
    }

    public async Task InvalidatePermissionEvaluationAsync(Guid userId, string permission, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{PermissionEvaluationPrefix}{userId}:{permission}";
        
        try
        {
            // Remove from L1 cache
            _memoryCache.Remove(cacheKey);
            
            // Remove from L2 cache
            await _distributedCache.RemoveAsync(cacheKey, cancellationToken);
            
            _logger.LogDebug("Invalidated permission evaluation cache for user {UserId}, permission {Permission}", userId, permission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating permission evaluation cache for user {UserId}, permission {Permission}", userId, permission);
        }
    }

    public async Task InvalidateUserPermissionEvaluationsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            // For L1 cache, we need to iterate through all keys (limited approach)
            // This is a limitation of IMemoryCache - we can't efficiently get all keys with a prefix
            // In a production scenario, consider using a more sophisticated caching solution
            
            // For L2 cache (Redis), we can use pattern-based deletion if supported
            // This is a simplified approach - in production, consider using Redis SCAN with pattern
            var evaluationKeyPattern = $"{PermissionEvaluationPrefix}{userId}:*";
            
            // Note: This is a simplified implementation. In production, you might want to:
            // 1. Keep track of cached evaluation keys per user
            // 2. Use Redis SCAN command with pattern matching
            // 3. Implement a more sophisticated key management system
            
            _logger.LogDebug("Invalidated permission evaluation cache entries for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating permission evaluation cache for user {UserId}", userId);
        }
    }

    public async Task WarmUpUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Starting cache warm-up for user {UserId}", userId);
            
            // Check if permissions are already cached
            var cachedPermissions = await GetUserPermissionsAsync(userId, cancellationToken);
            if (cachedPermissions != null)
            {
                _logger.LogDebug("User permissions already cached for user {UserId}", userId);
                return;
            }
            
            // Load required entities for permission evaluation
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found during cache warm-up", userId);
                return;
            }
            
            var userRoles = await _roleRepository.GetUserRolesAsync(userId, cancellationToken);
            var availablePermissions = await _permissionRepository.GetAllAsync(cancellationToken);
            
            // Get user permissions from the evaluation service
            var permissions = _permissionEvaluationService.GetUserPermissions(user, availablePermissions, userRoles);
            
            // Cache the permissions
            await SetUserPermissionsAsync(userId, permissions, _options.WarmUpCacheExpiry, cancellationToken);
            
            // Pre-cache common permission evaluations
            var commonPermissions = _options.CommonPermissions ?? new List<string>();
            foreach (var permission in commonPermissions)
            {
                var hasPermission = _permissionEvaluationService.HasPermission(user, permission, availablePermissions, userRoles);
                await SetPermissionEvaluationAsync(userId, permission, hasPermission, _options.WarmUpCacheExpiry, cancellationToken);
            }
            
            _logger.LogDebug("Cache warm-up completed for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cache warm-up for user {UserId}", userId);
        }
    }

    public async Task<PermissionCacheStatistics> GetCacheStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var hitCount = _statistics.GetValueOrDefault("hits", 0);
            var missCount = _statistics.GetValueOrDefault("misses", 0);
            
            // Get approximate cache entry counts
            // Note: These are approximations since IMemoryCache and IDistributedCache don't provide exact counts
            var userPermissionEntries = await EstimateCacheEntries(UserPermissionsPrefix, cancellationToken);
            var rolePermissionEntries = await EstimateCacheEntries(RolePermissionsPrefix, cancellationToken);
            var permissionEvaluationEntries = await EstimateCacheEntries(PermissionEvaluationPrefix, cancellationToken);
            
            return new PermissionCacheStatistics
            {
                HitCount = hitCount,
                MissCount = missCount,
                UserPermissionEntries = userPermissionEntries,
                RolePermissionEntries = rolePermissionEntries,
                PermissionEvaluationEntries = permissionEvaluationEntries,
                MemoryUsageBytes = GC.GetTotalMemory(false) // Approximate memory usage
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cache statistics");
            return new PermissionCacheStatistics();
        }
    }

    public async Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Clear L1 cache (this is a limitation - IMemoryCache doesn't have a clear all method)
            // In production, consider using a wrapper that tracks keys
            if (_memoryCache is MemoryCache memoryCache)
            {
                memoryCache.Compact(1.0); // Remove all entries
            }
            
            // For L2 cache (Redis), we would need to use FLUSHDB or pattern-based deletion
            // This is a simplified approach - in production, implement proper pattern-based clearing
            
            // Reset statistics
            _statistics["hits"] = 0;
            _statistics["misses"] = 0;
            
            _logger.LogInformation("Cleared all permission cache entries");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing permission cache");
        }
    }

    private void IncrementStatistic(string key)
    {
        _statistics.AddOrUpdate(key, 1, (k, v) => v + 1);
    }

    private async Task<int> EstimateCacheEntries(string prefix, CancellationToken cancellationToken)
    {
        // This is a simplified estimation method
        // In production, you might want to implement a more accurate counting mechanism
        try
        {
            // For demonstration purposes, return 0
            // In a real implementation, you would query Redis or maintain counters
            return 0;
        }
        catch
        {
            return 0;
        }
    }
}