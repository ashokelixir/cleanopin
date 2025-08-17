using CleanArchTemplate.Domain.Entities;

namespace CleanArchTemplate.Domain.Interfaces;

public interface IPermissionAuditLogRepository : IRepository<PermissionAuditLog>
{
    Task<IEnumerable<PermissionAuditLog>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<PermissionAuditLog>> GetByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task<IEnumerable<PermissionAuditLog>> GetByPermissionIdAsync(Guid permissionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<PermissionAuditLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<PermissionAuditLog>> GetByActionAsync(string action, CancellationToken cancellationToken = default);
    Task<IEnumerable<PermissionAuditLog>> GetByPerformerAsync(string performedBy, CancellationToken cancellationToken = default);
}