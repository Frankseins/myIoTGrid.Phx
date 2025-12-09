using FluentAssertions;
using myIoTGrid.Shared.Common.Constants;
using Xunit;

namespace myIoTGrid.Shared.Common.Tests;

public class SensorTypesTests
{
    [Theory]
    [InlineData("temperature", "°C")]
    [InlineData("humidity", "%")]
    [InlineData("pressure", "hPa")]
    [InlineData("co2", "ppm")]
    [InlineData("pm25", "µg/m³")]
    [InlineData("battery", "%")]
    public void GetUnit_ValidType_ReturnsCorrectUnit(string type, string expectedUnit)
    {
        // Act
        var result = SensorTypes.GetUnit(type);

        // Assert
        result.Should().Be(expectedUnit);
    }

    [Theory]
    [InlineData("TEMPERATURE", "°C")]
    [InlineData("Temperature", "°C")]
    [InlineData("HUMIDITY", "%")]
    public void GetUnit_CaseInsensitive_ReturnsCorrectUnit(string type, string expectedUnit)
    {
        // Act
        var result = SensorTypes.GetUnit(type);

        // Assert
        result.Should().Be(expectedUnit);
    }

    [Fact]
    public void GetUnit_InvalidType_ReturnsUnknown()
    {
        // Arrange
        var type = "invalid_sensor_type";

        // Act
        var result = SensorTypes.GetUnit(type);

        // Assert
        result.Should().Be("unknown");
    }

    [Theory]
    [InlineData("temperature", true)]
    [InlineData("humidity", true)]
    [InlineData("invalid", false)]
    [InlineData("", false)]
    public void IsValidType_ReturnsCorrectResult(string type, bool expected)
    {
        // Act
        var result = SensorTypes.IsValidType(type);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void GetAllTypes_ReturnsAllKnownTypes()
    {
        // Act
        var types = SensorTypes.GetAllTypes();

        // Assert
        types.Should().Contain("temperature");
        types.Should().Contain("humidity");
        types.Should().Contain("pressure");
        types.Should().Contain("co2");
        types.Should().HaveCountGreaterThan(10);
    }

    [Fact]
    public void Units_ContainsExpectedEntries()
    {
        // Assert
        SensorTypes.Units.Should().ContainKey("temperature");
        SensorTypes.Units.Should().ContainKey("humidity");
        SensorTypes.Units.Should().ContainKey("pressure");
    }
}
