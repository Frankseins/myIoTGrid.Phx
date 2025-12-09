using FluentAssertions;
using myIoTGrid.Hub.Service.Services;

namespace myIoTGrid.Hub.Service.Tests.Services;

/// <summary>
/// Tests for EffectiveConfigService (v3.0 Two-Tier Model).
/// Calculates effective configuration values with two-tier inheritance:
/// Assignment → Sensor
/// </summary>
public class EffectiveConfigServiceTests
{
    private readonly EffectiveConfigService _sut;

    public EffectiveConfigServiceTests()
    {
        _sut = new EffectiveConfigService();
    }

    #region GetEffectiveConfig Tests

    [Fact]
    public void GetEffectiveConfig_WithNoOverrides_UsesSensorValues()
    {
        // Arrange
        var sensor = CreateSensor(
            intervalSeconds: 60,
            i2cAddress: "0x40",
            sdaPin: 21,
            sclPin: 22,
            oneWirePin: 4,
            analogPin: 34,
            digitalPin: 17,
            triggerPin: 5,
            echoPin: 18,
            offsetCorrection: 0.5,
            gainCorrection: 1.02
        );
        var assignment = CreateAssignment();

        // Act
        var result = _sut.GetEffectiveConfig(assignment, sensor);

        // Assert
        result.IntervalSeconds.Should().Be(60);
        result.I2CAddress.Should().Be("0x40");
        result.SdaPin.Should().Be(21);
        result.SclPin.Should().Be(22);
        result.OneWirePin.Should().Be(4);
        result.AnalogPin.Should().Be(34);
        result.DigitalPin.Should().Be(17);
        result.TriggerPin.Should().Be(5);
        result.EchoPin.Should().Be(18);
        result.OffsetCorrection.Should().Be(0.5);
        result.GainCorrection.Should().Be(1.02);
    }

    [Fact]
    public void GetEffectiveConfig_WithAssignmentOverrides_UsesOverrides()
    {
        // Arrange
        var sensor = CreateSensor(
            intervalSeconds: 60,
            i2cAddress: "0x40",
            sdaPin: 21,
            sclPin: 22,
            oneWirePin: 4,
            analogPin: 34,
            digitalPin: 17
        );
        var assignment = CreateAssignment(
            intervalOverride: 30,
            i2cAddressOverride: "0x41",
            sdaPinOverride: 25,
            sclPinOverride: 26,
            oneWirePinOverride: 15,
            analogPinOverride: 35,
            digitalPinOverride: 18,
            triggerPinOverride: 12,
            echoPinOverride: 13
        );

        // Act
        var result = _sut.GetEffectiveConfig(assignment, sensor);

        // Assert
        result.IntervalSeconds.Should().Be(30);
        result.I2CAddress.Should().Be("0x41");
        result.SdaPin.Should().Be(25);
        result.SclPin.Should().Be(26);
        result.OneWirePin.Should().Be(15);
        result.AnalogPin.Should().Be(35);
        result.DigitalPin.Should().Be(18);
        result.TriggerPin.Should().Be(12);
        result.EchoPin.Should().Be(13);
    }

    [Fact]
    public void GetEffectiveConfig_WithPartialOverrides_UsesMixedValues()
    {
        // Arrange
        var sensor = CreateSensor(
            intervalSeconds: 60,
            i2cAddress: "0x40",
            sdaPin: 21,
            sclPin: 22
        );
        var assignment = CreateAssignment(
            sdaPinOverride: 25
            // sclPin not overridden
        );

        // Act
        var result = _sut.GetEffectiveConfig(assignment, sensor);

        // Assert
        result.SdaPin.Should().Be(25);  // Overridden
        result.SclPin.Should().Be(22);  // From sensor
    }

    [Fact]
    public void GetEffectiveConfig_CalibrationValuesFromSensor()
    {
        // Arrange
        var sensor = CreateSensor(offsetCorrection: 1.5, gainCorrection: 0.98);
        var assignment = CreateAssignment();

        // Act
        var result = _sut.GetEffectiveConfig(assignment, sensor);

        // Assert
        result.OffsetCorrection.Should().Be(1.5);
        result.GainCorrection.Should().Be(0.98);
    }

    #endregion

    #region ApplyCalibration Tests

    [Fact]
    public void ApplyCalibration_WithDefaultValues_ReturnsRawValue()
    {
        // Arrange
        var sensor = CreateSensor(offsetCorrection: 0, gainCorrection: 1.0);

        // Act
        var result = _sut.ApplyCalibration(21.5, sensor);

        // Assert
        result.Should().Be(21.5);
    }

    [Fact]
    public void ApplyCalibration_WithOffset_AddsOffset()
    {
        // Arrange
        var sensor = CreateSensor(offsetCorrection: 0.5, gainCorrection: 1.0);

        // Act
        var result = _sut.ApplyCalibration(21.0, sensor);

        // Assert
        result.Should().Be(21.5);  // (21.0 * 1.0) + 0.5
    }

    [Fact]
    public void ApplyCalibration_WithGain_MultipliesGain()
    {
        // Arrange
        var sensor = CreateSensor(offsetCorrection: 0, gainCorrection: 1.02);

        // Act
        var result = _sut.ApplyCalibration(100.0, sensor);

        // Assert
        result.Should().BeApproximately(102.0, 0.001);  // (100.0 * 1.02) + 0
    }

    [Fact]
    public void ApplyCalibration_WithBoth_AppliesGainThenOffset()
    {
        // Arrange
        var sensor = CreateSensor(offsetCorrection: 0.5, gainCorrection: 1.02);

        // Act
        var result = _sut.ApplyCalibration(21.5, sensor);

        // Assert
        // (21.5 * 1.02) + 0.5 = 21.93 + 0.5 = 22.43
        result.Should().BeApproximately(22.43, 0.001);
    }

    [Fact]
    public void ApplyCalibration_WithNegativeOffset_SubtractsOffset()
    {
        // Arrange
        var sensor = CreateSensor(offsetCorrection: -2.0, gainCorrection: 1.0);

        // Act
        var result = _sut.ApplyCalibration(25.0, sensor);

        // Assert
        result.Should().Be(23.0);  // (25.0 * 1.0) + (-2.0)
    }

    [Fact]
    public void ApplyCalibration_WithLowGain_ScalesDown()
    {
        // Arrange
        var sensor = CreateSensor(offsetCorrection: 0, gainCorrection: 0.9);

        // Act
        var result = _sut.ApplyCalibration(100.0, sensor);

        // Assert
        result.Should().Be(90.0);  // (100.0 * 0.9) + 0
    }

    [Fact]
    public void ApplyCalibration_WithZeroGain_ReturnsOffset()
    {
        // Arrange - edge case (should not happen in practice)
        var sensor = CreateSensor(offsetCorrection: 5.0, gainCorrection: 0);

        // Act
        var result = _sut.ApplyCalibration(100.0, sensor);

        // Assert
        result.Should().Be(5.0);  // (100.0 * 0) + 5.0
    }

    #endregion

    #region GetEffectiveOffset Tests

    [Fact]
    public void GetEffectiveOffset_ReturnsSensorOffset()
    {
        // Arrange
        var sensor = CreateSensor(offsetCorrection: 1.5);

        // Act
        var result = _sut.GetEffectiveOffset(sensor);

        // Assert
        result.Should().Be(1.5);
    }

    [Fact]
    public void GetEffectiveOffset_WithZeroOffset_ReturnsZero()
    {
        // Arrange
        var sensor = CreateSensor(offsetCorrection: 0);

        // Act
        var result = _sut.GetEffectiveOffset(sensor);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void GetEffectiveOffset_WithNegativeOffset_ReturnsNegative()
    {
        // Arrange
        var sensor = CreateSensor(offsetCorrection: -2.5);

        // Act
        var result = _sut.GetEffectiveOffset(sensor);

        // Assert
        result.Should().Be(-2.5);
    }

    #endregion

    #region GetEffectiveGain Tests

    [Fact]
    public void GetEffectiveGain_ReturnsSensorGain()
    {
        // Arrange
        var sensor = CreateSensor(gainCorrection: 1.05);

        // Act
        var result = _sut.GetEffectiveGain(sensor);

        // Assert
        result.Should().Be(1.05);
    }

    [Fact]
    public void GetEffectiveGain_WithDefaultGain_ReturnsOne()
    {
        // Arrange
        var sensor = CreateSensor(gainCorrection: 1.0);

        // Act
        var result = _sut.GetEffectiveGain(sensor);

        // Assert
        result.Should().Be(1.0);
    }

    #endregion

    #region GetEffectiveInterval Tests

    [Fact]
    public void GetEffectiveInterval_WithNoAssignment_ReturnsSensorInterval()
    {
        // Arrange
        var sensor = CreateSensor(intervalSeconds: 60);

        // Act
        var result = _sut.GetEffectiveInterval(null, sensor);

        // Assert
        result.Should().Be(60);
    }

    [Fact]
    public void GetEffectiveInterval_WithAssignmentNoOverride_ReturnsSensorInterval()
    {
        // Arrange
        var sensor = CreateSensor(intervalSeconds: 60);
        var assignment = CreateAssignment(intervalOverride: null);

        // Act
        var result = _sut.GetEffectiveInterval(assignment, sensor);

        // Assert
        result.Should().Be(60);
    }

    [Fact]
    public void GetEffectiveInterval_WithAssignmentOverride_ReturnsOverride()
    {
        // Arrange
        var sensor = CreateSensor(intervalSeconds: 60);
        var assignment = CreateAssignment(intervalOverride: 30);

        // Act
        var result = _sut.GetEffectiveInterval(assignment, sensor);

        // Assert
        result.Should().Be(30);
    }

    [Fact]
    public void GetEffectiveInterval_WithZeroOverride_ReturnsZero()
    {
        // Arrange - edge case (interval override can't be 0 in practice, but test the logic)
        var sensor = CreateSensor(intervalSeconds: 60);
        var assignment = CreateAssignment();
        assignment.IntervalSecondsOverride = 0;

        // Act
        var result = _sut.GetEffectiveInterval(assignment, sensor);

        // Assert
        // Note: 0 is "HasValue == true" so it will be used
        result.Should().Be(0);
    }

    #endregion

    #region Helper Methods

    private Sensor CreateSensor(
        int intervalSeconds = 60,
        string? i2cAddress = null,
        int? sdaPin = null,
        int? sclPin = null,
        int? oneWirePin = null,
        int? analogPin = null,
        int? digitalPin = null,
        int? triggerPin = null,
        int? echoPin = null,
        double offsetCorrection = 0,
        double gainCorrection = 1.0)
    {
        return new Sensor
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Code = "test-sensor",
            Name = "Test Sensor",
            Protocol = CommunicationProtocol.Digital,
            Category = "climate",
            IntervalSeconds = intervalSeconds,
            MinIntervalSeconds = 2,
            I2CAddress = i2cAddress,
            SdaPin = sdaPin,
            SclPin = sclPin,
            OneWirePin = oneWirePin,
            AnalogPin = analogPin,
            DigitalPin = digitalPin,
            TriggerPin = triggerPin,
            EchoPin = echoPin,
            OffsetCorrection = offsetCorrection,
            GainCorrection = gainCorrection,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private NodeSensorAssignment CreateAssignment(
        int? intervalOverride = null,
        string? i2cAddressOverride = null,
        int? sdaPinOverride = null,
        int? sclPinOverride = null,
        int? oneWirePinOverride = null,
        int? analogPinOverride = null,
        int? digitalPinOverride = null,
        int? triggerPinOverride = null,
        int? echoPinOverride = null)
    {
        return new NodeSensorAssignment
        {
            Id = Guid.NewGuid(),
            NodeId = Guid.NewGuid(),
            SensorId = Guid.NewGuid(),
            EndpointId = 1,
            IntervalSecondsOverride = intervalOverride,
            I2CAddressOverride = i2cAddressOverride,
            SdaPinOverride = sdaPinOverride,
            SclPinOverride = sclPinOverride,
            OneWirePinOverride = oneWirePinOverride,
            AnalogPinOverride = analogPinOverride,
            DigitalPinOverride = digitalPinOverride,
            TriggerPinOverride = triggerPinOverride,
            EchoPinOverride = echoPinOverride,
            IsActive = true,
            AssignedAt = DateTime.UtcNow
        };
    }

    #endregion
}
