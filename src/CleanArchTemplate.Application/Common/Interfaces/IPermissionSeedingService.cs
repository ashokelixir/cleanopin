using CleanArchTemplate.Domain.Entities;

namespace CleanArchTemplate.Application.Common.Interfaces;

/// <summary>
/// Service for seeding and managing default permissions
/// </summary>
public interface IPermissionSeedingService
{
    /// <summary>
    /// Seeds default permissions for the application
    /// </summary>
    Task SeedDefaultPermissionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Seeds environment-specific permissions
    /// </summary>
    /// <param name="environment">Environment name (Development, Staging, Production)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SeedEnvironmentPermissionsAsync(string environment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Seeds default role-permission assignments
    /// </summary>
    Task SeedDefaultRolePermissionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all default permissions that should exist in the system
    /// </summary>
    Task<IEnumerable<Permission>> GetDefaultPermissionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that all required permissions exist
    /// </summary>
    Task<bool> ValidatePermissionIntegrityAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up orphaned permissions that are no longer used
    /// </summary>
    Task CleanupOrphanedPermissionsAsync(CancellationToken cancellationToken = default);
}