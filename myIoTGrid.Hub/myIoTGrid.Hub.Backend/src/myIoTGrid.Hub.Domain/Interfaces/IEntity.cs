namespace myIoTGrid.Hub.Domain.Interfaces;

/// <summary>
/// Basis-Interface für alle Entities
/// </summary>
public interface IEntity
{
    /// <summary>Primärschlüssel</summary>
    Guid Id { get; set; }
}

/// <summary>
/// Interface für Entities mit Tenant-Zugehörigkeit
/// </summary>
public interface ITenantEntity : IEntity
{
    /// <summary>Tenant-ID für Multi-Tenant Support</summary>
    Guid TenantId { get; set; }
}

/// <summary>
/// Interface für Entities die mit Cloud synchronisiert werden
/// </summary>
public interface ISyncableEntity : IEntity
{
    /// <summary>Ob dieses Entity global (von Cloud definiert) ist</summary>
    bool IsGlobal { get; set; }

    /// <summary>Erstellungszeitpunkt</summary>
    DateTime CreatedAt { get; set; }
}
