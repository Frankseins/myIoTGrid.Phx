using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using myIoTGrid.Shared.Common.Entities;

namespace myIoTGrid.Cloud.Infrastructure.Data.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.CloudApiKey)
            .HasMaxLength(500);

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(t => t.Name)
            .IsUnique();

        builder.HasIndex(t => t.CloudApiKey)
            .IsUnique()
            .HasFilter("\"CloudApiKey\" IS NOT NULL");
    }
}
