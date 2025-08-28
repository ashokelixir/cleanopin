using System.Reflection;
using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Domain.Common;
using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System.Linq.Expressions;

namespace CleanArchTemplate.Infrastructure.Data.Contexts;

/// <summary>
/// Application database context for Entity Framework Core
/// </summary>
public class ApplicationDbContext : DbContext
{
    private readonly IServiceProvider? _serviceProvider;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
        // Get service provider from the options if available
        _serviceProvider = options.FindExtension<CoreOptionsExtension>()?.ApplicationServiceProvider;
    }

    /// <summary>
    /// Users DbSet
    /// </summary>
    public DbSet<User> Users { get; set; } = null!;

    /// <summary>
    /// Roles DbSet
    /// </summary>
    public DbSet<Role> Roles { get; set; } = null!;

    /// <summary>
    /// Permissions DbSet
    /// </summary>
    public DbSet<Permission> Permissions { get; set; } = null!;

    /// <summary>
    /// UserRoles DbSet
    /// </summary>
    public DbSet<UserRole> UserRoles { get; set; } = null!;

    /// <summary>
    /// RolePermissions DbSet
    /// </summary>
    public DbSet<RolePermission> RolePermissions { get; set; } = null!;

    /// <summary>
    /// RefreshTokens DbSet
    /// </summary>
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    /// <summary>
    /// UserPermissions DbSet
    /// </summary>
    public DbSet<UserPermission> UserPermissions { get; set; } = null!;

    /// <summary>
    /// PermissionAuditLogs DbSet
    /// </summary>
    public DbSet<PermissionAuditLog> PermissionAuditLogs { get; set; } = null!;

    /// <summary>
    /// Tenants DbSet
    /// </summary>
    public DbSet<Tenant> Tenants { get; set; } = null!;

    /// <summary>
    /// TenantConfigurations DbSet
    /// </summary>
    public DbSet<TenantConfiguration> TenantConfigurations { get; set; } = null!;

    /// <summary>
    /// TenantUsageMetrics DbSet
    /// </summary>
    public DbSet<TenantUsageMetric> TenantUsageMetrics { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from the current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Apply global query filters for multi-tenancy
        ApplyTenantQueryFilters(modelBuilder);
    }

    /// <summary>
    /// Applies global query filters for tenant isolation
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    private void ApplyTenantQueryFilters(ModelBuilder modelBuilder)
    {
        // Skip tenant filters if no service provider is available (e.g., during migrations)
        var tenantContext = GetTenantContext();
        if (tenantContext == null)
            return;

        // Apply tenant filter to all ITenantEntity implementations
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var tenantProperty = Expression.Property(parameter, nameof(ITenantEntity.TenantId));
                
                // Create expression: e => tenantContext.CurrentTenant == null || e.TenantId == tenantContext.CurrentTenant.Id
                var tenantContextProperty = Expression.Property(Expression.Constant(tenantContext), nameof(ITenantContext.CurrentTenant));
                var tenantIdProperty = Expression.Property(tenantContextProperty, nameof(Application.Common.Models.TenantInfo.Id));
                
                var nullCheck = Expression.Equal(tenantContextProperty, Expression.Constant(null));
                var tenantMatch = Expression.Equal(tenantProperty, tenantIdProperty);
                var filter = Expression.OrElse(nullCheck, tenantMatch);
                
                var lambda = Expression.Lambda(filter, parameter);
                
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Update audit fields before saving
        UpdateAuditFields();

        var result = await base.SaveChangesAsync(cancellationToken);

        return result;
    }

    public override int SaveChanges()
    {
        // Update audit fields before saving
        UpdateAuditFields();

        var result = base.SaveChanges();

        return result;
    }

    private void UpdateAuditFields()
    {
        var entries = ChangeTracker.Entries<BaseAuditableEntity>();

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.CreatedBy = GetCurrentUser();
                    
                    // Set tenant ID for tenant entities
                    var tenantContext = GetTenantContext();
                    if (entry.Entity is ITenantEntity tenantEntity && tenantContext?.CurrentTenant != null)
                    {
                        tenantEntity.TenantId = tenantContext.CurrentTenant.Id;
                    }
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedBy = GetCurrentUser();
                    break;
            }
        }
    }



    private ITenantContext? GetTenantContext()
    {
        try
        {
            return _serviceProvider?.GetService<ITenantContext>();
        }
        catch
        {
            // Return null if service provider is not available or service cannot be resolved
            return null;
        }
    }

    private string GetCurrentUser()
    {
        // TODO: Implement user context service to get current user
        // For now, return system as default
        return "System";
    }
}