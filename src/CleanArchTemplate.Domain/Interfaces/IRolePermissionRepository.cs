using CleanArchTemplate.Domain.Entities;

namespace CleanArchTemplate.Domain.Interfaces;

public interface IRolePermissionRepository : IRepository<RolePermission>
{
    Task<RolePermission?> GetByRoleAndPermissionAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<RolePermission>> GetByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task<IEnumerable<RolePermission>> GetByPermissionIdAsync(Guid permissionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<RolePermission>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default);
    Task<bool> RoleHasPermissionAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default);
    Task RemoveAllPermissionsFromRoleAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task RemovePermissionFromAllRolesAsync(Guid permissionId, CancellationToken cancellationToken = default);
}