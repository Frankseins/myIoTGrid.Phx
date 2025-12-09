
namespace myIoTGrid.Hub.Service.Extensions;

/// <summary>
/// Mapping Extensions for NodeSensorAssignment Entity (v3.0).
/// Hardware binding of Sensors to Nodes.
/// Two-tier model: Sensor → NodeSensorAssignment
/// </summary>
public static class NodeSensorAssignmentMappingExtensions
{
    /// <summary>
    /// Converts a NodeSensorAssignment entity to a NodeSensorAssignmentDto
    /// </summary>
    public static NodeSensorAssignmentDto ToDto(
        this NodeSensorAssignment assignment,
        IEffectiveConfigService effectiveConfigService)
    {
        var effectiveConfig = effectiveConfigService.GetEffectiveConfig(
            assignment,
            assignment.Sensor);

        return new NodeSensorAssignmentDto(
            Id: assignment.Id,
            NodeId: assignment.NodeId,
            NodeName: assignment.Node?.Name ?? string.Empty,
            SensorId: assignment.SensorId,
            SensorCode: assignment.Sensor?.Code ?? string.Empty,
            SensorName: assignment.Sensor?.Name ?? string.Empty,
            EndpointId: assignment.EndpointId,
            Alias: assignment.Alias,
            I2CAddressOverride: assignment.I2CAddressOverride,
            SdaPinOverride: assignment.SdaPinOverride,
            SclPinOverride: assignment.SclPinOverride,
            OneWirePinOverride: assignment.OneWirePinOverride,
            AnalogPinOverride: assignment.AnalogPinOverride,
            DigitalPinOverride: assignment.DigitalPinOverride,
            TriggerPinOverride: assignment.TriggerPinOverride,
            EchoPinOverride: assignment.EchoPinOverride,
            BaudRateOverride: assignment.BaudRateOverride,
            IntervalSecondsOverride: assignment.IntervalSecondsOverride,
            IsActive: assignment.IsActive,
            LastSeenAt: assignment.LastSeenAt,
            AssignedAt: assignment.AssignedAt,
            EffectiveConfig: effectiveConfig
        );
    }

    /// <summary>
    /// Converts a CreateNodeSensorAssignmentDto to a NodeSensorAssignment entity
    /// </summary>
    public static NodeSensorAssignment ToEntity(this CreateNodeSensorAssignmentDto dto, Guid nodeId)
    {
        return new NodeSensorAssignment
        {
            Id = Guid.NewGuid(),
            NodeId = nodeId,
            SensorId = dto.SensorId,
            EndpointId = dto.EndpointId,
            Alias = dto.Alias,
            I2CAddressOverride = dto.I2CAddressOverride,
            SdaPinOverride = dto.SdaPinOverride,
            SclPinOverride = dto.SclPinOverride,
            OneWirePinOverride = dto.OneWirePinOverride,
            AnalogPinOverride = dto.AnalogPinOverride,
            DigitalPinOverride = dto.DigitalPinOverride,
            TriggerPinOverride = dto.TriggerPinOverride,
            EchoPinOverride = dto.EchoPinOverride,
            BaudRateOverride = dto.BaudRateOverride,
            IntervalSecondsOverride = dto.IntervalSecondsOverride,
            IsActive = true,
            AssignedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Applies an UpdateNodeSensorAssignmentDto to a NodeSensorAssignment entity
    /// </summary>
    public static void ApplyUpdate(this NodeSensorAssignment assignment, UpdateNodeSensorAssignmentDto dto)
    {
        if (dto.Alias != null)
            assignment.Alias = dto.Alias;

        if (dto.I2CAddressOverride != null)
            assignment.I2CAddressOverride = dto.I2CAddressOverride;

        if (dto.SdaPinOverride.HasValue)
            assignment.SdaPinOverride = dto.SdaPinOverride;

        if (dto.SclPinOverride.HasValue)
            assignment.SclPinOverride = dto.SclPinOverride;

        if (dto.OneWirePinOverride.HasValue)
            assignment.OneWirePinOverride = dto.OneWirePinOverride;

        if (dto.AnalogPinOverride.HasValue)
            assignment.AnalogPinOverride = dto.AnalogPinOverride;

        if (dto.DigitalPinOverride.HasValue)
            assignment.DigitalPinOverride = dto.DigitalPinOverride;

        if (dto.TriggerPinOverride.HasValue)
            assignment.TriggerPinOverride = dto.TriggerPinOverride;

        if (dto.EchoPinOverride.HasValue)
            assignment.EchoPinOverride = dto.EchoPinOverride;

        if (dto.BaudRateOverride.HasValue)
            assignment.BaudRateOverride = dto.BaudRateOverride;

        if (dto.IntervalSecondsOverride.HasValue)
            assignment.IntervalSecondsOverride = dto.IntervalSecondsOverride;

        if (dto.IsActive.HasValue)
            assignment.IsActive = dto.IsActive.Value;
    }
}
