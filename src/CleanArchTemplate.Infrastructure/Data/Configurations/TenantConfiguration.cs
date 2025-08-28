using CleanArchTemplate.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArchTemplate.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for the Tenant entity
/// </summary>
public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Identifier)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(t => t.ConnectionString)
            .HasMaxLength(1000);

        builder.Property(t => t.Configuration)
            .IsRequired()
            .HasDefaultValue("{}");

        builder.Property(t => t.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(t => t.SubscriptionExpiresAt);

        // Indexes
        builder.HasIndex(t => t.Identifier)
            .IsUnique()
            .HasDatabaseName("IX_Tenants_Identifier");

        builder.HasIndex(t => t.IsActive)
            .HasDatabaseName("IX_Tenants_IsActive");

        builder.HasIndex(t => new { t.IsActive, t.SubscriptionExpiresAt })
            .HasDatabaseName("IX_Tenants_IsActive_SubscriptionExpiresAt");

        // Relationships
        builder.HasMany(t => t.Users)
            .WithOne()
            .HasForeignKey("TenantId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Roles)
            .WithOne()
            .HasForeignKey("TenantId")
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore navigation properties that are handled by the domain entity
        builder.Ignore(t => t.DomainEvents);
    }
}