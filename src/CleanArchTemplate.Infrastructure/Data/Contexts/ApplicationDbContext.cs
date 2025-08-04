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

        // Get entities with domain events before saving
        var entitiesWithEvents = GetEntitiesWithDomainEvents();

        var result = await base.SaveChangesAsync(cancellationToken);

        // Dispatch domain events after successful save
        await DispatchDomainEventsAsync(entitiesWithEvents, cancellationToken);

        return result;
    }

    public override int SaveChanges()
    {
        // Update audit fields before saving
        UpdateAuditFields();

        // Get entities with domain events before saving
        var entitiesWithEvents = GetEntitiesWithDomainEvents();

        var result = base.SaveChanges();

        // Dispatch domain events after successful save (synchronously)
        DispatchDomainEventsAsync(entitiesWithEvents).GetAwaiter().GetResult();

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

    private void ClearDomainEvents()
    {
        var entities = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity);

        foreach (var entity in entities)
        {
            entity.ClearDomainEvents();
        }
    }

    private List<BaseEntity> GetEntitiesWithDomainEvents()
    {
        return ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();
    }

    private async Task DispatchDomainEventsAsync(IEnumerable<BaseEntity> entities, CancellationToken cancellationToken = default)
    {
        // For now, we'll clear the events without dispatching
        // In a production scenario, you might want to use a different approach
        // such as collecting events and dispatching them via a separate service
        // or using an outbox pattern
        
        foreach (var entity in entities)
        {
            entity.ClearDomainEvents();
        }
        
        // TODO: Implement proper domain event dispatching
        // This could be done via:
        // 1. An outbox pattern where events are stored and processed separately
        // 2. A domain event collector service that's called after SaveChanges
        // 3. Using EF Core interceptors for event dispatching
        
        await Task.CompletedTask;
    }

    private string GetCurrentUser()
    {
        // TODO: Implement user context service to get current user
        // For now, return system as default
        return "System";
    }
}