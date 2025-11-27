using FluentAssertions;
using FluentValidation;
using myIoTGrid.Hub.Service.Validators;
using myIoTGrid.Hub.Shared.DTOs;

namespace myIoTGrid.Hub.Service.Tests.Validators;

public class CreateSensorDataValidatorTests
{
    private readonly CreateSensorDataValidator _sut;

    public CreateSensorDataValidatorTests()
    {
        _sut = new CreateSensorDataValidator();
    }

    [Fact]
    public void Validate_WithValidDto_ShouldNotHaveErrors()
    {
        // Arrange
        var dto = new CreateSensorDataDto(
            SensorId: "sensor-01",
            SensorType: "temperature",
            Value: 21.5
        );

        // Act
        var result = _sut.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptySensorId_ShouldHaveError(string? sensorId)
    {
        // Arrange
        var dto = new CreateSensorDataDto(
            SensorId: sensorId!,
            SensorType: "temperature",
            Value: 21.5
        );

        // Act
        var result = _sut.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateSensorDataDto.SensorId));
    }

    [Fact]
    public void Validate_WithInvalidSensorIdCharacters_ShouldHaveError()
    {
        // Arrange
        var dto = new CreateSensorDataDto(
            SensorId: "sensor@01!",
            SensorType: "temperature",
            Value: 21.5
        );

        // Act
        var result = _sut.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(CreateSensorDataDto.SensorId) &&
            e.ErrorMessage.Contains("letters", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptySensorType_ShouldHaveError(string? sensorType)
    {
        // Arrange
        var dto = new CreateSensorDataDto(
            SensorId: "sensor-01",
            SensorType: sensorType!,
            Value: 21.5
        );

        // Act
        var result = _sut.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateSensorDataDto.SensorType));
    }

    [Fact]
    public void Validate_WithUppercaseSensorType_ShouldHaveError()
    {
        // Arrange
        var dto = new CreateSensorDataDto(
            SensorId: "sensor-01",
            SensorType: "Temperature",
            Value: 21.5
        );

        // Act
        var result = _sut.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(CreateSensorDataDto.SensorType) &&
            e.ErrorMessage.Contains("lowercase", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_WithNaNValue_ShouldHaveError()
    {
        // Arrange
        var dto = new CreateSensorDataDto(
            SensorId: "sensor-01",
            SensorType: "temperature",
            Value: double.NaN
        );

        // Act
        var result = _sut.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateSensorDataDto.Value));
    }

    [Fact]
    public void Validate_WithInfinityValue_ShouldHaveError()
    {
        // Arrange
        var dto = new CreateSensorDataDto(
            SensorId: "sensor-01",
            SensorType: "temperature",
            Value: double.PositiveInfinity
        );

        // Act
        var result = _sut.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateSensorDataDto.Value));
    }

    [Fact]
    public void Validate_WithValidHubId_ShouldNotHaveError()
    {
        // Arrange
        var dto = new CreateSensorDataDto(
            SensorId: "sensor-01",
            SensorType: "temperature",
            Value: 21.5,
            HubId: "hub-home-01"
        );

        // Act
        var result = _sut.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateSensorDataDto.HubId));
    }

    [Fact]
    public void Validate_WithInvalidHubIdCharacters_ShouldHaveError()
    {
        // Arrange
        var dto = new CreateSensorDataDto(
            SensorId: "sensor-01",
            SensorType: "temperature",
            Value: 21.5,
            HubId: "hub@home"
        );

        // Act
        var result = _sut.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateSensorDataDto.HubId));
    }
}
