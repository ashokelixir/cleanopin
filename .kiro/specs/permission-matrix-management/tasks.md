# Implementation Plan

- [x] 1. Enhance Domain Layer with Permission Matrix Entities







  - Modify the existing Permission entity to support resource-action model with hierarchical relationships
  - Create new UserPermission entity for user-specific permission overrides
  - Add PermissionState enum and related domain events
  - Update existing domain events to support new permission structure
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 3.1, 3.2, 3.3_

- [x] 2. Create Permission Evaluation Domain Service








  - Implement IPermissionEvaluationService interface in domain layer
  - Create permission evaluation logic that combines role permissions with user overrides
  - Add hierarchical permission resolution logic
  - Implement permission conflict resolution (user overrides take precedence)
  - Write unit tests for permission evaluation scenarios
  - _Requirements: 4.1, 4.4, 3.4, 2.5_

- [x] 3. Extend Application Layer Interfaces










  - Create IUserPermissionRepository interface extending IRepository<UserPermission>
  - Enhance IPermissionRepository with resource-action query methods
  - Add IPermissionCacheService interface for caching operations
  - Create IPermissionMatrixService interface for matrix operations
  - Add IPermissionAuthorizationService for authorization logic
  - _Requirements: 4.2, 4.3, 8.2, 2.1, 2.2_

- [x] 4. Implement Permission Management Commands





  - Create CreatePermissionCommand with resource-action validation
  - Implement UpdatePermissionCommand with hierarchy validation
  - Add AssignPermissionToUserCommand for user-specific overrides
  - Create RemovePermissionFromUserCommand for override removal
  - Implement BulkAssignPermissionsCommand for batch operations
  - Write command validators and unit tests
  - _Requirements: 1.1, 1.2, 3.1, 3.2, 5.3, 2.3_

- [x] 5. Implement Permission Management Queries






  - Create GetPermissionsQuery with filtering by resource and action
  - Implement GetRolePermissionMatrixQuery for matrix display
  - Add GetUserPermissionsQuery for effective user permissions
  - Create GetPermissionAuditLogQuery for audit trail access
  - Implement pagination, filtering, and sorting for all queries
  - Write query handlers and unit tests
  - _Requirements: 1.5, 2.2, 3.4, 6.2, 5.4_

- [x] 6. Create Permission Caching Service




  - Implement PermissionCacheService with Redis and in-memory caching
  - Add cache invalidation logic for user and role permission changes
  - Implement hierarchical cache invalidation for parent-child relationships
  - Create cache warming strategies for frequently accessed permissions
  - Write unit tests with mock Redis and integration tests
  - _Requirements: 8.2, 8.5, 4.1, 4.5_

- [x] 7. Implement Infrastructure Layer Repositories






  - Create UserPermissionRepository with EF Core implementation
  - Enhance PermissionRepository with resource-action query methods
  - Add database indexes for optimal permission query performance
  - Implement bulk operations for efficient permission assignments
  - Write repository unit tests with in-memory database
  - _Requirements: 8.1, 8.3, 5.3, 2.3_

- [x] 8. Create Database Migration for Schema Changes





  - Generate EF Core migration for enhanced Permission entity
  - Add UserPermissions table with proper foreign key constraints
  - Create PermissionAuditLogs table for audit trail
  - Add database indexes for performance optimization
  - Include data seeding for default permissions and roles
  - _Requirements: 7.1, 7.2, 6.1, 6.4, 8.1_

- [x] 9. Implement Permission Authorization Service





  - Create PermissionAuthorizationService for ClaimsPrincipal authorization
  - Add attribute-based authorization for controllers and actions
  - Implement bulk permission checking for multiple permissions
  - Create authorization result models with detailed error information
  - Write unit tests for authorization scenarios
  - _Requirements: 4.2, 4.4, 4.5, 5.5_

- [x] 10. Create Permission Matrix Service







  - Implement PermissionMatrixService for role-permission matrix operations
  - Add user permission matrix functionality with effective permissions
  - Create bulk role permission assignment methods
  - Implement matrix data transformation and optimization
  - Write unit tests for matrix operations
  - _Requirements: 2.1, 2.2, 2.3, 3.4, 5.2_

- [x] 11. Implement Permission Audit Service










  - Create PermissionAuditService for logging all permission changes
  - Add audit log entry creation for assignments, removals, and modifications
  - Implement audit log querying with filtering and export capabilities
  - Create compliance reporting functionality
  - Write unit tests for audit logging scenarios
  - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 3.5_

- [x] 12. Create Permission Management API Controllers





  - Implement PermissionsController with CRUD operations
  - Add PermissionMatrixController for role-permission matrix management
  - Create UserPermissionsController for user-specific overrides
  - Implement PermissionAuditController for audit trail access
  - Add proper HTTP status codes and error handling
  - _Requirements: 5.1, 5.2, 5.4, 5.5_

- [x] 13. Add Permission-Based Authorization Attributes





  - Create RequirePermissionAttribute for controller/action authorization
  - Implement RequireResourceActionAttribute for resource-action permissions
  - Add RequireAnyPermissionAttribute for multiple permission checks
  - Create authorization policy providers for permission-based policies
  - Write integration tests for authorization attributes
  - _Requirements: 4.2, 4.3, 5.5_

- [x] 14. Implement Permission Seeding and Default Configuration



  - Create permission seeding service for default permissions
  - Add default role configurations with standard permission sets
  - Implement environment-specific permission configurations
  - Create migration data seeding for initial system setup
  - Add permission cleanup service for orphaned permissions
  - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_

- [ ] 15. Create Custom Exception Handling
  - Implement PermissionNotFoundException with proper error codes
  - Add DuplicatePermissionException for resource-action conflicts
  - Create CircularPermissionHierarchyException for hierarchy validation
  - Implement InsufficientPermissionException for authorization failures
  - Extend global exception handler for permission-specific exceptions
  - _Requirements: 4.4, 1.4, 2.5_

- [ ] 16. Write Comprehensive Unit Tests
  - Create unit tests for all domain entities and services
  - Add unit tests for command and query handlers
  - Implement unit tests for permission evaluation logic
  - Create unit tests for caching service behavior
  - Add unit tests for authorization service functionality
  - _Requirements: All requirements - testing coverage_

- [ ] 17. Create Integration Tests
  - Implement API integration tests for permission management endpoints
  - Add database integration tests for complex permission queries
  - Create caching integration tests with Redis
  - Implement end-to-end permission evaluation tests
  - Add performance tests for sub-100ms permission evaluation
  - _Requirements: 4.1, 8.4, 5.1, 8.3_

- [ ] 18. Add Performance Monitoring and Optimization
  - Implement permission evaluation latency tracking
  - Add cache hit ratio monitoring and alerting
  - Create database query performance profiling
  - Implement API endpoint response time monitoring
  - Add performance benchmarks and optimization recommendations
  - _Requirements: 8.4, 8.2, 8.1, 8.3_

- [ ] 19. Update Terraform Infrastructure for Permission System
  - Add Redis cluster configuration for distributed permission caching
  - Update RDS configuration with additional indexes for permission queries
  - Configure CloudWatch metrics for permission evaluation performance
  - Add IAM roles and policies for permission audit log access
  - Update security groups for Redis cluster access
  - _Requirements: 8.2, 8.4, 6.1, 8.1_

- [ ] 20. Create Terraform Modules for Permission Infrastructure
  - Create Redis cluster module with high availability configuration
  - Add permission audit log storage module (S3 or CloudWatch Logs)
  - Implement database performance monitoring module
  - Create IAM module for permission service roles
  - Add application load balancer health checks for permission endpoints
  - _Requirements: 8.2, 6.1, 8.4, 5.1_

- [ ] 21. Update Environment-Specific Terraform Configurations
  - Configure development environment with single Redis instance
  - Set up staging environment with Redis cluster for testing
  - Configure production environment with multi-AZ Redis cluster
  - Add environment-specific permission seeding configurations
  - Update backup and disaster recovery for permission data
  - _Requirements: 7.3, 8.2, 6.4, 8.1_