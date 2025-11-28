using Microsoft.EntityFrameworkCore;
using myIoTGrid.Hub.Domain.Entities;

namespace myIoTGrid.Hub.Infrastructure.Data;

/// <summary>
/// Entity Framework Core DbContext for the Hub database
/// </summary>
public class HubDbContext : DbContext
{
    public HubDbContext(DbContextOptions<HubDbContext> options) : base(options)
    {
    }

    /// <summary>Tenants</summary>
    public DbSet<Tenant> Tenants => Set<Tenant>();

    /// <summary>Hubs (Raspberry Pi Gateways)</summary>
    public DbSet<Domain.Entities.Hub> Hubs => Set<Domain.Entities.Hub>();

    /// <summary>Nodes (ESP32/LoRa32 Devices) - Matter Nodes</summary>
    public DbSet<Node> Nodes => Set<Node>();

    /// <summary>Sensors (Physical sensor chips: DHT22, BME280) - Matter Endpoints</summary>
    public DbSet<Sensor> Sensors => Set<Sensor>();

    /// <summary>Sensor Types - Matter Clusters</summary>
    public DbSet<SensorType> SensorTypes => Set<SensorType>();

    /// <summary>Readings (Measurement Data) - Matter Attribute Reports</summary>
    public DbSet<Reading> Readings => Set<Reading>();

    /// <summary>Synced Nodes (from Cloud)</summary>
    public DbSet<SyncedNode> SyncedNodes => Set<SyncedNode>();

    /// <summary>Synced Readings (from Cloud)</summary>
    public DbSet<SyncedReading> SyncedReadings => Set<SyncedReading>();

    /// <summary>Alert Types</summary>
    public DbSet<AlertType> AlertTypes => Set<AlertType>();

    /// <summary>Alerts/Warnings</summary>
    public DbSet<Alert> Alerts => Set<Alert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Load all Configurations from this Assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HubDbContext).Assembly);
    }
}
