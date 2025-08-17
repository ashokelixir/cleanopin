using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.Interfaces;
using CleanArchTemplate.Domain.Services;

namespace CleanArchTemplate.Application.Services;

/// <summary>
/// Application service for permission evaluation that handles repository interactions
/// and delegates domain logic to the domain service
/// </summary>
public class PermissionApplicationService : IPermissionApplicationService
{
    private readonly IUserRepository _userRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionEvaluationService _permissionEvaluationService;

    public PermissionApplicationService(
        IUserRepository userRepository,
        IPermissionRepository permissionRepository,
        IRoleRepository roleRepository,
        IPermissionEvaluationService permissionEvaluationService)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _permissionRepository = permissionRepository ?? throw new ArgumentNullException(nameof(permissionRepository));
        _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
        _permissionEvaluationService = permissionEvaluationService ?? throw new ArgumentNullException(nameof(permissionEvaluationService));
    }

    /// <inheritdoc />
    public async Task<bool> HasPermissionAsync(Guid userId, string resource, string action, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));

        if (string.IsNullOrWhiteSpace(resource))
            throw new ArgumentException("Resource cannot be empty.", nameof(resource));

        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action cannot be empty.", nameof(action));

        var permissionName = $"{resource.Trim()}.{action.Trim()}";
        return await HasPermissionAsync(userId, permissionName, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> HasPermissionAsync(Guid userId, string permissionName, CancellationToken cancellationToken = default)
    {
        var result = await EvaluatePermissionAsync(userId, permissionName, cancellationToken);
        return result.HasPermission;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));

        // Load user with roles and permissions
        var user = await _userRepository.GetUserWithRolesAndPermissionsAsync(userId, cancellationToken);
        if (user == null)
            return Enumerable.Empty<string>();

        // Get all available permissions
        var availablePermissions = await _permissionRepository.GetAllAsync(cancellationToken);
        
        // Get user's roles
        var userRoles = await _roleRepository.GetUserRolesAsync(userId, cancellationToken);

        // Use domain service to evaluate permissions
        return user.GetEffectivePermissions(availablePermissions, userRoles);
    }

    /// <inheritdoc />
    public async Task<bool> HasAnyPermissionAsync(Guid userId, IEnumerable<string> permissions, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));

        if (permissions == null)
            throw new ArgumentNullException(nameof(permissions));

        var permissionList = permissions.ToList();
        if (!permissionList.Any())
            return false;

        // Load user with roles and permissions
        var user = await _userRepository.GetUserWithRolesAndPermissionsAsync(userId, cancellationToken);
        if (user == null)
            return false;

        // Get all available permissions
        var availablePermissions = await _permissionRepository.GetAllAsync(cancellationToken);
        
        // Get user's roles
        var userRoles = await _roleRepository.GetUserRolesAsync(userId, cancellationToken);

        // Use domain logic to check permissions
        return user.HasAnyPermission(permissionList, availablePermissions, userRoles);
    }

    /// <inheritdoc />
    public async Task<PermissionEvaluationResult> EvaluatePermissionAsync(Guid userId, string permissionName, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));

        if (string.IsNullOrWhiteSpace(permissionName))
            throw new ArgumentException("Permission name cannot be empty.", nameof(permissionName));

        permissionName = permissionName.Trim();

        // Load user with roles and permissions
        var user = await _userRepository.GetUserWithRolesAndPermissionsAsync(userId, cancellationToken);
        if (user == null)
        {
            return PermissionEvaluationResult.Denied($"User with ID '{userId}' not found.");
        }

        // Get all available permissions
        var availablePermissions = await _permissionRepository.GetAllAsync(cancellationToken);
        
        // Get user's roles
        var userRoles = await _roleRepository.GetUserRolesAsync(userId, cancellationToken);

        // Use domain service to evaluate permission
        return _permissionEvaluationService.EvaluatePermission(user, permissionName, availablePermissions, userRoles);
    }

    /// <inheritdoc />
    public async Task<bool> HasHierarchicalPermissionAsync(Guid userId, string permissionName, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));

        if (string.IsNullOrWhiteSpace(permissionName))
            throw new ArgumentException("Permission name cannot be empty.", nameof(permissionName));

        // Load user with roles and permissions
        var user = await _userRepository.GetUserWithRolesAndPermissionsAsync(userId, cancellationToken);
        if (user == null)
            return false;

        // Get all available permissions
        var availablePermissions = await _permissionRepository.GetAllAsync(cancellationToken);
        
        // Get user's roles
        var userRoles = await _roleRepository.GetUserRolesAsync(userId, cancellationToken);

        // Use domain service to check hierarchical permission
        return _permissionEvaluationService.HasHierarchicalPermission(user, permissionName, availablePermissions, userRoles);
    }
}