using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace myIoTGrid.Hub.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core Configuration for Hub Entity (Raspberry Pi Gateway)
/// </summary>
public class HubConfiguration : IEntityTypeConfiguration<Domain.Entities.Hub>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Hub> builder)
    {
        builder.ToTable("Hubs");

        // Primary Key
        builder.HasKey(h => h.Id);

        // Properties
        builder.Property(h => h.TenantId)
            .IsRequired();

        builder.Property(h => h.HubId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(h => h.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(h => h.Description)
            .HasMaxLength(500);

        builder.Property(h => h.IsOnline)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(h => h.CreatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(h => h.Tenant)
            .WithMany(t => t.Hubs)
            .HasForeignKey(h => h.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(h => h.Nodes)
            .WithOne(n => n.Hub)
            .HasForeignKey(n => n.HubId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(h => h.Alerts)
            .WithOne(a => a.Hub)
            .HasForeignKey(a => a.HubId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(h => h.TenantId);
        builder.HasIndex(h => h.HubId);
        builder.HasIndex(h => new { h.TenantId, h.HubId }).IsUnique();
        builder.HasIndex(h => h.IsOnline);
        builder.HasIndex(h => h.LastSeen);
    }
}
