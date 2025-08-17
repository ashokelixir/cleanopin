using System.Reflection;
using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Domain.Common;
using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CleanArchTemplate.Infrastructure.Data.Contexts;

/// <summary>
/// Application database context for Entity Framework Core
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from the current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
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
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedBy = GetCurrentUser();
                    break;
            }
        }
    }



    private string GetCurrentUser()
    {
        // TODO: Implement user context service to get current user
        // For now, return system as default
        return "System";
    }
}