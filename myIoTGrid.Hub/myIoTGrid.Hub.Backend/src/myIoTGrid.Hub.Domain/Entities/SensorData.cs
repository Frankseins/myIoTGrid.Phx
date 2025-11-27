using myIoTGrid.Hub.Domain.Interfaces;

namespace myIoTGrid.Hub.Domain.Entities;

/// <summary>
/// Individual measurement from a sensor
/// </summary>
public class SensorData : ITenantEntity
{
    /// <summary>Primary key</summary>
    public Guid Id { get; set; }

    /// <summary>Tenant-ID for Multi-Tenant Support</summary>
    public Guid TenantId { get; set; }

    /// <summary>Reference to the Sensor (NOT Hub!)</summary>
    public Guid SensorId { get; set; }

    /// <summary>Reference to the Sensor Type</summary>
    public Guid SensorTypeId { get; set; }

    /// <summary>Measurement value</summary>
    public double Value { get; set; }

    /// <summary>Timestamp of measurement</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>Whether this measurement was synced to cloud</summary>
    public bool IsSyncedToCloud { get; set; }

    // Navigation Properties
    public Sensor? Sensor { get; set; }
    public SensorType? SensorType { get; set; }
}
