
namespace myIoTGrid.Hub.Service.Extensions;

/// <summary>
/// Mapping Extensions for Reading Entity (Measurement).
/// Matter-konform: Entspricht einem Attribute Report.
/// </summary>
public static class ReadingMappingExtensions
{
    /// <summary>
    /// Converts a Reading entity to a ReadingDto
    /// </summary>
    /// <param name="reading">The reading entity</param>
    /// <param name="capabilityLookup">Optional: SensorCapability to use for DisplayName when no Assignment exists</param>
    public static ReadingDto ToDto(this Reading reading, SensorCapability? capabilityLookup = null)
    {
        // Get Sensor from Assignment (Reading -> Assignment -> Sensor)
        var sensor = reading.Assignment?.Sensor;

        // Find DisplayName from Capability if available
        // Priority: 1. Assignment's Sensor Capability, 2. Provided capabilityLookup, 3. MeasurementType
        var capability = sensor?.Capabilities
            .FirstOrDefault(c => c.MeasurementType.Equals(reading.MeasurementType, StringComparison.OrdinalIgnoreCase));

        var displayName = capability?.DisplayName
            ?? capabilityLookup?.DisplayName
            ?? reading.MeasurementType;

        // Get icon and color from capability's sensor if no assignment sensor
        var sensorIcon = sensor?.Icon ?? capabilityLookup?.Sensor?.Icon;
        var sensorColor = sensor?.Color ?? capabilityLookup?.Sensor?.Color;
        var sensorCode = sensor?.Code ?? capabilityLookup?.Sensor?.Code ?? string.Empty;
        var sensorName = sensor?.Name ?? capabilityLookup?.Sensor?.Name ?? string.Empty;
        var sensorId = sensor?.Id ?? capabilityLookup?.Sensor?.Id;

        return new ReadingDto(
            Id: reading.Id,
            TenantId: reading.TenantId,
            NodeId: reading.NodeId,
            NodeName: reading.Node?.Name ?? string.Empty,
            AssignmentId: reading.AssignmentId,
            SensorId: sensorId,
            SensorCode: sensorCode,
            SensorName: sensorName,
            SensorIcon: sensorIcon,
            SensorColor: sensorColor,
            MeasurementType: reading.MeasurementType,
            DisplayName: displayName,
            RawValue: reading.RawValue,
            Value: reading.Value,
            Unit: reading.Unit,
            Timestamp: reading.Timestamp,
            Location: reading.Node?.Location?.ToDto(),
            IsSyncedToCloud: reading.IsSyncedToCloud
        );
    }

    /// <summary>
    /// Converts a CreateReadingDto to a Reading entity with calibration applied
    /// </summary>
    public static Reading ToEntity(
        this CreateReadingDto dto,
        Guid tenantId,
        Guid nodeId,
        Guid assignmentId,
        string unit,
        double calibratedValue)
    {
        return new Reading
        {
            TenantId = tenantId,
            NodeId = nodeId,
            AssignmentId = assignmentId,
            MeasurementType = dto.MeasurementType.ToLowerInvariant(),
            RawValue = dto.RawValue,
            Value = calibratedValue,
            Unit = unit,
            Timestamp = dto.Timestamp ?? DateTime.UtcNow,
            IsSyncedToCloud = false
        };
    }

    /// <summary>
    /// Converts a list of Reading Entities to DTOs
    /// </summary>
    public static IEnumerable<ReadingDto> ToDtos(this IEnumerable<Reading> readings)
    {
        return readings.Select(r => r.ToDto());
    }

    /// <summary>
    /// Converts a list of Reading Entities to DTOs with capability lookup for display names
    /// </summary>
    /// <param name="readings">The reading entities</param>
    /// <param name="capabilityLookup">Dictionary mapping MeasurementType to SensorCapability</param>
    public static IEnumerable<ReadingDto> ToDtos(
        this IEnumerable<Reading> readings,
        IDictionary<string, SensorCapability>? capabilityLookup)
    {
        if (capabilityLookup == null)
        {
            return readings.Select(r => r.ToDto());
        }

        return readings.Select(r =>
        {
            // Only use capability lookup if reading has no assignment
            if (r.AssignmentId == null &&
                capabilityLookup.TryGetValue(r.MeasurementType.ToLowerInvariant(), out var capability))
            {
                return r.ToDto(capability);
            }
            return r.ToDto();
        });
    }
}
