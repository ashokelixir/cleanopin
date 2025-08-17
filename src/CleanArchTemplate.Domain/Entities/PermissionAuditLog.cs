using CleanArchTemplate.Domain.Common;

namespace CleanArchTemplate.Domain.Entities;

public class PermissionAuditLog : BaseEntity
{
    public Guid? UserId { get; private set; }
    public Guid? RoleId { get; private set; }
    public Guid PermissionId { get; private set; }
    public string Action { get; private set; } = string.Empty; // Assigned, Removed, Modified
    public string? OldValue { get; private set; }
    public string? NewValue { get; private set; }
    public string? Reason { get; private set; }
    public string PerformedBy { get; private set; } = string.Empty;
    public DateTime PerformedAt { get; private set; }

    // Navigation properties
    public User? User { get; private set; }
    public Role? Role { get; private set; }
    public Permission Permission { get; private set; } = null!;

    private PermissionAuditLog() { } // For EF Core

    public PermissionAuditLog(
        Guid? userId,
        Guid? roleId,
        Guid permissionId,
        string action,
        string? oldValue,
        string? newValue,
        string? reason,
        string performedBy)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        RoleId = roleId;
        PermissionId = permissionId;
        Action = action;
        OldValue = oldValue;
        NewValue = newValue;
        Reason = reason;
        PerformedBy = performedBy;
        PerformedAt = DateTime.UtcNow;
    }

    public static PermissionAuditLog CreateUserPermissionAssigned(
        Guid userId, 
        Guid permissionId, 
        string performedBy, 
        string? reason = null)
    {
        return new PermissionAuditLog(userId, null, permissionId, "Assigned", null, "Granted", reason, performedBy);
    }

    public static PermissionAuditLog CreateUserPermissionRemoved(
        Guid userId, 
        Guid permissionId, 
        string performedBy, 
        string? reason = null)
    {
        return new PermissionAuditLog(userId, null, permissionId, "Removed", "Granted", null, reason, performedBy);
    }

    public static PermissionAuditLog CreateRolePermissionAssigned(
        Guid roleId, 
        Guid permissionId, 
        string performedBy, 
        string? reason = null)
    {
        return new PermissionAuditLog(null, roleId, permissionId, "Assigned", null, "Granted", reason, performedBy);
    }

    public static PermissionAuditLog CreateRolePermissionRemoved(
        Guid roleId, 
        Guid permissionId, 
        string performedBy, 
        string? reason = null)
    {
        return new PermissionAuditLog(null, roleId, permissionId, "Removed", "Granted", null, reason, performedBy);
    }

    public static PermissionAuditLog CreatePermissionModified(
        Guid permissionId, 
        string oldValue, 
        string newValue, 
        string performedBy, 
        string? reason = null)
    {
        return new PermissionAuditLog(null, null, permissionId, "Modified", oldValue, newValue, reason, performedBy);
    }
}