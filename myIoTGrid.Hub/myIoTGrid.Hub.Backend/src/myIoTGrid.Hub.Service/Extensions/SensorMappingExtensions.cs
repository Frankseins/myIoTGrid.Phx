using myIoTGrid.Hub.Domain.Entities;
using myIoTGrid.Hub.Shared.DTOs;
using myIoTGrid.Hub.Shared.Enums;

namespace myIoTGrid.Hub.Service.Extensions;

/// <summary>
/// Mapping Extensions for Sensor Entity (v3.0).
/// Complete sensor definition with hardware configuration and calibration.
/// Two-tier model: Sensor → NodeSensorAssignment
/// </summary>
public static class SensorMappingExtensions
{
    /// <summary>
    /// Converts a Sensor entity to a SensorDto
    /// </summary>
    public static SensorDto ToDto(this Sensor sensor)
    {
        return new SensorDto(
            Id: sensor.Id,
            TenantId: sensor.TenantId,

            // === Identification ===
            Code: sensor.Code,
            Name: sensor.Name,
            Description: sensor.Description,
            SerialNumber: sensor.SerialNumber,

            // === Hardware Info ===
            Manufacturer: sensor.Manufacturer,
            Model: sensor.Model,
            DatasheetUrl: sensor.DatasheetUrl,

            // === Communication Protocol ===
            Protocol: (CommunicationProtocolDto)sensor.Protocol,

            // === Pin Configuration ===
            I2CAddress: sensor.I2CAddress,
            SdaPin: sensor.SdaPin,
            SclPin: sensor.SclPin,
            OneWirePin: sensor.OneWirePin,
            AnalogPin: sensor.AnalogPin,
            DigitalPin: sensor.DigitalPin,
            TriggerPin: sensor.TriggerPin,
            EchoPin: sensor.EchoPin,

            // === Timing Configuration ===
            IntervalSeconds: sensor.IntervalSeconds,
            MinIntervalSeconds: sensor.MinIntervalSeconds,
            WarmupTimeMs: sensor.WarmupTimeMs,

            // === Calibration ===
            OffsetCorrection: sensor.OffsetCorrection,
            GainCorrection: sensor.GainCorrection,
            LastCalibratedAt: sensor.LastCalibratedAt,
            CalibrationNotes: sensor.CalibrationNotes,
            CalibrationDueAt: sensor.CalibrationDueAt,

            // === Categorization ===
            Category: sensor.Category,
            Icon: sensor.Icon,
            Color: sensor.Color,

            // === Capabilities ===
            Capabilities: sensor.Capabilities.Select(c => c.ToDto()),

            // === Status ===
            IsActive: sensor.IsActive,
            CreatedAt: sensor.CreatedAt,
            UpdatedAt: sensor.UpdatedAt
        );
    }

    /// <summary>
    /// Converts a SensorCapability entity to a SensorCapabilityDto
    /// </summary>
    public static SensorCapabilityDto ToDto(this SensorCapability capability)
    {
        return new SensorCapabilityDto(
            Id: capability.Id,
            SensorId: capability.SensorId,
            MeasurementType: capability.MeasurementType,
            DisplayName: capability.DisplayName,
            Unit: capability.Unit,
            MinValue: capability.MinValue,
            MaxValue: capability.MaxValue,
            Resolution: capability.Resolution,
            Accuracy: capability.Accuracy,
            MatterClusterId: capability.MatterClusterId,
            MatterClusterName: capability.MatterClusterName,
            SortOrder: capability.SortOrder,
            IsActive: capability.IsActive
        );
    }

    /// <summary>
    /// Converts a CreateSensorDto to a Sensor entity
    /// </summary>
    public static Sensor ToEntity(this CreateSensorDto dto, Guid tenantId)
    {
        var now = DateTime.UtcNow;
        var sensor = new Sensor
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,

            // === Identification ===
            Code = dto.Code.ToLowerInvariant(),
            Name = dto.Name,
            Description = dto.Description,
            SerialNumber = dto.SerialNumber,

            // === Hardware Info ===
            Manufacturer = dto.Manufacturer,
            Model = dto.Model,
            DatasheetUrl = dto.DatasheetUrl,

            // === Communication Protocol ===
            Protocol = (Domain.Enums.CommunicationProtocol)dto.Protocol,

            // === Pin Configuration ===
            I2CAddress = dto.I2CAddress,
            SdaPin = dto.SdaPin,
            SclPin = dto.SclPin,
            OneWirePin = dto.OneWirePin,
            AnalogPin = dto.AnalogPin,
            DigitalPin = dto.DigitalPin,
            TriggerPin = dto.TriggerPin,
            EchoPin = dto.EchoPin,

            // === Timing Configuration ===
            IntervalSeconds = dto.IntervalSeconds,
            MinIntervalSeconds = dto.MinIntervalSeconds,
            WarmupTimeMs = dto.WarmupTimeMs,

            // === Calibration ===
            OffsetCorrection = dto.OffsetCorrection,
            GainCorrection = dto.GainCorrection,

            // === Categorization ===
            Category = dto.Category.ToLowerInvariant(),
            Icon = dto.Icon,
            Color = dto.Color,

            // === Status ===
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        // Add capabilities
        if (dto.Capabilities != null)
        {
            var sortOrder = 0;
            foreach (var cap in dto.Capabilities)
            {
                sensor.Capabilities.Add(new SensorCapability
                {
                    Id = Guid.NewGuid(),
                    SensorId = sensor.Id,
                    MeasurementType = cap.MeasurementType.ToLowerInvariant(),
                    DisplayName = cap.DisplayName,
                    Unit = cap.Unit,
                    MinValue = cap.MinValue,
                    MaxValue = cap.MaxValue,
                    Resolution = cap.Resolution,
                    Accuracy = cap.Accuracy,
                    MatterClusterId = cap.MatterClusterId,
                    MatterClusterName = cap.MatterClusterName,
                    SortOrder = cap.SortOrder != 0 ? cap.SortOrder : sortOrder++,
                    IsActive = true
                });
            }
        }

        return sensor;
    }

    /// <summary>
    /// Applies an UpdateSensorDto to a Sensor entity
    /// </summary>
    public static void ApplyUpdate(this Sensor sensor, UpdateSensorDto dto)
    {
        // === Identification ===
        if (!string.IsNullOrEmpty(dto.Name))
            sensor.Name = dto.Name;

        if (dto.Description != null)
            sensor.Description = dto.Description;

        if (dto.SerialNumber != null)
            sensor.SerialNumber = dto.SerialNumber;

        // === Hardware Info ===
        if (dto.Manufacturer != null)
            sensor.Manufacturer = dto.Manufacturer;

        if (dto.Model != null)
            sensor.Model = dto.Model;

        if (dto.DatasheetUrl != null)
            sensor.DatasheetUrl = dto.DatasheetUrl;

        // === Pin Configuration ===
        if (dto.I2CAddress != null)
            sensor.I2CAddress = dto.I2CAddress;

        if (dto.SdaPin.HasValue)
            sensor.SdaPin = dto.SdaPin;

        if (dto.SclPin.HasValue)
            sensor.SclPin = dto.SclPin;

        if (dto.OneWirePin.HasValue)
            sensor.OneWirePin = dto.OneWirePin;

        if (dto.AnalogPin.HasValue)
            sensor.AnalogPin = dto.AnalogPin;

        if (dto.DigitalPin.HasValue)
            sensor.DigitalPin = dto.DigitalPin;

        if (dto.TriggerPin.HasValue)
            sensor.TriggerPin = dto.TriggerPin;

        if (dto.EchoPin.HasValue)
            sensor.EchoPin = dto.EchoPin;

        // === Timing Configuration ===
        if (dto.IntervalSeconds.HasValue)
            sensor.IntervalSeconds = dto.IntervalSeconds.Value;

        if (dto.MinIntervalSeconds.HasValue)
            sensor.MinIntervalSeconds = dto.MinIntervalSeconds.Value;

        if (dto.WarmupTimeMs.HasValue)
            sensor.WarmupTimeMs = dto.WarmupTimeMs.Value;

        // === Calibration ===
        if (dto.OffsetCorrection.HasValue)
            sensor.OffsetCorrection = dto.OffsetCorrection.Value;

        if (dto.GainCorrection.HasValue)
            sensor.GainCorrection = dto.GainCorrection.Value;

        if (dto.CalibrationNotes != null)
            sensor.CalibrationNotes = dto.CalibrationNotes;

        // === Categorization ===
        if (dto.Category != null)
            sensor.Category = dto.Category.ToLowerInvariant();

        if (dto.Icon != null)
            sensor.Icon = dto.Icon;

        if (dto.Color != null)
            sensor.Color = dto.Color;

        // === Status ===
        if (dto.IsActive.HasValue)
            sensor.IsActive = dto.IsActive.Value;

        sensor.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Applies calibration to a Sensor entity
    /// </summary>
    public static void ApplyCalibration(this Sensor sensor, CalibrateSensorDto dto)
    {
        sensor.OffsetCorrection = dto.OffsetCorrection;
        sensor.GainCorrection = dto.GainCorrection;
        sensor.CalibrationNotes = dto.CalibrationNotes;
        sensor.CalibrationDueAt = dto.CalibrationDueAt;
        sensor.LastCalibratedAt = DateTime.UtcNow;
        sensor.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Converts a list of Sensor Entities to DTOs
    /// </summary>
    public static IEnumerable<SensorDto> ToDtos(this IEnumerable<Sensor> sensors)
    {
        return sensors.Select(s => s.ToDto());
    }

    /// <summary>
    /// Converts a list of SensorCapability Entities to DTOs
    /// </summary>
    public static IEnumerable<SensorCapabilityDto> ToDtos(this IEnumerable<SensorCapability> capabilities)
    {
        return capabilities.Select(c => c.ToDto());
    }
}
