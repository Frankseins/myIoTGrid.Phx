using myIoTGrid.Shared.Common.DTOs;
using myIoTGrid.Shared.Common.Entities;

namespace myIoTGrid.Shared.Contracts.Services;

/// <summary>
/// Service for computing effective configuration values (v3.0).
/// Two-tier inheritance: Assignment → Sensor
/// </summary>
public interface IEffectiveConfigService
{
    EffectiveConfigDto GetEffectiveConfig(NodeSensorAssignment assignment, Sensor sensor);
    double ApplyCalibration(double rawValue, Sensor sensor);
    double GetEffectiveOffset(Sensor sensor);
    double GetEffectiveGain(Sensor sensor);
    int GetEffectiveInterval(NodeSensorAssignment? assignment, Sensor sensor);
}
