# Permission Matrix Management System Design

## Overview

The Permission Matrix Management System extends the existing .NET 8 Clean Architecture Template with a comprehensive, fine-grained permission management system. This system builds upon the existing Role and Permission entities to provide a matrix-based approach for managing permissions across roles and users, with support for user-specific permission overrides, efficient caching, and comprehensive audit trails.

The design leverages the existing Clean Architecture patterns with Domain-Driven Design principles, maintaining separation of concerns while providing high-performance permission evaluation suitable for enterprise applications.

## Architecture

### High-Level Architecture

The system follows the existing Clean Architecture pattern with four main layers:

```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer                       │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────┐ │
│  │ Permission APIs │  │ Role Matrix API │  │ Audit APIs   │ │
│  └─────────────────┘  └─────────────────┘  └──────────────┘ │
└─────────────────────────────────────────────────────────────┘
┌─────────────────────────────────────────────────────────────┐
│                   Application Layer                         │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────┐ │
│  │ Permission      │  │ Matrix          │  │ Authorization│ │
│  │ Commands/Queries│  │ Commands/Queries│  │ Services     │ │
│  └─────────────────┘  └─────────────────┘  └──────────────┘ │
└─────────────────────────────────────────────────────────────┘
┌─────────────────────────────────────────────────────────────┐
│                     Domain Layer                            │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────┐ │
│  │ Enhanced        │  │ User Permission │  │ Permission   │ │
│  │ Permission      │  │ Override        │  │ Evaluation   │ │
│  │ Entity          │  │ Entity          │  │ Services     │ │
│  └─────────────────┘  └─────────────────┘  └──────────────┘ │
└─────────────────────────────────────────────────────────────┘
┌─────────────────────────────────────────────────────────────┐
│                  Infrastructure Layer                       │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────┐ │
│  │ EF Core         │  │ Redis Cache     │  │ Audit Log    │ │
│  │ Repositories    │  │ Service         │  │ Service      │ │
│  └─────────────────┘  └─────────────────┘  └──────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

### Design Rationale

**Resource-Action Permission Model**: The system uses a resource-action based permission model instead of simple string-based permissions. This provides better organization and supports hierarchical permission structures.

**User Permission Overrides**: Direct user permission assignments take precedence over role-based permissions, allowing for exceptional access scenarios while maintaining role-based defaults.

**Caching Strategy**: Multi-level caching with Redis for distributed scenarios and in-memory caching for single-instance deployments ensures sub-100ms permission evaluation.

**Audit-First Design**: All permission changes are logged with comprehensive audit trails to support compliance and security requirements.

## Components and Interfaces

### Domain Layer Enhancements

#### Enhanced Permission Entity

The existing Permission entity will be enhanced to support the resource-action model:

```csharp
public class Permission : BaseAuditableEntity
{
    public string Resource { get; private set; } // e.g., "Users", "Reports", "Settings"
    public string Action { get; private set; }   // e.g., "Create", "Read", "Update", "Delete"
    public string Name { get; private set; }     // Computed: "{Resource}.{Action}"
    public string Description { get; private set; }
    public string Category { get; private set; }
    public bool IsActive { get; private set; }
    public Guid? ParentPermissionId { get; private set; } // For hierarchical permissions
    
    // Navigation properties
    public Permission? ParentPermission { get; private set; }
    public IReadOnlyCollection<Permission> ChildPermissions { get; private set; }
    public IReadOnlyCollection<RolePermission> RolePermissions { get; private set; }
    public IReadOnlyCollection<UserPermission> UserPermissions { get; private set; }
}
```

#### New UserPermission Entity

```csharp
public class UserPermission : BaseAuditableEntity
{
    public Guid UserId { get; private set; }
    public Guid PermissionId { get; private set; }
    public PermissionState State { get; private set; } // Grant, Deny
    public string? Reason { get; private set; } // Optional reason for the override
    public DateTime? ExpiresAt { get; private set; } // Optional expiration
    
    // Navigation properties
    public User User { get; private set; }
    public Permission Permission { get; private set; }
}

public enum PermissionState
{
    Grant = 1,
    Deny = 2
}
```

#### Permission Evaluation Service

```csharp
public interface IPermissionEvaluationService
{
    Task<bool> HasPermissionAsync(Guid userId, string resource, string action, CancellationToken cancellationToken = default);
    Task<bool> HasPermissionAsync(Guid userId, string permissionName, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> HasAnyPermissionAsync(Guid userId, IEnumerable<string> permissions, CancellationToken cancellationToken = default);
    Task InvalidateUserPermissionCacheAsync(Guid userId, CancellationToken cancellationToken = default);
}
```

### Application Layer Components

#### Permission Management Commands and Queries

**Commands:**
- `CreatePermissionCommand` - Create new permissions
- `UpdatePermissionCommand` - Update existing permissions
- `DeletePermissionCommand` - Soft delete permissions
- `AssignPermissionToRoleCommand` - Assign permissions to roles
- `RemovePermissionFromRoleCommand` - Remove permissions from roles
- `AssignPermissionToUserCommand` - Direct user permission assignment
- `RemovePermissionFromUserCommand` - Remove user permission overrides
- `BulkAssignPermissionsCommand` - Batch permission assignments

**Queries:**
- `GetPermissionByIdQuery` - Get single permission details
- `GetPermissionsQuery` - Get paginated permission list with filtering
- `GetRolePermissionMatrixQuery` - Get role-permission matrix data
- `GetUserPermissionsQuery` - Get effective user permissions
- `GetPermissionAuditLogQuery` - Get permission change audit trail

#### Authorization Services

```csharp
public interface IPermissionAuthorizationService
{
    Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, string resource, string action);
    Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, string permission);
    Task<IEnumerable<string>> GetUserPermissionsAsync(ClaimsPrincipal user);
}
```

#### Caching Service

```csharp
public interface IPermissionCacheService
{
    Task<IEnumerable<string>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task SetUserPermissionsAsync(Guid userId, IEnumerable<string> permissions, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
    Task InvalidateUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task InvalidateRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken = default);
}
```

### Infrastructure Layer Components

#### Enhanced Repository Interfaces

```csharp
public interface IPermissionRepository : IRepository<Permission>
{
    Task<IEnumerable<Permission>> GetByResourceAsync(string resource, CancellationToken cancellationToken = default);
    Task<IEnumerable<Permission>> GetByResourceAndActionAsync(string resource, string action, CancellationToken cancellationToken = default);
    Task<Permission?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IEnumerable<Permission>> GetHierarchicalPermissionsAsync(Guid parentId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string resource, string action, CancellationToken cancellationToken = default);
}

public interface IUserPermissionRepository : IRepository<UserPermission>
{
    Task<IEnumerable<UserPermission>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserPermission?> GetByUserAndPermissionAsync(Guid userId, Guid permissionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserPermission>> GetExpiringPermissionsAsync(DateTime before, CancellationToken cancellationToken = default);
}
```

#### Permission Matrix Service

```csharp
public interface IPermissionMatrixService
{
    Task<PermissionMatrixDto> GetRolePermissionMatrixAsync(CancellationToken cancellationToken = default);
    Task<UserPermissionMatrixDto> GetUserPermissionMatrixAsync(Guid userId, CancellationToken cancellationToken = default);
    Task UpdateRolePermissionMatrixAsync(Guid roleId, IEnumerable<Guid> permissionIds, CancellationToken cancellationToken = default);
}
```

## Data Models

### Database Schema Changes

#### Enhanced Permissions Table
```sql
CREATE TABLE Permissions (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Resource NVARCHAR(100) NOT NULL,
    Action NVARCHAR(100) NOT NULL,
    Name AS (Resource + '.' + Action) PERSISTED,
    Description NVARCHAR(500) NOT NULL,
    Category NVARCHAR(50) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    ParentPermissionId UNIQUEIDENTIFIER NULL,
    CreatedAt DATETIME2 NOT NULL,
    CreatedBy NVARCHAR(256) NULL,
    LastModifiedAt DATETIME2 NULL,
    LastModifiedBy NVARCHAR(256) NULL,
    
    CONSTRAINT FK_Permissions_ParentPermission FOREIGN KEY (ParentPermissionId) REFERENCES Permissions(Id),
    CONSTRAINT UQ_Permissions_Resource_Action UNIQUE (Resource, Action),
    INDEX IX_Permissions_Resource (Resource),
    INDEX IX_Permissions_Action (Action),
    INDEX IX_Permissions_Category (Category),
    INDEX IX_Permissions_ParentPermissionId (ParentPermissionId)
);
```

#### New UserPermissions Table
```sql
CREATE TABLE UserPermissions (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL,
    PermissionId UNIQUEIDENTIFIER NOT NULL,
    State INT NOT NULL, -- 1 = Grant, 2 = Deny
    Reason NVARCHAR(500) NULL,
    ExpiresAt DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL,
    CreatedBy NVARCHAR(256) NULL,
    LastModifiedAt DATETIME2 NULL,
    LastModifiedBy NVARCHAR(256) NULL,
    
    CONSTRAINT FK_UserPermissions_User FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_UserPermissions_Permission FOREIGN KEY (PermissionId) REFERENCES Permissions(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_UserPermissions_User_Permission UNIQUE (UserId, PermissionId),
    INDEX IX_UserPermissions_UserId (UserId),
    INDEX IX_UserPermissions_PermissionId (PermissionId),
    INDEX IX_UserPermissions_ExpiresAt (ExpiresAt)
);
```

#### Permission Audit Log Table
```sql
CREATE TABLE PermissionAuditLogs (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NULL,
    RoleId UNIQUEIDENTIFIER NULL,
    PermissionId UNIQUEIDENTIFIER NOT NULL,
    Action NVARCHAR(50) NOT NULL, -- Assigned, Removed, Modified
    OldValue NVARCHAR(MAX) NULL,
    NewValue NVARCHAR(MAX) NULL,
    Reason NVARCHAR(500) NULL,
    PerformedBy NVARCHAR(256) NOT NULL,
    PerformedAt DATETIME2 NOT NULL,
    
    INDEX IX_PermissionAuditLogs_UserId (UserId),
    INDEX IX_PermissionAuditLogs_RoleId (RoleId),
    INDEX IX_PermissionAuditLogs_PermissionId (PermissionId),
    INDEX IX_PermissionAuditLogs_PerformedAt (PerformedAt)
);
```

### DTOs and Response Models

#### Permission Matrix DTOs
```csharp
public class PermissionMatrixDto
{
    public IEnumerable<RoleDto> Roles { get; set; } = new List<RoleDto>();
    public IEnumerable<PermissionDto> Permissions { get; set; } = new List<PermissionDto>();
    public IEnumerable<RolePermissionAssignmentDto> Assignments { get; set; } = new List<RolePermissionAssignmentDto>();
}

public class UserPermissionMatrixDto
{
    public UserDto User { get; set; } = null!;
    public IEnumerable<PermissionDto> RolePermissions { get; set; } = new List<PermissionDto>();
    public IEnumerable<UserPermissionOverrideDto> UserOverrides { get; set; } = new List<UserPermissionOverrideDto>();
    public IEnumerable<PermissionDto> EffectivePermissions { get; set; } = new List<PermissionDto>();
}

public class PermissionDto
{
    public Guid Id { get; set; }
    public string Resource { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public Guid? ParentPermissionId { get; set; }
    public IEnumerable<PermissionDto> ChildPermissions { get; set; } = new List<PermissionDto>();
}
```

## Error Handling

### Custom Exceptions

```csharp
public class PermissionNotFoundException : DomainException
{
    public PermissionNotFoundException(string permissionName) 
        : base($"Permission '{permissionName}' was not found.") { }
}

public class DuplicatePermissionException : DomainException
{
    public DuplicatePermissionException(string resource, string action) 
        : base($"Permission for resource '{resource}' and action '{action}' already exists.") { }
}

public class CircularPermissionHierarchyException : DomainException
{
    public CircularPermissionHierarchyException() 
        : base("Circular reference detected in permission hierarchy.") { }
}

public class InsufficientPermissionException : DomainException
{
    public InsufficientPermissionException(string permission) 
        : base($"Insufficient permissions. Required: {permission}") { }
}
```

### Error Response Models

```csharp
public class PermissionErrorResponse
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? RequiredPermission { get; set; }
    public IEnumerable<string> UserPermissions { get; set; } = new List<string>();
}
```

### Global Exception Handling

The existing global exception handler will be extended to handle permission-specific exceptions with appropriate HTTP status codes:

- `PermissionNotFoundException` → 404 Not Found
- `DuplicatePermissionException` → 409 Conflict
- `InsufficientPermissionException` → 403 Forbidden
- `CircularPermissionHierarchyException` → 400 Bad Request

## Testing Strategy

### Unit Testing Approach

**Domain Layer Testing:**
- Permission entity business logic validation
- UserPermission entity state management
- Permission hierarchy validation
- Domain event generation

**Application Layer Testing:**
- Command and query handler logic
- Permission evaluation service algorithms
- Caching behavior validation
- Authorization service logic

**Infrastructure Layer Testing:**
- Repository implementation testing with in-memory database
- Cache service behavior with mock Redis
- Database query optimization validation

### Integration Testing Strategy

**API Integration Tests:**
- End-to-end permission management workflows
- Role-permission matrix operations
- User permission override scenarios
- Performance testing for permission evaluation

**Database Integration Tests:**
- Complex permission queries
- Hierarchical permission retrieval
- Audit log generation
- Data consistency validation

### Performance Testing

**Permission Evaluation Performance:**
- Target: Sub-100ms response time for permission checks
- Load testing with 1000+ concurrent permission evaluations
- Cache hit ratio optimization
- Database query performance profiling

**Caching Performance:**
- Redis cache performance under load
- Cache invalidation efficiency
- Memory usage optimization
- Distributed cache consistency

### Security Testing

**Authorization Testing:**
- Permission bypass attempt detection
- Role escalation prevention
- User permission override security
- Audit trail integrity

**Data Protection Testing:**
- Sensitive permission data encryption
- Audit log tamper detection
- Permission data access logging
- Compliance requirement validation

## Performance and Scalability Considerations

### Caching Strategy

**Multi-Level Caching:**
1. **L1 Cache (In-Memory)**: User permissions cached for 5 minutes
2. **L2 Cache (Redis)**: Distributed cache for user and role permissions (15 minutes)
3. **Database**: Optimized with proper indexing and query optimization

**Cache Invalidation Strategy:**
- User-specific cache invalidation on permission changes
- Role-based cache invalidation affecting all users with that role
- Hierarchical cache invalidation for parent-child permission relationships

### Database Optimization

**Indexing Strategy:**
- Composite indexes on (Resource, Action) for permission lookups
- User-specific indexes for rapid permission evaluation
- Audit log indexes for compliance reporting

**Query Optimization:**
- Materialized views for complex permission matrices
- Stored procedures for bulk permission operations
- Connection pooling and query plan optimization

### Scalability Design

**Horizontal Scaling:**
- Stateless permission evaluation services
- Redis cluster support for distributed caching
- Database read replicas for permission queries

**Performance Monitoring:**
- Permission evaluation latency tracking
- Cache hit ratio monitoring
- Database query performance metrics
- API endpoint response time monitoring

## Security Considerations

### Permission Evaluation Security

**Principle of Least Privilege:**
- Default deny for all permissions
- Explicit grant requirements
- User overrides require additional authorization

**Permission Hierarchy Security:**
- Circular reference prevention
- Depth limit enforcement
- Inheritance validation

### Audit and Compliance

**Comprehensive Audit Trail:**
- All permission changes logged with timestamps
- User context and reason tracking
- Immutable audit log design
- Compliance report generation

**Data Protection:**
- Permission data encryption at rest
- Secure transmission of permission data
- Access logging for sensitive operations
- Regular permission access reviews

This design provides a robust, scalable, and secure foundation for the permission matrix management system while maintaining consistency with the existing Clean Architecture patterns and ensuring high performance for enterprise-scale applications.