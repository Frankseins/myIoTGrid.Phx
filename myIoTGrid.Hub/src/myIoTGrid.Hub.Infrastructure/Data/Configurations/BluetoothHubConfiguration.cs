using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace myIoTGrid.Hub.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core Configuration for BluetoothHub Entity (Bluetooth Gateway).
/// Represents a Raspberry Pi or similar device that receives sensor data via BLE
/// from ESP32 devices and forwards it to the Hub API.
/// </summary>
public class BluetoothHubConfiguration : IEntityTypeConfiguration<BluetoothHub>
{
    public void Configure(EntityTypeBuilder<BluetoothHub> builder)
    {
        builder.ToTable("BluetoothHubs");

        // Primary Key
        builder.HasKey(b => b.Id);

        // Properties
        builder.Property(b => b.HubId)
            .IsRequired();

        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(b => b.MacAddress)
            .HasMaxLength(17); // Format: AA:BB:CC:DD:EE:FF

        builder.Property(b => b.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("Inactive");

        builder.Property(b => b.CreatedAt)
            .IsRequired();

        builder.Property(b => b.UpdatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(b => b.Hub)
            .WithMany(h => h.BluetoothHubs)
            .HasForeignKey(b => b.HubId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(b => b.Nodes)
            .WithOne(n => n.BluetoothHub)
            .HasForeignKey(n => n.BluetoothHubId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(b => b.HubId);
        builder.HasIndex(b => b.MacAddress).IsUnique();
        builder.HasIndex(b => b.Status);
        builder.HasIndex(b => b.LastSeen);
    }
}
