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

    /// <summary>Sensors (ESP32/LoRa32 Devices)</summary>
    public DbSet<Sensor> Sensors => Set<Sensor>();

    /// <summary>Sensor Types</summary>
    public DbSet<SensorType> SensorTypes => Set<SensorType>();

    /// <summary>Measurement Data</summary>
    public DbSet<SensorData> SensorData => Set<SensorData>();

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
