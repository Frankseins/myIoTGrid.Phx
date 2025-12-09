
namespace myIoTGrid.Hub.Service.Services;

/// <summary>
/// Service for calculating effective configuration values (v3.0).
/// Two-tier inheritance: Assignment → Sensor
/// EffectiveValue = Assignment ?? Sensor
/// </summary>
public class EffectiveConfigService : IEffectiveConfigService
{
    /// <inheritdoc />
    public EffectiveConfigDto GetEffectiveConfig(
        NodeSensorAssignment assignment,
        Sensor sensor)
    {
        return new EffectiveConfigDto(
            IntervalSeconds: GetEffectiveInterval(assignment, sensor),
            I2CAddress: assignment.I2CAddressOverride ?? sensor.I2CAddress,
            SdaPin: assignment.SdaPinOverride ?? sensor.SdaPin,
            SclPin: assignment.SclPinOverride ?? sensor.SclPin,
            OneWirePin: assignment.OneWirePinOverride ?? sensor.OneWirePin,
            AnalogPin: assignment.AnalogPinOverride ?? sensor.AnalogPin,
            DigitalPin: assignment.DigitalPinOverride ?? sensor.DigitalPin,
            TriggerPin: assignment.TriggerPinOverride ?? sensor.TriggerPin,
            EchoPin: assignment.EchoPinOverride ?? sensor.EchoPin,
            BaudRate: assignment.BaudRateOverride ?? sensor.BaudRate,
            OffsetCorrection: GetEffectiveOffset(sensor),
            GainCorrection: GetEffectiveGain(sensor)
        );
    }

    /// <inheritdoc />
    public double ApplyCalibration(double rawValue, Sensor sensor)
    {
        var offset = GetEffectiveOffset(sensor);
        var gain = GetEffectiveGain(sensor);

        // Calibrated = (Raw * Gain) + Offset
        return (rawValue * gain) + offset;
    }

    /// <inheritdoc />
    public double GetEffectiveOffset(Sensor sensor)
    {
        // Sensor offset is the definitive value in the 2-tier model
        return sensor.OffsetCorrection;
    }

    /// <inheritdoc />
    public double GetEffectiveGain(Sensor sensor)
    {
        // Sensor gain is the definitive value in the 2-tier model
        return sensor.GainCorrection;
    }

    /// <inheritdoc />
    public int GetEffectiveInterval(NodeSensorAssignment? assignment, Sensor sensor)
    {
        // Priority: Assignment → Sensor
        if (assignment?.IntervalSecondsOverride.HasValue == true)
            return assignment.IntervalSecondsOverride.Value;

        return sensor.IntervalSeconds;
    }
}
