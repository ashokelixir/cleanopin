using AutoMapper;
using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Application.Common.Models;
using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.Enums;
using CleanArchTemplate.Domain.Interfaces;
using CleanArchTemplate.Domain.Services;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace CleanArchTemplate.Infrastructure.Services;

/// <summary>
/// Service for permission matrix operations including role-permission matrix management,
/// user permission matrix functionality, and bulk operations
/// </summary>
public class PermissionMatrixService : IPermissionMatrixService
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IUserPermissionRepository _userPermissionRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPermissionEvaluationService _permissionEvaluationService;
    private readonly IMapper _mapper;
    private readonly ILogger<PermissionMatrixService> _logger;

    public PermissionMatrixService(
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IUserPermissionRepository userPermissionRepository,
        IUserRepository userRepository,
        IPermissionEvaluationService permissionEvaluationService,
        IMapper mapper,
        ILogger<PermissionMatrixService> logger)
    {
        _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
        _permissionRepository = permissionRepository ?? throw new ArgumentNullException(nameof(permissionRepository));
        _userPermissionRepository = userPermissionRepository ?? throw new ArgumentNullException(nameof(userPermissionRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _permissionEvaluationService = permissionEvaluationService ?? throw new ArgumentNullException(nameof(permissionEvaluationService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<PermissionMatrixDto> GetRolePermissionMatrixAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting complete role-permission matrix");

        var roles = await _roleRepository.GetRolesWithPermissionsAsync(cancellationToken);
        var permissions = await _permissionRepository.GetAllAsync(cancellationToken);

        return await BuildPermissionMatrixAsync(roles, permissions, null, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PermissionMatrixDto> GetRolePermissionMatrixByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Category cannot be empty.", nameof(category));

        _logger.LogInformation("Getting role-permission matrix for category: {Category}", category);

        var roles = await _roleRepository.GetRolesWithPermissionsAsync(cancellationToken);
        var permissions = await _permissionRepository.GetByCategoryAsync(category, cancellationToken);

        return await BuildPermissionMatrixAsync(roles, permissions, category, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<UserPermissionMatrixDto> GetUserPermissionMatrixAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting user permission matrix for user: {UserId}", userId);

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            throw new ArgumentException($"User with ID {userId} not found.", nameof(userId));

        var userRoles = await _roleRepository.GetUserRolesAsync(userId, cancellationToken);
        var allPermissions = await _permissionRepository.GetAllAsync(cancellationToken);
        var userOverrides = await _userPermissionRepository.GetUserPermissionsWithDetailsAsync(userId, cancellationToken);

        return await BuildUserPermissionMatrixAsync(user, userRoles, allPermissions, userOverrides, null, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<UserPermissionMatrixDto> GetUserPermissionMatrixByCategoryAsync(Guid userId, string category, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Category cannot be empty.", nameof(category));

        _logger.LogInformation("Getting user permission matrix for user: {UserId}, category: {Category}", userId, category);

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            throw new ArgumentException($"User with ID {userId} not found.", nameof(userId));

        var userRoles = await _roleRepository.GetUserRolesAsync(userId, cancellationToken);
        var permissions = await _permissionRepository.GetByCategoryAsync(category, cancellationToken);
        var userOverrides = await _userPermissionRepository.GetUserPermissionsWithDetailsAsync(userId, cancellationToken);

        // Filter user overrides to only include permissions in the specified category
        var filteredOverrides = userOverrides.Where(uo => 
            permissions.Any(p => p.Id == uo.PermissionId));

        return await BuildUserPermissionMatrixAsync(user, userRoles, permissions, filteredOverrides, category, cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateRolePermissionMatrixAsync(Guid roleId, IEnumerable<Guid> permissionIds, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating role permission matrix for role: {RoleId}", roleId);

        var role = await _roleRepository.GetRoleWithPermissionsByIdAsync(roleId, cancellationToken);
        if (role == null)
            throw new ArgumentException($"Role with ID {roleId} not found.", nameof(roleId));

        var permissionIdsList = permissionIds.ToList();
        var permissions = await _permissionRepository.GetByIdsAsync(permissionIdsList, cancellationToken);

        // Validate all permissions exist
        var foundPermissionIds = permissions.Select(p => p.Id).ToHashSet();
        var missingPermissionIds = permissionIdsList.Where(id => !foundPermissionIds.Contains(id)).ToList();
        if (missingPermissionIds.Any())
        {
            throw new ArgumentException($"Permissions not found: {string.Join(", ", missingPermissionIds)}");
        }

        // Clear existing permissions and add new ones
        role.ClearPermissions();
        foreach (var permission in permissions.Where(p => p.IsActive))
        {
            role.AddPermission(permission);
        }

        await _roleRepository.UpdateAsync(role, cancellationToken);
        _logger.LogInformation("Updated role {RoleId} with {PermissionCount} permissions", roleId, permissions.Count());
    }

    /// <inheritdoc />
    public async Task BulkUpdateRolePermissionMatrixAsync(Dictionary<Guid, IEnumerable<Guid>> rolePermissionUpdates, CancellationToken cancellationToken = default)
    {
        if (rolePermissionUpdates == null || !rolePermissionUpdates.Any())
            throw new ArgumentException("Role permission updates cannot be empty.", nameof(rolePermissionUpdates));

        _logger.LogInformation("Bulk updating role permission matrix for {RoleCount} roles", rolePermissionUpdates.Count);

        var roleIds = rolePermissionUpdates.Keys.ToList();
        var allPermissionIds = rolePermissionUpdates.Values.SelectMany(ids => ids).Distinct().ToList();

        // Load all required data
        var roles = await _roleRepository.GetByIdsAsync(roleIds, cancellationToken);
        var permissions = await _permissionRepository.GetByIdsAsync(allPermissionIds, cancellationToken);

        var roleDict = roles.ToDictionary(r => r.Id);
        var permissionDict = permissions.ToDictionary(p => p.Id);

        // Validate all roles exist
        var missingRoleIds = roleIds.Where(id => !roleDict.ContainsKey(id)).ToList();
        if (missingRoleIds.Any())
        {
            throw new ArgumentException($"Roles not found: {string.Join(", ", missingRoleIds)}");
        }

        // Validate all permissions exist
        var missingPermissionIds = allPermissionIds.Where(id => !permissionDict.ContainsKey(id)).ToList();
        if (missingPermissionIds.Any())
        {
            throw new ArgumentException($"Permissions not found: {string.Join(", ", missingPermissionIds)}");
        }

        // Update each role
        foreach (var (roleId, permissionIds) in rolePermissionUpdates)
        {
            var role = roleDict[roleId];
            var rolePermissions = permissionIds.Select(id => permissionDict[id]).Where(p => p.IsActive);

            role.ClearPermissions();
            foreach (var permission in rolePermissions)
            {
                role.AddPermission(permission);
            }

            await _roleRepository.UpdateAsync(role, cancellationToken);
        }

        _logger.LogInformation("Bulk updated {RoleCount} roles with permission assignments", rolePermissionUpdates.Count);
    }

    /// <inheritdoc />
    public async Task UpdateUserPermissionOverridesAsync(Guid userId, Dictionary<Guid, PermissionState> permissionOverrides, CancellationToken cancellationToken = default)
    {
        if (permissionOverrides == null || !permissionOverrides.Any())
            throw new ArgumentException("Permission overrides cannot be empty.", nameof(permissionOverrides));

        _logger.LogInformation("Updating user permission overrides for user: {UserId}", userId);

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            throw new ArgumentException($"User with ID {userId} not found.", nameof(userId));

        var permissionIds = permissionOverrides.Keys.ToList();
        var permissions = await _permissionRepository.GetByIdsAsync(permissionIds, cancellationToken);
        var existingOverrides = await _userPermissionRepository.GetByUserIdAsync(userId, cancellationToken);

        var permissionDict = permissions.ToDictionary(p => p.Id);
        var existingOverrideDict = existingOverrides.ToDictionary(uo => uo.PermissionId);

        // Validate all permissions exist
        var missingPermissionIds = permissionIds.Where(id => !permissionDict.ContainsKey(id)).ToList();
        if (missingPermissionIds.Any())
        {
            throw new ArgumentException($"Permissions not found: {string.Join(", ", missingPermissionIds)}");
        }

        var overridesToAdd = new List<UserPermission>();
        var overridesToUpdate = new List<UserPermission>();
        var overridesToRemove = new List<UserPermission>();

        foreach (var (permissionId, state) in permissionOverrides)
        {
            var permission = permissionDict[permissionId];
            if (!permission.IsActive)
                continue;

            if (existingOverrideDict.TryGetValue(permissionId, out var existingOverride))
            {
                // Update existing override
                existingOverride.UpdateState(state, $"Bulk update - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
                overridesToUpdate.Add(existingOverride);
            }
            else
            {
                // Create new override
                var newOverride = UserPermission.Create(userId, permissionId, state, 
                    $"Bulk assignment - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
                overridesToAdd.Add(newOverride);
            }
        }

        // Add new overrides
        if (overridesToAdd.Any())
        {
            await _userPermissionRepository.BulkAddAsync(overridesToAdd, cancellationToken);
        }

        // Update existing overrides
        foreach (var override_ in overridesToUpdate)
        {
            await _userPermissionRepository.UpdateAsync(override_, cancellationToken);
        }

        _logger.LogInformation("Updated user {UserId} with {OverrideCount} permission overrides", 
            userId, permissionOverrides.Count);
    }

    /// <inheritdoc />
    public async Task<PermissionMatrixComparisonDto> CompareRolePermissionsAsync(Guid roleId1, Guid roleId2, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Comparing permissions between roles: {RoleId1} and {RoleId2}", roleId1, roleId2);

        var role1 = await _roleRepository.GetRoleWithPermissionsByIdAsync(roleId1, cancellationToken);
        var role2 = await _roleRepository.GetRoleWithPermissionsByIdAsync(roleId2, cancellationToken);

        if (role1 == null)
            throw new ArgumentException($"Role with ID {roleId1} not found.", nameof(roleId1));
        if (role2 == null)
            throw new ArgumentException($"Role with ID {roleId2} not found.", nameof(roleId2));

        var role1Permissions = role1.GetPermissions().ToList();
        var role2Permissions = role2.GetPermissions().ToList();

        return BuildPermissionComparison(
            new PermissionEntityDto { Id = role1.Id, Name = role1.Name, Type = "Role", PermissionCount = role1Permissions.Count },
            new PermissionEntityDto { Id = role2.Id, Name = role2.Name, Type = "Role", PermissionCount = role2Permissions.Count },
            role1Permissions,
            role2Permissions);
    }

    /// <inheritdoc />
    public async Task<PermissionMatrixComparisonDto> CompareUserAndRolePermissionsAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Comparing permissions between user: {UserId} and role: {RoleId}", userId, roleId);

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        var role = await _roleRepository.GetRoleWithPermissionsByIdAsync(roleId, cancellationToken);

        if (user == null)
            throw new ArgumentException($"User with ID {userId} not found.", nameof(userId));
        if (role == null)
            throw new ArgumentException($"Role with ID {roleId} not found.", nameof(roleId));

        var userRoles = await _roleRepository.GetUserRolesAsync(userId, cancellationToken);
        var allPermissions = await _permissionRepository.GetAllAsync(cancellationToken);
        var userEffectivePermissions = user.GetEffectivePermissions(allPermissions, userRoles).ToList();
        var rolePermissions = role.GetPermissions().ToList();

        // Convert permission names to Permission objects for comparison
        var userPermissionObjects = allPermissions.Where(p => userEffectivePermissions.Contains(p.Name)).ToList();

        return BuildPermissionComparison(
            new PermissionEntityDto { Id = user.Id, Name = user.FullName, Type = "User", PermissionCount = userPermissionObjects.Count },
            new PermissionEntityDto { Id = role.Id, Name = role.Name, Type = "Role", PermissionCount = rolePermissions.Count },
            userPermissionObjects,
            rolePermissions);
    }

    /// <inheritdoc />
    public async Task<PermissionMatrixStatisticsDto> GetPermissionMatrixStatisticsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting permission matrix statistics");

        var roles = await _roleRepository.GetRolesWithPermissionsAsync(cancellationToken);
        var permissions = await _permissionRepository.GetAllAsync(cancellationToken);
        var users = await _userRepository.GetAllAsync(cancellationToken);
        var userOverrides = await _userPermissionRepository.GetAllAsync(cancellationToken);

        var rolesList = roles.ToList();
        var permissionsList = permissions.ToList();
        var usersList = users.ToList();
        var userOverridesList = userOverrides.ToList();

        // Calculate system statistics
        var systemStats = new SystemPermissionStatisticsDto
        {
            TotalPermissions = permissionsList.Count,
            TotalRoles = rolesList.Count,
            TotalUsers = usersList.Count,
            TotalRolePermissionAssignments = rolesList.Sum(r => r.GetPermissions().Count()),
            TotalUserPermissionOverrides = userOverridesList.Count,
            ActivePermissions = permissionsList.Count(p => p.IsActive),
            InactivePermissions = permissionsList.Count(p => !p.IsActive),
            AveragePermissionsPerRole = rolesList.Any() ? (double)rolesList.Sum(r => r.GetPermissions().Count()) / rolesList.Count : 0,
            AverageEffectivePermissionsPerUser = 0 // Will be calculated below
        };

        // Calculate role statistics
        var roleStats = rolesList.Select(role => new RolePermissionStatisticsDto
        {
            Role = _mapper.Map<RoleDto>(role),
            PermissionCount = role.GetPermissions().Count(),
            UserCount = usersList.Count(u => u.UserRoles.Any(ur => ur.RoleId == role.Id)),
            PermissionsByCategory = role.GetPermissions()
                .GroupBy(p => p.Category)
                .ToDictionary(g => g.Key, g => g.Count()),
            LastPermissionAssigned = role.UpdatedAt
        }).ToList();

        // Calculate permission usage statistics
        var permissionUsage = permissionsList.Select(permission =>
        {
            var rolesWithPermission = rolesList.Where(r => r.HasPermission(permission.Id)).ToList();
            var usersWithPermission = usersList.Where(u => 
                u.UserRoles.Any(ur => rolesWithPermission.Any(r => r.Id == ur.RoleId))).ToList();
            var userOverridesForPermission = userOverridesList.Where(uo => uo.PermissionId == permission.Id).ToList();

            return new PermissionUsageStatisticsDto
            {
                Permission = _mapper.Map<PermissionDto>(permission),
                RoleCount = rolesWithPermission.Count,
                UserCount = usersWithPermission.Count,
                UserOverrideCount = userOverridesForPermission.Count,
                RoleUsagePercentage = rolesList.Any() ? (double)rolesWithPermission.Count / rolesList.Count * 100 : 0,
                UserUsagePercentage = usersList.Any() ? (double)usersWithPermission.Count / usersList.Count * 100 : 0
            };
        }).ToList();

        // Calculate category statistics
        var categoryStats = permissionsList
            .GroupBy(p => p.Category)
            .Select(g => new CategoryStatisticsDto
            {
                Category = g.Key,
                PermissionCount = g.Count(),
                RoleAssignmentCount = rolesList.Sum(r => r.GetPermissions().Count(p => p.Category == g.Key)),
                UserOverrideCount = userOverridesList.Count(uo => 
                    permissionsList.Any(p => p.Id == uo.PermissionId && p.Category == g.Key)),
                AverageRoleUsage = rolesList.Any() ? 
                    (double)rolesList.Count(r => r.GetPermissions().Any(p => p.Category == g.Key)) / rolesList.Count * 100 : 0
            }).ToList();

        // Calculate user override statistics
        var activeOverrides = userOverridesList.Where(uo => uo.IsActive()).ToList();
        var expiredOverrides = userOverridesList.Where(uo => uo.IsExpired()).ToList();
        var usersWithOverrides = userOverridesList.Select(uo => uo.UserId).Distinct().Count();

        var userOverrideStats = new UserOverrideStatisticsDto
        {
            TotalOverrides = userOverridesList.Count,
            GrantOverrides = userOverridesList.Count(uo => uo.State == PermissionState.Grant),
            DenyOverrides = userOverridesList.Count(uo => uo.State == PermissionState.Deny),
            ActiveOverrides = activeOverrides.Count,
            ExpiredOverrides = expiredOverrides.Count,
            UsersWithOverrides = usersWithOverrides,
            AverageOverridesPerUser = usersWithOverrides > 0 ? (double)userOverridesList.Count / usersWithOverrides : 0,
            CommonOverrideReasons = userOverridesList
                .Where(uo => !string.IsNullOrWhiteSpace(uo.Reason))
                .GroupBy(uo => uo.Reason!)
                .ToDictionary(g => g.Key, g => g.Count())
        };

        return new PermissionMatrixStatisticsDto
        {
            SystemStatistics = systemStats,
            RoleStatistics = roleStats,
            PermissionUsage = permissionUsage,
            CategoryStatistics = categoryStats,
            UserOverrideStatistics = userOverrideStats
        };
    }

    /// <inheritdoc />
    public async Task<PermissionMatrixExportDto> ExportRolePermissionMatrixAsync(string format, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(format))
            throw new ArgumentException("Export format cannot be empty.", nameof(format));

        _logger.LogInformation("Exporting role-permission matrix in format: {Format}", format);

        var matrix = await GetRolePermissionMatrixAsync(cancellationToken);
        
        return format.ToUpperInvariant() switch
        {
            "CSV" => await ExportToCsvAsync(matrix, cancellationToken),
            "JSON" => await ExportToJsonAsync(matrix, cancellationToken),
            "EXCEL" => await ExportToExcelAsync(matrix, cancellationToken),
            _ => throw new ArgumentException($"Unsupported export format: {format}", nameof(format))
        };
    }

    /// <inheritdoc />
    public async Task<PermissionMatrixImportResultDto> ImportRolePermissionMatrixAsync(PermissionMatrixImportDto importData, bool overwriteExisting = false, CancellationToken cancellationToken = default)
    {
        if (importData == null)
            throw new ArgumentNullException(nameof(importData));

        _logger.LogInformation("Importing role-permission matrix from format: {Format}", importData.Format);

        var result = new PermissionMatrixImportResultDto
        {
            IsSuccess = false,
            ProcessedRecords = 0,
            SuccessfulImports = 0,
            FailedImports = 0,
            SkippedRecords = 0,
            Errors = new List<PermissionMatrixImportErrorDto>()
        };

        try
        {
            var importResult = importData.Format.ToUpperInvariant() switch
            {
                "CSV" => await ImportFromCsvAsync(importData, overwriteExisting, cancellationToken),
                "JSON" => await ImportFromJsonAsync(importData, overwriteExisting, cancellationToken),
                "EXCEL" => await ImportFromExcelAsync(importData, overwriteExisting, cancellationToken),
                _ => throw new ArgumentException($"Unsupported import format: {importData.Format}")
            };

            result = importResult;
            result.IsSuccess = result.FailedImports == 0 || result.SuccessfulImports > 0;
            result.Summary = $"Processed {result.ProcessedRecords} records. " +
                           $"Successful: {result.SuccessfulImports}, Failed: {result.FailedImports}, Skipped: {result.SkippedRecords}";

            _logger.LogInformation("Import completed. {Summary}", result.Summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during import operation");
            result.Errors = result.Errors.Append(new PermissionMatrixImportErrorDto
            {
                RowNumber = 0,
                ErrorType = "System",
                Message = ex.Message,
                IsWarning = false
            });
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<PermissionMatrixValidationResultDto> ValidatePermissionMatrixAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating permission matrix consistency");

        var errors = new List<PermissionMatrixValidationErrorDto>();
        var warnings = new List<PermissionMatrixValidationWarningDto>();

        var roles = await _roleRepository.GetRolesWithPermissionsAsync(cancellationToken);
        var permissions = await _permissionRepository.GetAllAsync(cancellationToken);
        var users = await _userRepository.GetAllAsync(cancellationToken);
        var userOverrides = await _userPermissionRepository.GetAllAsync(cancellationToken);

        var rolesList = roles.ToList();
        var permissionsList = permissions.ToList();
        var usersList = users.ToList();
        var userOverridesList = userOverrides.ToList();

        // Validate permission hierarchy
        await ValidatePermissionHierarchy(permissionsList, errors, warnings);

        // Validate role-permission assignments
        await ValidateRolePermissionAssignments(rolesList, permissionsList, errors, warnings);

        // Validate user permission overrides
        await ValidateUserPermissionOverrides(userOverridesList, permissionsList, usersList, errors, warnings);

        // Validate orphaned permissions
        await ValidateOrphanedPermissions(permissionsList, rolesList, userOverridesList, errors, warnings);

        var summary = new PermissionMatrixValidationSummaryDto
        {
            ErrorCount = errors.Count,
            WarningCount = warnings.Count,
            RolesValidated = rolesList.Count,
            PermissionsValidated = permissionsList.Count,
            AssignmentsValidated = rolesList.Sum(r => r.GetPermissions().Count()),
            UserOverridesValidated = userOverridesList.Count,
            ChecksPerformed = new[]
            {
                "Permission Hierarchy",
                "Role-Permission Assignments",
                "User Permission Overrides",
                "Orphaned Permissions"
            }
        };

        return new PermissionMatrixValidationResultDto
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings,
            Summary = summary
        };
    }

    /// <inheritdoc />
    public async Task<IEnumerable<PermissionMatrixChangeDto>> GetPermissionMatrixHistoryAsync(
        DateTime fromDate, 
        DateTime toDate, 
        Guid? roleId = null, 
        Guid? userId = null, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting permission matrix history from {FromDate} to {ToDate}", fromDate, toDate);

        // This would typically query an audit log table
        // For now, return empty collection as audit functionality would be implemented separately
        return new List<PermissionMatrixChangeDto>();
    }

    #region Private Helper Methods

    /// <summary>
    /// Builds the permission matrix DTO from roles and permissions
    /// </summary>
    private async Task<PermissionMatrixDto> BuildPermissionMatrixAsync(
        IEnumerable<Role> roles, 
        IEnumerable<Permission> permissions, 
        string? category,
        CancellationToken cancellationToken)
    {
        var rolesList = roles.ToList();
        var permissionsList = permissions.Where(p => p.IsActive).ToList();

        var assignments = new List<RolePermissionAssignmentDto>();

        foreach (var role in rolesList.Where(r => r.IsActive))
        {
            var rolePermissions = role.GetPermissions().ToList();
            
            foreach (var permission in permissionsList)
            {
                var isAssigned = rolePermissions.Any(rp => rp.Id == permission.Id);
                assignments.Add(new RolePermissionAssignmentDto
                {
                    RoleId = role.Id,
                    PermissionId = permission.Id,
                    IsAssigned = isAssigned,
                    AssignedAt = isAssigned ? role.UpdatedAt : null,
                    AssignedBy = isAssigned ? role.UpdatedBy : null
                });
            }
        }

        var categories = permissionsList.Select(p => p.Category).Distinct().ToList();
        var resources = permissionsList.Select(p => p.Resource).Distinct().ToList();

        return new PermissionMatrixDto
        {
            Roles = _mapper.Map<IEnumerable<RoleDto>>(rolesList.Where(r => r.IsActive)),
            Permissions = _mapper.Map<IEnumerable<PermissionDto>>(permissionsList),
            Assignments = assignments,
            Metadata = new PermissionMatrixMetadataDto
            {
                TotalRoles = rolesList.Count(r => r.IsActive),
                TotalPermissions = permissionsList.Count,
                TotalAssignments = assignments.Count(a => a.IsAssigned),
                Categories = categories,
                Resources = resources
            }
        };
    }

    /// <summary>
    /// Builds the user permission matrix DTO
    /// </summary>
    private async Task<UserPermissionMatrixDto> BuildUserPermissionMatrixAsync(
        User user,
        IEnumerable<Role> userRoles,
        IEnumerable<Permission> allPermissions,
        IEnumerable<UserPermission> userOverrides,
        string? category,
        CancellationToken cancellationToken)
    {
        var rolesList = userRoles.Where(r => r.IsActive).ToList();
        var permissionsList = allPermissions.Where(p => p.IsActive).ToList();
        var overridesList = userOverrides.Where(uo => uo.IsActive()).ToList();

        // Get permissions from roles
        var rolePermissions = new List<Permission>();
        foreach (var role in rolesList)
        {
            rolePermissions.AddRange(role.GetPermissions().Where(p => p.IsActive));
        }
        rolePermissions = rolePermissions.Distinct().ToList();

        // Calculate effective permissions
        var effectivePermissionNames = user.GetEffectivePermissions(permissionsList, rolesList).ToList();
        var effectivePermissions = permissionsList.Where(p => effectivePermissionNames.Contains(p.Name)).ToList();

        var categories = permissionsList.Select(p => p.Category).Distinct().ToList();
        var resources = permissionsList.Select(p => p.Resource).Distinct().ToList();

        return new UserPermissionMatrixDto
        {
            User = _mapper.Map<UserDto>(user),
            RolePermissions = _mapper.Map<IEnumerable<PermissionDto>>(rolePermissions),
            UserOverrides = _mapper.Map<IEnumerable<UserPermissionOverrideDto>>(overridesList),
            EffectivePermissions = _mapper.Map<IEnumerable<PermissionDto>>(effectivePermissions),
            Metadata = new PermissionMatrixMetadataDto
            {
                TotalRoles = rolesList.Count,
                TotalPermissions = effectivePermissions.Count,
                TotalAssignments = effectivePermissions.Count,
                Categories = categories,
                Resources = resources
            }
        };
    }

    /// <summary>
    /// Builds permission comparison DTO
    /// </summary>
    private PermissionMatrixComparisonDto BuildPermissionComparison(
        PermissionEntityDto firstEntity,
        PermissionEntityDto secondEntity,
        IEnumerable<Permission> firstPermissions,
        IEnumerable<Permission> secondPermissions)
    {
        var firstPermissionsList = firstPermissions.ToList();
        var secondPermissionsList = secondPermissions.ToList();

        var firstPermissionIds = firstPermissionsList.Select(p => p.Id).ToHashSet();
        var secondPermissionIds = secondPermissionsList.Select(p => p.Id).ToHashSet();

        var commonPermissionIds = firstPermissionIds.Intersect(secondPermissionIds).ToHashSet();
        var firstOnlyPermissionIds = firstPermissionIds.Except(secondPermissionIds).ToHashSet();
        var secondOnlyPermissionIds = secondPermissionIds.Except(firstPermissionIds).ToHashSet();

        var commonPermissions = firstPermissionsList.Where(p => commonPermissionIds.Contains(p.Id));
        var firstOnlyPermissions = firstPermissionsList.Where(p => firstOnlyPermissionIds.Contains(p.Id));
        var secondOnlyPermissions = secondPermissionsList.Where(p => secondOnlyPermissionIds.Contains(p.Id));

        var totalUniquePermissions = firstPermissionIds.Union(secondPermissionIds).Count();
        var similarityPercentage = totalUniquePermissions > 0 ? 
            (double)commonPermissionIds.Count / totalUniquePermissions * 100 : 100;

        return new PermissionMatrixComparisonDto
        {
            FirstEntity = firstEntity,
            SecondEntity = secondEntity,
            CommonPermissions = _mapper.Map<IEnumerable<PermissionDto>>(commonPermissions),
            FirstEntityOnlyPermissions = _mapper.Map<IEnumerable<PermissionDto>>(firstOnlyPermissions),
            SecondEntityOnlyPermissions = _mapper.Map<IEnumerable<PermissionDto>>(secondOnlyPermissions),
            Statistics = new PermissionComparisonStatisticsDto
            {
                CommonPermissionsCount = commonPermissionIds.Count,
                FirstEntityUniqueCount = firstOnlyPermissionIds.Count,
                SecondEntityUniqueCount = secondOnlyPermissionIds.Count,
                SimilarityPercentage = similarityPercentage,
                TotalUniquePermissions = totalUniquePermissions
            }
        };
    }  
  /// <summary>
    /// Exports matrix to CSV format
    /// </summary>
    private async Task<PermissionMatrixExportDto> ExportToCsvAsync(PermissionMatrixDto matrix, CancellationToken cancellationToken)
    {
        var csv = new StringBuilder();
        
        // Header row
        csv.Append("Role,");
        csv.AppendLine(string.Join(",", matrix.Permissions.Select(p => p.Name)));

        // Data rows
        foreach (var role in matrix.Roles)
        {
            csv.Append($"{role.Name},");
            var roleAssignments = matrix.Assignments.Where(a => a.RoleId == role.Id).ToDictionary(a => a.PermissionId, a => a.IsAssigned);
            
            var values = matrix.Permissions.Select(p => roleAssignments.GetValueOrDefault(p.Id, false) ? "1" : "0");
            csv.AppendLine(string.Join(",", values));
        }

        var data = Encoding.UTF8.GetBytes(csv.ToString());
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

        return new PermissionMatrixExportDto
        {
            Format = "CSV",
            Data = data,
            ContentType = "text/csv",
            FileName = $"permission_matrix_{timestamp}.csv",
            Metadata = new PermissionMatrixExportMetadataDto
            {
                RoleCount = matrix.Roles.Count(),
                PermissionCount = matrix.Permissions.Count(),
                AssignmentCount = matrix.Assignments.Count(a => a.IsAssigned)
            }
        };
    }

    /// <summary>
    /// Exports matrix to JSON format
    /// </summary>
    private async Task<PermissionMatrixExportDto> ExportToJsonAsync(PermissionMatrixDto matrix, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(matrix, new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        
        var data = Encoding.UTF8.GetBytes(json);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

        return new PermissionMatrixExportDto
        {
            Format = "JSON",
            Data = data,
            ContentType = "application/json",
            FileName = $"permission_matrix_{timestamp}.json",
            Metadata = new PermissionMatrixExportMetadataDto
            {
                RoleCount = matrix.Roles.Count(),
                PermissionCount = matrix.Permissions.Count(),
                AssignmentCount = matrix.Assignments.Count(a => a.IsAssigned)
            }
        };
    }

    /// <summary>
    /// Exports matrix to Excel format (placeholder - would require Excel library)
    /// </summary>
    private async Task<PermissionMatrixExportDto> ExportToExcelAsync(PermissionMatrixDto matrix, CancellationToken cancellationToken)
    {
        // For now, export as CSV with Excel content type
        // In a real implementation, you would use a library like EPPlus or ClosedXML
        var csvExport = await ExportToCsvAsync(matrix, cancellationToken);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

        return new PermissionMatrixExportDto
        {
            Format = "EXCEL",
            Data = csvExport.Data,
            ContentType = "application/vnd.ms-excel",
            FileName = $"permission_matrix_{timestamp}.xls",
            Metadata = csvExport.Metadata
        };
    }

    /// <summary>
    /// Imports matrix from CSV format (placeholder implementation)
    /// </summary>
    private async Task<PermissionMatrixImportResultDto> ImportFromCsvAsync(PermissionMatrixImportDto importData, bool overwriteExisting, CancellationToken cancellationToken)
    {
        // Placeholder implementation - would parse CSV and update role permissions
        return new PermissionMatrixImportResultDto
        {
            IsSuccess = false,
            ProcessedRecords = 0,
            SuccessfulImports = 0,
            FailedImports = 0,
            SkippedRecords = 0,
            Errors = new List<PermissionMatrixImportErrorDto>
            {
                new()
                {
                    RowNumber = 0,
                    ErrorType = "NotImplemented",
                    Message = "CSV import functionality not yet implemented",
                    IsWarning = false
                }
            },
            Summary = "CSV import not implemented"
        };
    }

    /// <summary>
    /// Imports matrix from JSON format (placeholder implementation)
    /// </summary>
    private async Task<PermissionMatrixImportResultDto> ImportFromJsonAsync(PermissionMatrixImportDto importData, bool overwriteExisting, CancellationToken cancellationToken)
    {
        // Placeholder implementation - would parse JSON and update role permissions
        return new PermissionMatrixImportResultDto
        {
            IsSuccess = false,
            ProcessedRecords = 0,
            SuccessfulImports = 0,
            FailedImports = 0,
            SkippedRecords = 0,
            Errors = new List<PermissionMatrixImportErrorDto>
            {
                new()
                {
                    RowNumber = 0,
                    ErrorType = "NotImplemented",
                    Message = "JSON import functionality not yet implemented",
                    IsWarning = false
                }
            },
            Summary = "JSON import not implemented"
        };
    }

    /// <summary>
    /// Imports matrix from Excel format (placeholder implementation)
    /// </summary>
    private async Task<PermissionMatrixImportResultDto> ImportFromExcelAsync(PermissionMatrixImportDto importData, bool overwriteExisting, CancellationToken cancellationToken)
    {
        // Placeholder implementation - would parse Excel and update role permissions
        return new PermissionMatrixImportResultDto
        {
            IsSuccess = false,
            ProcessedRecords = 0,
            SuccessfulImports = 0,
            FailedImports = 0,
            SkippedRecords = 0,
            Errors = new List<PermissionMatrixImportErrorDto>
            {
                new()
                {
                    RowNumber = 0,
                    ErrorType = "NotImplemented",
                    Message = "Excel import functionality not yet implemented",
                    IsWarning = false
                }
            },
            Summary = "Excel import not implemented"
        };
    }

    /// <summary>
    /// Validates permission hierarchy for circular references
    /// </summary>
    private async Task ValidatePermissionHierarchy(
        List<Permission> permissions, 
        List<PermissionMatrixValidationErrorDto> errors, 
        List<PermissionMatrixValidationWarningDto> warnings)
    {
        var permissionDict = permissions.ToDictionary(p => p.Id);

        foreach (var permission in permissions)
        {
            if (permission.ParentPermissionId.HasValue)
            {
                // Check if parent exists
                if (!permissionDict.ContainsKey(permission.ParentPermissionId.Value))
                {
                    errors.Add(new PermissionMatrixValidationErrorDto
                    {
                        Code = "PARENT_NOT_FOUND",
                        Message = $"Permission '{permission.Name}' references non-existent parent permission",
                        Severity = ValidationSeverity.High,
                        EntityType = "Permission",
                        EntityId = permission.Id,
                        EntityName = permission.Name
                    });
                }
                else
                {
                    // Check for circular references
                    if (HasCircularReference(permission, permissionDict, new HashSet<Guid>()))
                    {
                        errors.Add(new PermissionMatrixValidationErrorDto
                        {
                            Code = "CIRCULAR_REFERENCE",
                            Message = $"Permission '{permission.Name}' has circular reference in hierarchy",
                            Severity = ValidationSeverity.Critical,
                            EntityType = "Permission",
                            EntityId = permission.Id,
                            EntityName = permission.Name
                        });
                    }
                }
            }
        }
    }

    /// <summary>
    /// Validates role-permission assignments
    /// </summary>
    private async Task ValidateRolePermissionAssignments(
        List<Role> roles, 
        List<Permission> permissions, 
        List<PermissionMatrixValidationErrorDto> errors, 
        List<PermissionMatrixValidationWarningDto> warnings)
    {
        var permissionDict = permissions.ToDictionary(p => p.Id);

        foreach (var role in roles)
        {
            var rolePermissions = role.GetPermissions().ToList();
            
            // Check for invalid permission references
            foreach (var permission in rolePermissions)
            {
                if (!permissionDict.ContainsKey(permission.Id))
                {
                    errors.Add(new PermissionMatrixValidationErrorDto
                    {
                        Code = "INVALID_PERMISSION_REFERENCE",
                        Message = $"Role '{role.Name}' references non-existent permission",
                        Severity = ValidationSeverity.High,
                        EntityType = "Role",
                        EntityId = role.Id,
                        EntityName = role.Name
                    });
                }
                else if (!permission.IsActive)
                {
                    warnings.Add(new PermissionMatrixValidationWarningDto
                    {
                        Code = "INACTIVE_PERMISSION_ASSIGNED",
                        Message = $"Role '{role.Name}' has inactive permission '{permission.Name}' assigned",
                        EntityType = "Role",
                        EntityId = role.Id,
                        EntityName = role.Name,
                        SuggestedAction = "Remove inactive permission or reactivate permission"
                    });
                }
            }

            // Check for roles with no permissions
            if (!rolePermissions.Any())
            {
                warnings.Add(new PermissionMatrixValidationWarningDto
                {
                    Code = "ROLE_NO_PERMISSIONS",
                    Message = $"Role '{role.Name}' has no permissions assigned",
                    EntityType = "Role",
                    EntityId = role.Id,
                    EntityName = role.Name,
                    SuggestedAction = "Assign permissions to role or consider removing unused role"
                });
            }
        }
    }

    /// <summary>
    /// Validates user permission overrides
    /// </summary>
    private async Task ValidateUserPermissionOverrides(
        List<UserPermission> userOverrides, 
        List<Permission> permissions, 
        List<User> users,
        List<PermissionMatrixValidationErrorDto> errors, 
        List<PermissionMatrixValidationWarningDto> warnings)
    {
        var permissionDict = permissions.ToDictionary(p => p.Id);
        var userDict = users.ToDictionary(u => u.Id);

        foreach (var userOverride in userOverrides)
        {
            // Check if permission exists
            if (!permissionDict.ContainsKey(userOverride.PermissionId))
            {
                errors.Add(new PermissionMatrixValidationErrorDto
                {
                    Code = "INVALID_PERMISSION_OVERRIDE",
                    Message = $"User permission override references non-existent permission",
                    Severity = ValidationSeverity.High,
                    EntityType = "UserPermission",
                    EntityId = userOverride.Id,
                    Context = new Dictionary<string, object> { { "UserId", userOverride.UserId } }
                });
            }

            // Check if user exists
            if (!userDict.ContainsKey(userOverride.UserId))
            {
                errors.Add(new PermissionMatrixValidationErrorDto
                {
                    Code = "INVALID_USER_OVERRIDE",
                    Message = $"User permission override references non-existent user",
                    Severity = ValidationSeverity.High,
                    EntityType = "UserPermission",
                    EntityId = userOverride.Id,
                    Context = new Dictionary<string, object> { { "PermissionId", userOverride.PermissionId } }
                });
            }

            // Check for expired overrides
            if (userOverride.IsExpired())
            {
                warnings.Add(new PermissionMatrixValidationWarningDto
                {
                    Code = "EXPIRED_USER_OVERRIDE",
                    Message = $"User permission override has expired",
                    EntityType = "UserPermission",
                    EntityId = userOverride.Id,
                    SuggestedAction = "Remove expired override or extend expiration date"
                });
            }
        }
    }

    /// <summary>
    /// Validates orphaned permissions
    /// </summary>
    private async Task ValidateOrphanedPermissions(
        List<Permission> permissions, 
        List<Role> roles, 
        List<UserPermission> userOverrides,
        List<PermissionMatrixValidationErrorDto> errors, 
        List<PermissionMatrixValidationWarningDto> warnings)
    {
        var rolePermissionIds = roles.SelectMany(r => r.GetPermissions().Select(p => p.Id)).ToHashSet();
        var userOverridePermissionIds = userOverrides.Select(uo => uo.PermissionId).ToHashSet();

        foreach (var permission in permissions.Where(p => p.IsActive))
        {
            if (!rolePermissionIds.Contains(permission.Id) && !userOverridePermissionIds.Contains(permission.Id))
            {
                warnings.Add(new PermissionMatrixValidationWarningDto
                {
                    Code = "ORPHANED_PERMISSION",
                    Message = $"Permission '{permission.Name}' is not assigned to any role or user",
                    EntityType = "Permission",
                    EntityId = permission.Id,
                    EntityName = permission.Name,
                    SuggestedAction = "Assign permission to roles or consider deactivating unused permission"
                });
            }
        }
    }

    /// <summary>
    /// Checks for circular references in permission hierarchy
    /// </summary>
    private bool HasCircularReference(Permission permission, Dictionary<Guid, Permission> permissionDict, HashSet<Guid> visited)
    {
        if (visited.Contains(permission.Id))
            return true;

        visited.Add(permission.Id);

        if (permission.ParentPermissionId.HasValue && 
            permissionDict.TryGetValue(permission.ParentPermissionId.Value, out var parent))
        {
            return HasCircularReference(parent, permissionDict, visited);
        }

        visited.Remove(permission.Id);
        return false;
    }

    #endregion
}