using Microsoft.EntityFrameworkCore;

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
    public DbSet<myIoTGrid.Shared.Common.Entities.Hub> Hubs => Set<myIoTGrid.Shared.Common.Entities.Hub>();

    /// <summary>Nodes (ESP32/LoRa32 Devices) - Matter Nodes</summary>
    public DbSet<Node> Nodes => Set<Node>();

    // === Zweistufiges Sensor-Modell (v3.0) ===

    /// <summary>Sensors (Complete hardware definition with calibration)</summary>
    public DbSet<Sensor> Sensors => Set<Sensor>();

    /// <summary>Sensor Capabilities (Measurement types per Sensor)</summary>
    public DbSet<SensorCapability> SensorCapabilities => Set<SensorCapability>();

    /// <summary>Node Sensor Assignments (Hardware Binding) - Matter Endpoints</summary>
    public DbSet<NodeSensorAssignment> NodeSensorAssignments => Set<NodeSensorAssignment>();

    /// <summary>Readings (Measurement Data with raw + calibrated values)</summary>
    public DbSet<Reading> Readings => Set<Reading>();

    // === Synced Data from Cloud ===

    /// <summary>Synced Nodes (from Cloud)</summary>
    public DbSet<SyncedNode> SyncedNodes => Set<SyncedNode>();

    /// <summary>Synced Readings (from Cloud)</summary>
    public DbSet<SyncedReading> SyncedReadings => Set<SyncedReading>();

    // === Alerts ===

    /// <summary>Alert Types</summary>
    public DbSet<AlertType> AlertTypes => Set<AlertType>();

    /// <summary>Alerts/Warnings</summary>
    public DbSet<Alert> Alerts => Set<Alert>();

    // === Remote Debug System (Sprint 8) ===

    /// <summary>Node Debug Logs</summary>
    public DbSet<NodeDebugLog> NodeDebugLogs => Set<NodeDebugLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Load all Configurations from this Assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HubDbContext).Assembly);
    }
}
