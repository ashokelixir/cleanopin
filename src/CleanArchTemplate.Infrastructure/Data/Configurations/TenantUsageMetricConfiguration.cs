using CleanArchTemplate.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArchTemplate.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for the TenantUsageMetric entity
/// </summary>
public class TenantUsageMetricConfiguration : IEntityTypeConfiguration<TenantUsageMetric>
{
    public void Configure(EntityTypeBuilder<TenantUsageMetric> builder)
    {
        builder.ToTable("TenantUsageMetrics");

        builder.HasKey(tum => tum.Id);

        builder.Property(tum => tum.TenantId)
            .IsRequired();

        builder.Property(tum => tum.MetricName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(tum => tum.Value)
            .IsRequired();

        builder.Property(tum => tum.Tags)
            .IsRequired()
            .HasDefaultValue("{}");

        builder.Property(tum => tum.RecordedAt)
            .IsRequired();

        builder.Property(tum => tum.RecordedBy);

        builder.Property(tum => tum.Metadata)
            .IsRequired()
            .HasDefaultValue("{}");

        // Indexes
        builder.HasIndex(tum => tum.TenantId)
            .HasDatabaseName("IX_TenantUsageMetrics_TenantId");

        builder.HasIndex(tum => new { tum.TenantId, tum.MetricName })
            .HasDatabaseName("IX_TenantUsageMetrics_TenantId_MetricName");

        builder.HasIndex(tum => new { tum.TenantId, tum.RecordedAt })
            .HasDatabaseName("IX_TenantUsageMetrics_TenantId_RecordedAt");

        builder.HasIndex(tum => new { tum.TenantId, tum.MetricName, tum.RecordedAt })
            .HasDatabaseName("IX_TenantUsageMetrics_TenantId_MetricName_RecordedAt");

        builder.HasIndex(tum => tum.RecordedAt)
            .HasDatabaseName("IX_TenantUsageMetrics_RecordedAt");

        // Relationships
        builder.HasOne(tum => tum.Tenant)
            .WithMany()
            .HasForeignKey(tum => tum.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}