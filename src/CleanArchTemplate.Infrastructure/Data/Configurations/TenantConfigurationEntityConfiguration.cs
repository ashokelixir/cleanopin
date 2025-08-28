using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantConfigurationEntity = CleanArchTemplate.Domain.Entities.TenantConfiguration;

namespace CleanArchTemplate.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for the TenantConfiguration entity
/// </summary>
public class TenantConfigurationEntityConfiguration : IEntityTypeConfiguration<TenantConfigurationEntity>
{
    public void Configure(EntityTypeBuilder<TenantConfigurationEntity> builder)
    {
        builder.ToTable("TenantConfigurations");

        builder.HasKey(tc => tc.Id);

        builder.Property(tc => tc.TenantId)
            .IsRequired();

        builder.Property(tc => tc.Key)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(tc => tc.Value)
            .IsRequired();

        builder.Property(tc => tc.DataType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(tc => tc.Description)
            .HasMaxLength(500);

        builder.Property(tc => tc.IsSystemConfiguration)
            .IsRequired()
            .HasDefaultValue(false);

        // Indexes
        builder.HasIndex(tc => new { tc.TenantId, tc.Key })
            .IsUnique()
            .HasDatabaseName("IX_TenantConfigurations_TenantId_Key");

        builder.HasIndex(tc => tc.TenantId)
            .HasDatabaseName("IX_TenantConfigurations_TenantId");

        builder.HasIndex(tc => tc.IsSystemConfiguration)
            .HasDatabaseName("IX_TenantConfigurations_IsSystemConfiguration");

        // Relationships
        builder.HasOne(tc => tc.Tenant)
            .WithMany()
            .HasForeignKey(tc => tc.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}