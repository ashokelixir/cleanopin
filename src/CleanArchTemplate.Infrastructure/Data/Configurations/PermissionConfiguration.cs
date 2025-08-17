using CleanArchTemplate.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArchTemplate.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for Permission entity
/// </summary>
public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .ValueGeneratedNever();

        builder.Property(p => p.Resource)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.Action)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.Name)
            .HasMaxLength(201) // Resource.Action format: 100 + 1 + 100
            .IsRequired()
            .HasComputedColumnSql("\"Resource\" || '.' || \"Action\"", stored: true);

        builder.Property(p => p.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(p => p.Category)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(p => p.ParentPermissionId)
            .IsRequired(false);

        // Audit fields
        builder.Property(p => p.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(p => p.CreatedBy)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .IsRequired(false);

        builder.Property(p => p.UpdatedBy)
            .HasMaxLength(100)
            .IsRequired(false);

        // Configure relationships
        builder.HasMany(p => p.RolePermissions)
            .WithOne(rp => rp.Permission)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.UserPermissions)
            .WithOne(up => up.Permission)
            .HasForeignKey(up => up.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.ChildPermissions)
            .WithOne(p => p.ParentPermission)
            .HasForeignKey(p => p.ParentPermissionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraint for resource-action combination
        builder.HasIndex(p => new { p.Resource, p.Action })
            .IsUnique()
            .HasDatabaseName("IX_Permissions_Resource_Action");

        // Performance indexes for resource-action queries
        builder.HasIndex(p => p.Resource)
            .HasDatabaseName("IX_Permissions_Resource");

        builder.HasIndex(p => p.Action)
            .HasDatabaseName("IX_Permissions_Action");

        builder.HasIndex(p => p.Name)
            .IsUnique()
            .HasDatabaseName("IX_Permissions_Name");

        builder.HasIndex(p => p.Category)
            .HasDatabaseName("IX_Permissions_Category");

        builder.HasIndex(p => p.IsActive)
            .HasDatabaseName("IX_Permissions_IsActive");

        builder.HasIndex(p => p.ParentPermissionId)
            .HasDatabaseName("IX_Permissions_ParentPermissionId");

        builder.HasIndex(p => p.CreatedAt)
            .HasDatabaseName("IX_Permissions_CreatedAt");

        // Composite indexes for optimal query performance
        builder.HasIndex(p => new { p.Resource, p.IsActive })
            .HasDatabaseName("IX_Permissions_Resource_IsActive");

        builder.HasIndex(p => new { p.Action, p.IsActive })
            .HasDatabaseName("IX_Permissions_Action_IsActive");

        builder.HasIndex(p => new { p.Category, p.IsActive })
            .HasDatabaseName("IX_Permissions_Category_IsActive");

        // Ignore domain events (they are not persisted)
        builder.Ignore(p => p.DomainEvents);
    }
}