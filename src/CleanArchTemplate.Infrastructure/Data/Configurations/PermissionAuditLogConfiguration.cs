using CleanArchTemplate.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArchTemplate.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for PermissionAuditLog entity
/// </summary>
public class PermissionAuditLogConfiguration : IEntityTypeConfiguration<PermissionAuditLog>
{
    public void Configure(EntityTypeBuilder<PermissionAuditLog> builder)
    {
        builder.ToTable("PermissionAuditLogs");

        builder.HasKey(pal => pal.Id);

        builder.Property(pal => pal.Id)
            .ValueGeneratedNever();

        builder.Property(pal => pal.UserId)
            .IsRequired(false);

        builder.Property(pal => pal.RoleId)
            .IsRequired(false);

        builder.Property(pal => pal.PermissionId)
            .IsRequired();

        builder.Property(pal => pal.Action)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(pal => pal.OldValue)
            .HasMaxLength(4000)
            .IsRequired(false);

        builder.Property(pal => pal.NewValue)
            .HasMaxLength(4000)
            .IsRequired(false);

        builder.Property(pal => pal.Reason)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(pal => pal.PerformedBy)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(pal => pal.PerformedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");



        // Configure relationships
        builder.HasOne(pal => pal.User)
            .WithMany()
            .HasForeignKey(pal => pal.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(pal => pal.Role)
            .WithMany()
            .HasForeignKey(pal => pal.RoleId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(pal => pal.Permission)
            .WithMany()
            .HasForeignKey(pal => pal.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Performance indexes for audit queries
        builder.HasIndex(pal => pal.UserId)
            .HasDatabaseName("IX_PermissionAuditLogs_UserId");

        builder.HasIndex(pal => pal.RoleId)
            .HasDatabaseName("IX_PermissionAuditLogs_RoleId");

        builder.HasIndex(pal => pal.PermissionId)
            .HasDatabaseName("IX_PermissionAuditLogs_PermissionId");

        builder.HasIndex(pal => pal.Action)
            .HasDatabaseName("IX_PermissionAuditLogs_Action");

        builder.HasIndex(pal => pal.PerformedAt)
            .HasDatabaseName("IX_PermissionAuditLogs_PerformedAt");

        builder.HasIndex(pal => pal.PerformedBy)
            .HasDatabaseName("IX_PermissionAuditLogs_PerformedBy");

        // Composite indexes for common audit queries
        builder.HasIndex(pal => new { pal.UserId, pal.PerformedAt })
            .HasDatabaseName("IX_PermissionAuditLogs_UserId_PerformedAt");

        builder.HasIndex(pal => new { pal.PermissionId, pal.PerformedAt })
            .HasDatabaseName("IX_PermissionAuditLogs_PermissionId_PerformedAt");

        builder.HasIndex(pal => new { pal.Action, pal.PerformedAt })
            .HasDatabaseName("IX_PermissionAuditLogs_Action_PerformedAt");

        // Ignore domain events (they are not persisted)
        builder.Ignore(pal => pal.DomainEvents);
    }
}