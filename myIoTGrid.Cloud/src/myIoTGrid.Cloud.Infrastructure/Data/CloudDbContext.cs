using Microsoft.EntityFrameworkCore;
using myIoTGrid.Shared.Common.Entities;
using myIoTGrid.Cloud.Infrastructure.Data.Configurations;

namespace myIoTGrid.Cloud.Infrastructure.Data;

/// <summary>
/// Entity Framework Core DbContext for Cloud API (PostgreSQL)
/// </summary>
public class CloudDbContext : DbContext
{
    public CloudDbContext(DbContextOptions<CloudDbContext> options) : base(options)
    {
    }

    // =============================================================================
    // DbSets - Core Entities
    // =============================================================================
    
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Hub> Hubs => Set<Hub>();
    public DbSet<Node> Nodes => Set<Node>();
    public DbSet<Sensor> Sensors => Set<Sensor>();
    public DbSet<SensorCapability> SensorCapabilities => Set<SensorCapability>();
    public DbSet<NodeSensorAssignment> NodeSensorAssignments => Set<NodeSensorAssignment>();
    public DbSet<Reading> Readings => Set<Reading>();
    
    // =============================================================================
    // DbSets - Synced Entities (from Hubs)
    // =============================================================================
    
    public DbSet<SyncedNode> SyncedNodes => Set<SyncedNode>();
    public DbSet<SyncedReading> SyncedReadings => Set<SyncedReading>();
    
    // =============================================================================
    // DbSets - Alerts
    // =============================================================================
    
    public DbSet<AlertType> AlertTypes => Set<AlertType>();
    public DbSet<Alert> Alerts => Set<Alert>();
    
    // =============================================================================
    // DbSets - Debug/Monitoring
    // =============================================================================
    
    public DbSet<NodeDebugLog> NodeDebugLogs => Set<NodeDebugLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from this assembly
        modelBuilder.ApplyConfiguration(new TenantConfiguration());
        modelBuilder.ApplyConfiguration(new HubConfiguration());
        modelBuilder.ApplyConfiguration(new NodeConfiguration());
        modelBuilder.ApplyConfiguration(new SensorConfiguration());
        modelBuilder.ApplyConfiguration(new SensorCapabilityConfiguration());
        modelBuilder.ApplyConfiguration(new NodeSensorAssignmentConfiguration());
        modelBuilder.ApplyConfiguration(new ReadingConfiguration());
        modelBuilder.ApplyConfiguration(new SyncedNodeConfiguration());
        modelBuilder.ApplyConfiguration(new SyncedReadingConfiguration());
        modelBuilder.ApplyConfiguration(new AlertTypeConfiguration());
        modelBuilder.ApplyConfiguration(new AlertConfiguration());
        modelBuilder.ApplyConfiguration(new NodeDebugLogConfiguration());
    }
}
