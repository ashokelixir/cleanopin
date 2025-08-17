using System.Security.Claims;
using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Application.Common.Models;
using CleanArchTemplate.Domain.Interfaces;
using CleanArchTemplate.Domain.Services;
using Microsoft.Extensions.Logging;

namespace CleanArchTemplate.Infrastructure.Services;

/// <summary>
/// Implementation of permission-based authorization service for ClaimsPrincipal authorization
/// </summary>
public class PermissionAuthorizationService : IPermissionAuthorizationService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IUserPermissionRepository _userPermissionRepository;
    private readonly IPermissionEvaluationService _permissionEvaluationService;
    private readonly IPermissionCacheService _permissionCacheService;
    private readonly ILogger<PermissionAuthorizationService> _logger;

    public PermissionAuthorizationService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IUserPermissionRepository userPermissionRepository,
        IPermissionEvaluationService permissionEvaluationService,
        IPermissionCacheService permissionCacheService,
        ILogger<PermissionAuthorizationService> logger)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _userPermissionRepository = userPermissionRepository;
        _permissionEvaluationService = permissionEvaluationService;
        _permissionCacheService = permissionCacheService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, string resource, string action, CancellationToken cancellationToken = default)
    {
        if (user == null)
        {
            _logger.LogWarning("Authorization failed: User principal is null");
            return AuthorizationResult.Failure("", "User principal is null", new List<string>());
        }

        if (string.IsNullOrWhiteSpace(resource))
        {
            _logger.LogWarning("Authorization failed: Resource is null or empty");
            return AuthorizationResult.Failure("", "Resource cannot be null or empty", new List<string>());
        }

        if (string.IsNullOrWhiteSpace(action))
        {
            _logger.LogWarning("Authorization failed: Action is null or empty");
            return AuthorizationResult.Failure("", "Action cannot be null or empty", new List<string>());
        }

        var permissionName = $"{resource.Trim()}.{action.Trim()}";
        return await AuthorizeAsync(user, permissionName, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, string permission, CancellationToken cancellationToken = default)
    {
        if (user == null)
        {
            _logger.LogWarning("Authorization failed: User principal is null");
            return AuthorizationResult.Failure(permission, "User principal is null", new List<string>());
        }

        if (string.IsNullOrWhiteSpace(permission))
        {
            _logger.LogWarning("Authorization failed: Permission is null or empty");
            return AuthorizationResult.Failure("", "Permission cannot be null or empty", new List<string>());
        }

        var userId = GetUserIdFromClaims(user);
        if (userId == null)
        {
            _logger.LogWarning("Authorization failed: Unable to extract user ID from claims");
            return AuthorizationResult.Failure(permission, "Unable to extract user ID from claims", new List<string>());
        }

        return await AuthorizeAsync(userId.Value, permission, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AuthorizationResult> AuthorizeAsync(Guid userId, string resource, string action, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(resource))
        {
            _logger.LogWarning("Authorization failed: Resource is null or empty for user {UserId}", userId);
            return AuthorizationResult.Failure("", "Resource cannot be null or empty", new List<string>());
        }

        if (string.IsNullOrWhiteSpace(action))
        {
            _logger.LogWarning("Authorization failed: Action is null or empty for user {UserId}", userId);
            return AuthorizationResult.Failure("", "Action cannot be null or empty", new List<string>());
        }

        var permissionName = $"{resource.Trim()}.{action.Trim()}";
        return await AuthorizeAsync(userId, permissionName, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AuthorizationResult> AuthorizeAsync(Guid userId, string permission, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(permission))
        {
            _logger.LogWarning("Authorization failed: Permission is null or empty for user {UserId}", userId);
            return AuthorizationResult.Failure("", "Permission cannot be null or empty", new List<string>());
        }

        try
        {
            // Get user with roles and permissions
            var userEntity = await _userRepository.GetUserWithRolesAndPermissionsAsync(userId, cancellationToken);
            if (userEntity == null)
            {
                _logger.LogWarning("Authorization failed: User {UserId} not found", userId);
                return AuthorizationResult.Failure(permission, "User not found", new List<string>());
            }

            if (!userEntity.IsActive)
            {
                _logger.LogWarning("Authorization failed: User {UserId} is not active", userId);
                return AuthorizationResult.Failure(permission, "User account is not active", new List<string>());
            }

            // Get all available permissions
            var allPermissions = await _permissionRepository.GetAllAsync(cancellationToken);
            var availablePermissions = allPermissions.Where(p => p.IsActive);
            
            // Get user roles
            var userRoles = await _roleRepository.GetUserRolesAsync(userId, cancellationToken);

            // Get user's effective permissions for context
            var userPermissions = await GetUserPermissionsAsync(userId, cancellationToken);

            // Evaluate the specific permission
            var evaluationResult = _permissionEvaluationService.EvaluatePermission(
                userEntity, 
                permission, 
                availablePermissions, 
                userRoles);

            if (evaluationResult.HasPermission)
            {
                _logger.LogDebug("Authorization successful: User {UserId} has permission {Permission}. Source: {Source}, Reason: {Reason}", 
                    userId, permission, evaluationResult.Source, evaluationResult.Reason);
                
                return AuthorizationResult.Success(permission, userPermissions);
            }
            else
            {
                _logger.LogInformation("Authorization failed: User {UserId} does not have permission {Permission}. Reason: {Reason}", 
                    userId, permission, evaluationResult.Reason);
                
                return AuthorizationResult.Failure(permission, evaluationResult.Reason, userPermissions);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during authorization for user {UserId} and permission {Permission}", userId, permission);
            return AuthorizationResult.Failure(permission, "An error occurred during authorization", new List<string>());
        }
    }

    /// <inheritdoc />
    public async Task<AuthorizationResult> AuthorizeAnyAsync(ClaimsPrincipal user, IEnumerable<string> permissions, CancellationToken cancellationToken = default)
    {
        if (user == null)
        {
            _logger.LogWarning("Authorization failed: User principal is null");
            return AuthorizationResult.Failure("", "User principal is null", new List<string>());
        }

        var userId = GetUserIdFromClaims(user);
        if (userId == null)
        {
            _logger.LogWarning("Authorization failed: Unable to extract user ID from claims");
            return AuthorizationResult.Failure("", "Unable to extract user ID from claims", new List<string>());
        }

        if (permissions == null || !permissions.Any())
        {
            _logger.LogWarning("Authorization failed: No permissions provided for user {UserId}", userId);
            return AuthorizationResult.Failure("", "No permissions provided", new List<string>());
        }

        var permissionsList = permissions.ToList();
        var permissionsString = string.Join(", ", permissionsList);

        try
        {
            // Get user with roles and permissions
            var userEntity = await _userRepository.GetUserWithRolesAndPermissionsAsync(userId.Value, cancellationToken);
            if (userEntity == null)
            {
                _logger.LogWarning("Authorization failed: User {UserId} not found", userId);
                return AuthorizationResult.Failure(permissionsString, "User not found", new List<string>());
            }

            if (!userEntity.IsActive)
            {
                _logger.LogWarning("Authorization failed: User {UserId} is not active", userId);
                return AuthorizationResult.Failure(permissionsString, "User account is not active", new List<string>());
            }

            // Get all available permissions and user roles
            var allPermissions = await _permissionRepository.GetAllAsync(cancellationToken);
            var availablePermissions = allPermissions.Where(p => p.IsActive);
            var userRoles = await _roleRepository.GetUserRolesAsync(userId.Value, cancellationToken);
            var userPermissions = await GetUserPermissionsAsync(userId.Value, cancellationToken);

            // Check if user has any of the permissions
            var hasAnyPermission = _permissionEvaluationService.HasAnyPermission(
                userEntity, 
                permissionsList, 
                availablePermissions, 
                userRoles);

            if (hasAnyPermission)
            {
                _logger.LogDebug("Authorization successful: User {UserId} has at least one of the permissions: {Permissions}", 
                    userId, permissionsString);
                
                return AuthorizationResult.Success(permissionsString, userPermissions);
            }
            else
            {
                _logger.LogInformation("Authorization failed: User {UserId} does not have any of the permissions: {Permissions}", 
                    userId, permissionsString);
                
                return AuthorizationResult.Failure(permissionsString, 
                    $"User does not have any of the required permissions: {permissionsString}", userPermissions);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during authorization for user {UserId} and permissions {Permissions}", userId, permissionsString);
            return AuthorizationResult.Failure(permissionsString, "An error occurred during authorization", new List<string>());
        }
    }

    /// <inheritdoc />
    public async Task<AuthorizationResult> AuthorizeAllAsync(ClaimsPrincipal user, IEnumerable<string> permissions, CancellationToken cancellationToken = default)
    {
        if (user == null)
        {
            _logger.LogWarning("Authorization failed: User principal is null");
            return AuthorizationResult.Failure("", "User principal is null", new List<string>());
        }

        var userId = GetUserIdFromClaims(user);
        if (userId == null)
        {
            _logger.LogWarning("Authorization failed: Unable to extract user ID from claims");
            return AuthorizationResult.Failure("", "Unable to extract user ID from claims", new List<string>());
        }

        if (permissions == null || !permissions.Any())
        {
            _logger.LogWarning("Authorization failed: No permissions provided for user {UserId}", userId);
            return AuthorizationResult.Failure("", "No permissions provided", new List<string>());
        }

        var permissionsList = permissions.ToList();
        var permissionsString = string.Join(", ", permissionsList);

        try
        {
            // Get user with roles and permissions
            var userEntity = await _userRepository.GetUserWithRolesAndPermissionsAsync(userId.Value, cancellationToken);
            if (userEntity == null)
            {
                _logger.LogWarning("Authorization failed: User {UserId} not found", userId);
                return AuthorizationResult.Failure(permissionsString, "User not found", new List<string>());
            }

            if (!userEntity.IsActive)
            {
                _logger.LogWarning("Authorization failed: User {UserId} is not active", userId);
                return AuthorizationResult.Failure(permissionsString, "User account is not active", new List<string>());
            }

            // Get all available permissions and user roles
            var allPermissions = await _permissionRepository.GetAllAsync(cancellationToken);
            var availablePermissions = allPermissions.Where(p => p.IsActive);
            var userRoles = await _roleRepository.GetUserRolesAsync(userId.Value, cancellationToken);
            var userPermissions = await GetUserPermissionsAsync(userId.Value, cancellationToken);

            // Check each permission individually
            var missingPermissions = new List<string>();
            
            foreach (var permission in permissionsList)
            {
                var evaluationResult = _permissionEvaluationService.EvaluatePermission(
                    userEntity, 
                    permission, 
                    availablePermissions, 
                    userRoles);

                if (!evaluationResult.HasPermission)
                {
                    missingPermissions.Add(permission);
                }
            }

            if (!missingPermissions.Any())
            {
                _logger.LogDebug("Authorization successful: User {UserId} has all required permissions: {Permissions}", 
                    userId, permissionsString);
                
                return AuthorizationResult.Success(permissionsString, userPermissions);
            }
            else
            {
                var missingPermissionsString = string.Join(", ", missingPermissions);
                _logger.LogInformation("Authorization failed: User {UserId} is missing permissions: {MissingPermissions}", 
                    userId, missingPermissionsString);
                
                return AuthorizationResult.Failure(permissionsString, 
                    $"User is missing the following permissions: {missingPermissionsString}", userPermissions);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during authorization for user {UserId} and permissions {Permissions}", userId, permissionsString);
            return AuthorizationResult.Failure(permissionsString, "An error occurred during authorization", new List<string>());
        }
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, AuthorizationResult>> BulkAuthorizeAsync(ClaimsPrincipal user, IEnumerable<string> permissions, CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, AuthorizationResult>();

        if (user == null)
        {
            _logger.LogWarning("Bulk authorization failed: User principal is null");
            foreach (var permission in permissions ?? Enumerable.Empty<string>())
            {
                results[permission] = AuthorizationResult.Failure(permission, "User principal is null", new List<string>());
            }
            return results;
        }

        var userId = GetUserIdFromClaims(user);
        if (userId == null)
        {
            _logger.LogWarning("Bulk authorization failed: Unable to extract user ID from claims");
            foreach (var permission in permissions ?? Enumerable.Empty<string>())
            {
                results[permission] = AuthorizationResult.Failure(permission, "Unable to extract user ID from claims", new List<string>());
            }
            return results;
        }

        if (permissions == null || !permissions.Any())
        {
            _logger.LogWarning("Bulk authorization failed: No permissions provided for user {UserId}", userId);
            return results;
        }

        var permissionsList = permissions.ToList();

        try
        {
            // Get user with roles and permissions once
            var userEntity = await _userRepository.GetUserWithRolesAndPermissionsAsync(userId.Value, cancellationToken);
            if (userEntity == null)
            {
                _logger.LogWarning("Bulk authorization failed: User {UserId} not found", userId);
                foreach (var permission in permissionsList)
                {
                    results[permission] = AuthorizationResult.Failure(permission, "User not found", new List<string>());
                }
                return results;
            }

            if (!userEntity.IsActive)
            {
                _logger.LogWarning("Bulk authorization failed: User {UserId} is not active", userId);
                foreach (var permission in permissionsList)
                {
                    results[permission] = AuthorizationResult.Failure(permission, "User account is not active", new List<string>());
                }
                return results;
            }

            // Get all available permissions and user roles once
            var allPermissions = await _permissionRepository.GetAllAsync(cancellationToken);
            var availablePermissions = allPermissions.Where(p => p.IsActive);
            var userRoles = await _roleRepository.GetUserRolesAsync(userId.Value, cancellationToken);
            var userPermissions = await GetUserPermissionsAsync(userId.Value, cancellationToken);

            // Evaluate each permission
            foreach (var permission in permissionsList)
            {
                try
                {
                    var evaluationResult = _permissionEvaluationService.EvaluatePermission(
                        userEntity, 
                        permission, 
                        availablePermissions, 
                        userRoles);

                    if (evaluationResult.HasPermission)
                    {
                        results[permission] = AuthorizationResult.Success(permission, userPermissions);
                    }
                    else
                    {
                        results[permission] = AuthorizationResult.Failure(permission, evaluationResult.Reason, userPermissions);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during bulk authorization for user {UserId} and permission {Permission}", userId, permission);
                    results[permission] = AuthorizationResult.Failure(permission, "An error occurred during authorization", userPermissions);
                }
            }

            _logger.LogDebug("Bulk authorization completed for user {UserId}. Checked {PermissionCount} permissions", 
                userId, permissionsList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk authorization for user {UserId}", userId);
            foreach (var permission in permissionsList)
            {
                results[permission] = AuthorizationResult.Failure(permission, "An error occurred during authorization", new List<string>());
            }
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetUserPermissionsAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (user == null)
        {
            _logger.LogWarning("Get user permissions failed: User principal is null");
            return new List<string>();
        }

        var userId = GetUserIdFromClaims(user);
        if (userId == null)
        {
            _logger.LogWarning("Get user permissions failed: Unable to extract user ID from claims");
            return new List<string>();
        }

        return await GetUserPermissionsAsync(userId.Value, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to get from cache first
            var cachedPermissions = await _permissionCacheService.GetUserPermissionsAsync(userId, cancellationToken);
            if (cachedPermissions != null && cachedPermissions.Any())
            {
                _logger.LogDebug("Retrieved user permissions from cache for user {UserId}", userId);
                return cachedPermissions;
            }

            // Get from database if not in cache
            var userEntity = await _userRepository.GetUserWithRolesAndPermissionsAsync(userId, cancellationToken);
            if (userEntity == null)
            {
                _logger.LogWarning("Get user permissions failed: User {UserId} not found", userId);
                return new List<string>();
            }

            var allPermissions = await _permissionRepository.GetAllAsync(cancellationToken);
            var availablePermissions = allPermissions.Where(p => p.IsActive);
            var userRoles = await _roleRepository.GetUserRolesAsync(userId, cancellationToken);

            var permissions = userEntity.GetEffectivePermissions(availablePermissions, userRoles).ToList();

            // Cache the permissions
            await _permissionCacheService.SetUserPermissionsAsync(userId, permissions, cancellationToken: cancellationToken);

            _logger.LogDebug("Retrieved and cached user permissions for user {UserId}. Permission count: {PermissionCount}", 
                userId, permissions.Count);

            return permissions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user permissions for user {UserId}", userId);
            return new List<string>();
        }
    }

    /// <summary>
    /// Extracts the user ID from the claims principal
    /// </summary>
    /// <param name="user">The claims principal</param>
    /// <returns>The user ID if found, null otherwise</returns>
    private static Guid? GetUserIdFromClaims(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier) ?? user.FindFirst("sub") ?? user.FindFirst("userId");
        
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<EffectivePermissionsDto> GetEffectivePermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var userEntity = await _userRepository.GetUserWithRolesAndPermissionsAsync(userId, cancellationToken);
            if (userEntity == null)
            {
                _logger.LogWarning("Get effective permissions failed: User {UserId} not found", userId);
                return new EffectivePermissionsDto
                {
                    User = new UserDto { Id = userId },
                    CalculatedAt = DateTime.UtcNow
                };
            }

            var allPermissions = await _permissionRepository.GetAllAsync(cancellationToken);
            var availablePermissions = allPermissions.Where(p => p.IsActive);
            var userRoles = await _roleRepository.GetUserRolesAsync(userId, cancellationToken);
            var userPermissionOverrides = await _userPermissionRepository.GetByUserIdAsync(userId, cancellationToken);

            // Get role permissions
            var rolePermissions = new List<PermissionSourceDto>();
            foreach (var role in userRoles.Where(r => r.IsActive))
            {
                var rolePermissionIds = role.RolePermissions.Select(rp => rp.PermissionId);
                var rolePermissionEntities = availablePermissions.Where(p => rolePermissionIds.Contains(p.Id) && p.IsActive);
                
                foreach (var permission in rolePermissionEntities)
                {
                    rolePermissions.Add(new PermissionSourceDto
                    {
                        Permission = permission.Name,
                        SourceType = "Role",
                        SourceId = role.Id,
                        SourceName = role.Name,
                        IsActive = true
                    });
                }
            }

            // Get user overrides
            var userOverrides = userPermissionOverrides.Select(up => 
            {
                var permission = availablePermissions.FirstOrDefault(p => p.Id == up.PermissionId);
                return new UserPermissionOverrideDto
                {
                    Id = up.Id,
                    UserId = up.UserId,
                    Permission = permission != null ? new PermissionDto
                    {
                        Id = permission.Id,
                        Resource = permission.Resource,
                        Action = permission.Action,
                        Name = permission.Name,
                        Description = permission.Description,
                        Category = permission.Category,
                        IsActive = permission.IsActive,
                        ParentPermissionId = permission.ParentPermissionId
                    } : new PermissionDto(),
                    State = up.State,
                    Reason = up.Reason,
                    ExpiresAt = up.ExpiresAt,
                    CreatedAt = up.CreatedAt,
                    CreatedBy = up.CreatedBy ?? string.Empty,
                    UpdatedAt = up.UpdatedAt,
                    UpdatedBy = up.UpdatedBy
                };
            }).ToList();

            // Calculate effective permissions
            var effectivePermissions = userEntity.GetEffectivePermissions(availablePermissions, userRoles).ToList();
            
            // Separate granted and denied permissions from user overrides
            var grantedPermissions = userOverrides
                .Where(uo => uo.State == Domain.Enums.PermissionState.Grant && uo.IsActive)
                .Select(uo => uo.Permission.Name)
                .ToList();
                
            var deniedPermissions = userOverrides
                .Where(uo => uo.State == Domain.Enums.PermissionState.Deny && uo.IsActive)
                .Select(uo => uo.Permission.Name)
                .ToList();

            return new EffectivePermissionsDto
            {
                User = new UserDto
                {
                    Id = userEntity.Id,
                    Email = userEntity.Email.Value,
                    FirstName = userEntity.FirstName,
                    LastName = userEntity.LastName,
                    IsActive = userEntity.IsActive,
                    IsEmailVerified = userEntity.IsEmailVerified,
                    LastLoginAt = userEntity.LastLoginAt,
                    CreatedAt = userEntity.CreatedAt
                },
                RolePermissions = rolePermissions,
                UserOverrides = userOverrides,
                EffectivePermissions = effectivePermissions,
                GrantedPermissions = grantedPermissions,
                DeniedPermissions = deniedPermissions,
                CalculatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting effective permissions for user {UserId}", userId);
            return new EffectivePermissionsDto
            {
                User = new UserDto { Id = userId },
                CalculatedAt = DateTime.UtcNow
            };
        }
    }

    /// <inheritdoc />
    public async Task<AuthorizationResult> AuthorizeResourceAsync(ClaimsPrincipal user, string resource, string action, Guid resourceId, CancellationToken cancellationToken = default)
    {
        // For now, this is the same as regular authorization
        // In the future, this could be extended to check resource-specific permissions
        // or ownership-based access control
        var result = await AuthorizeAsync(user, resource, action, cancellationToken);
        
        if (result.IsAuthorized)
        {
            result.Context["ResourceId"] = resourceId;
            result.Context["ResourceSpecific"] = true;
        }
        
        return result;
    }

    /// <inheritdoc />
    public async Task<AuthorizationContextDto> GetAuthorizationContextAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (user == null)
        {
            _logger.LogWarning("Get authorization context failed: User principal is null");
            return new AuthorizationContextDto
            {
                GeneratedAt = DateTime.UtcNow
            };
        }

        var userId = GetUserIdFromClaims(user);
        if (userId == null)
        {
            _logger.LogWarning("Get authorization context failed: Unable to extract user ID from claims");
            return new AuthorizationContextDto
            {
                GeneratedAt = DateTime.UtcNow
            };
        }

        try
        {
            var userEntity = await _userRepository.GetUserWithRolesAndPermissionsAsync(userId.Value, cancellationToken);
            if (userEntity == null)
            {
                _logger.LogWarning("Get authorization context failed: User {UserId} not found", userId);
                return new AuthorizationContextDto
                {
                    User = new UserDto { Id = userId.Value },
                    GeneratedAt = DateTime.UtcNow
                };
            }

            var userRoles = await _roleRepository.GetUserRolesAsync(userId.Value, cancellationToken);
            var userPermissions = await GetUserPermissionsAsync(userId.Value, cancellationToken);
            var userPermissionOverrides = await _userPermissionRepository.GetByUserIdAsync(userId.Value, cancellationToken);
            var allPermissions = await _permissionRepository.GetAllAsync(cancellationToken);
            var availablePermissions = allPermissions.Where(p => p.IsActive);

            var roles = userRoles.Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                IsActive = r.IsActive,
                CreatedAt = r.CreatedAt
            }).ToList();

            var permissionOverrides = userPermissionOverrides.Select(up => 
            {
                var permission = availablePermissions.FirstOrDefault(p => p.Id == up.PermissionId);
                return new UserPermissionOverrideDto
                {
                    Id = up.Id,
                    UserId = up.UserId,
                    Permission = permission != null ? new PermissionDto
                    {
                        Id = permission.Id,
                        Resource = permission.Resource,
                        Action = permission.Action,
                        Name = permission.Name,
                        Description = permission.Description,
                        Category = permission.Category,
                        IsActive = permission.IsActive,
                        ParentPermissionId = permission.ParentPermissionId
                    } : new PermissionDto(),
                    State = up.State,
                    Reason = up.Reason,
                    ExpiresAt = up.ExpiresAt,
                    CreatedAt = up.CreatedAt,
                    CreatedBy = up.CreatedBy ?? string.Empty,
                    UpdatedAt = up.UpdatedAt,
                    UpdatedBy = up.UpdatedBy
                };
            }).ToList();

            // Extract session information from claims
            var sessionInfo = new AuthorizationSessionDto
            {
                SessionId = user.FindFirst("jti")?.Value ?? Guid.NewGuid().ToString(),
                StartedAt = DateTime.UtcNow, // This would ideally come from the token
                ExpiresAt = DateTime.UtcNow.AddHours(1), // This would ideally come from the token
                IsActive = true,
                IpAddress = user.FindFirst("ipaddr")?.Value,
                UserAgent = user.FindFirst("user_agent")?.Value
            };

            return new AuthorizationContextDto
            {
                User = new UserDto
                {
                    Id = userEntity.Id,
                    Email = userEntity.Email.Value,
                    FirstName = userEntity.FirstName,
                    LastName = userEntity.LastName,
                    IsActive = userEntity.IsActive,
                    IsEmailVerified = userEntity.IsEmailVerified,
                    LastLoginAt = userEntity.LastLoginAt,
                    CreatedAt = userEntity.CreatedAt
                },
                Roles = roles,
                Permissions = userPermissions,
                PermissionOverrides = permissionOverrides,
                Session = sessionInfo,
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting authorization context for user {UserId}", userId);
            return new AuthorizationContextDto
            {
                User = new UserDto { Id = userId.Value },
                GeneratedAt = DateTime.UtcNow
            };
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsValidPermissionAsync(string permission, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(permission))
        {
            return false;
        }

        try
        {
            var permissionEntity = await _permissionRepository.GetByNameAsync(permission.Trim(), cancellationToken);
            return permissionEntity != null && permissionEntity.IsActive;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating permission {Permission}", permission);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<AuthorizationRequirementsDto> GetAuthorizationRequirementsAsync(string resource, string action, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(resource) || string.IsNullOrWhiteSpace(action))
        {
            return new AuthorizationRequirementsDto
            {
                Resource = resource ?? string.Empty,
                Action = action ?? string.Empty,
                Description = "Invalid resource or action"
            };
        }

        try
        {
            var permissionName = $"{resource.Trim()}.{action.Trim()}";
            var permission = await _permissionRepository.GetByNameAsync(permissionName, cancellationToken);
            
            if (permission == null)
            {
                return new AuthorizationRequirementsDto
                {
                    Resource = resource,
                    Action = action,
                    RequiredPermissions = new List<string>(),
                    Description = $"Permission '{permissionName}' does not exist"
                };
            }

            var requiredPermissions = new List<string> { permissionName };
            
            // Check for hierarchical permissions
            var alternativePermissionSets = new List<IEnumerable<string>>();
            if (permission.ParentPermissionId.HasValue)
            {
                var parentPermission = await _permissionRepository.GetByIdAsync(permission.ParentPermissionId.Value, cancellationToken);
                if (parentPermission != null && parentPermission.IsActive)
                {
                    alternativePermissionSets.Add(new List<string> { parentPermission.Name });
                }
            }

            return new AuthorizationRequirementsDto
            {
                Resource = resource,
                Action = action,
                RequiredPermissions = requiredPermissions,
                AlternativePermissionSets = alternativePermissionSets,
                RequiresOwnership = false, // This could be extended based on resource type
                Description = $"Requires permission '{permissionName}' or equivalent parent permission"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting authorization requirements for resource {Resource} and action {Action}", resource, action);
            return new AuthorizationRequirementsDto
            {
                Resource = resource,
                Action = action,
                Description = "Error retrieving authorization requirements"
            };
        }
    }
}

