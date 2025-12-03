using myIoTGrid.Hub.Domain.Entities;
using myIoTGrid.Hub.Shared.DTOs;

namespace myIoTGrid.Hub.Service.Interfaces;

/// <summary>
/// Service Interface for calculating effective configuration values (v3.0).
/// Two-tier inheritance: Assignment → Sensor
/// EffectiveValue = Assignment ?? Sensor
/// </summary>
public interface IEffectiveConfigService
{
    /// <summary>
    /// Gets the effective configuration for an assignment.
    /// EffectiveValue = Assignment ?? Sensor
    /// </summary>
    EffectiveConfigDto GetEffectiveConfig(
        NodeSensorAssignment assignment,
        Sensor sensor);

    /// <summary>
    /// Applies calibration to a raw value.
    /// CalibratedValue = (RawValue * Gain) + Offset
    /// </summary>
    double ApplyCalibration(double rawValue, Sensor sensor);

    /// <summary>
    /// Gets the effective offset correction from the sensor.
    /// </summary>
    double GetEffectiveOffset(Sensor sensor);

    /// <summary>
    /// Gets the effective gain correction from the sensor.
    /// </summary>
    double GetEffectiveGain(Sensor sensor);

    /// <summary>
    /// Gets the effective interval in seconds.
    /// Priority: Assignment → Sensor
    /// </summary>
    int GetEffectiveInterval(NodeSensorAssignment? assignment, Sensor sensor);
}
