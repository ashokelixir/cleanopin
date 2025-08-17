using CleanArchTemplate.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArchTemplate.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for UserPermission entity
/// </summary>
public class UserPermissionConfiguration : IEntityTypeConfiguration<UserPermission>
{
    public void Configure(EntityTypeBuilder<UserPermission> builder)
    {
        builder.ToTable("UserPermissions");

        builder.HasKey(up => up.Id);

        builder.Property(up => up.Id)
            .ValueGeneratedNever();

        builder.Property(up => up.UserId)
            .IsRequired();

        builder.Property(up => up.PermissionId)
            .IsRequired();

        builder.Property(up => up.State)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(up => up.Reason)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(up => up.ExpiresAt)
            .IsRequired(false);

        // Audit fields
        builder.Property(up => up.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(up => up.CreatedBy)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(up => up.UpdatedAt)
            .IsRequired(false);

        builder.Property(up => up.UpdatedBy)
            .HasMaxLength(100)
            .IsRequired(false);

        // Configure relationships
        builder.HasOne(up => up.User)
            .WithMany(u => u.UserPermissions)
            .HasForeignKey(up => up.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(up => up.Permission)
            .WithMany(p => p.UserPermissions)
            .HasForeignKey(up => up.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint to prevent duplicate user-permission combinations
        builder.HasIndex(up => new { up.UserId, up.PermissionId })
            .IsUnique()
            .HasDatabaseName("IX_UserPermissions_UserId_PermissionId");

        // Performance indexes
        builder.HasIndex(up => up.UserId)
            .HasDatabaseName("IX_UserPermissions_UserId");

        builder.HasIndex(up => up.PermissionId)
            .HasDatabaseName("IX_UserPermissions_PermissionId");

        builder.HasIndex(up => up.State)
            .HasDatabaseName("IX_UserPermissions_State");

        builder.HasIndex(up => up.ExpiresAt)
            .HasDatabaseName("IX_UserPermissions_ExpiresAt");

        builder.HasIndex(up => up.CreatedAt)
            .HasDatabaseName("IX_UserPermissions_CreatedAt");

        // Composite index for efficient permission evaluation queries
        builder.HasIndex(up => new { up.UserId, up.State, up.ExpiresAt })
            .HasDatabaseName("IX_UserPermissions_UserId_State_ExpiresAt");

        // Ignore domain events (they are not persisted)
        builder.Ignore(up => up.DomainEvents);
    }
}