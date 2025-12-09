using System.Text.Json;
using FluentAssertions;

namespace myIoTGrid.Hub.Service.Tests.Converters;

/// <summary>
/// Tests for custom JSON converters.
/// These converters handle ESP32 firmware sending string or integer values.
/// </summary>
public class JsonConverterTests
{
    #region DebugLevelStringConverter Tests

    [Theory]
    [InlineData("\"Production\"", DebugLevelDto.Production)]
    [InlineData("\"Normal\"", DebugLevelDto.Normal)]
    [InlineData("\"Debug\"", DebugLevelDto.Debug)]
    [InlineData("\"production\"", DebugLevelDto.Production)]
    [InlineData("\"normal\"", DebugLevelDto.Normal)]
    [InlineData("\"debug\"", DebugLevelDto.Debug)]
    [InlineData("\"PRODUCTION\"", DebugLevelDto.Production)]
    [InlineData("\"NORMAL\"", DebugLevelDto.Normal)]
    [InlineData("\"DEBUG\"", DebugLevelDto.Debug)]
    public void DebugLevelStringConverter_Read_StringValue_ReturnsCorrectEnum(string json, DebugLevelDto expected)
    {
        // Arrange
        var options = new JsonSerializerOptions();
        options.Converters.Add(new DebugLevelStringConverter());

        // Act
        var result = JsonSerializer.Deserialize<DebugLevelDto>(json, options);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("0", DebugLevelDto.Production)]
    [InlineData("1", DebugLevelDto.Normal)]
    [InlineData("2", DebugLevelDto.Debug)]
    public void DebugLevelStringConverter_Read_IntegerValue_ReturnsCorrectEnum(string json, DebugLevelDto expected)
    {
        // Arrange
        var options = new JsonSerializerOptions();
        options.Converters.Add(new DebugLevelStringConverter());

        // Act
        var result = JsonSerializer.Deserialize<DebugLevelDto>(json, options);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void DebugLevelStringConverter_Read_InvalidString_ReturnsDefault()
    {
        // Arrange
        var options = new JsonSerializerOptions();
        options.Converters.Add(new DebugLevelStringConverter());
        var json = "\"invalid\"";

        // Act
        var result = JsonSerializer.Deserialize<DebugLevelDto>(json, options);

        // Assert
        result.Should().Be(DebugLevelDto.Normal); // Default
    }

    [Theory]
    [InlineData(DebugLevelDto.Production, "Production")]
    [InlineData(DebugLevelDto.Normal, "Normal")]
    [InlineData(DebugLevelDto.Debug, "Debug")]
    public void DebugLevelStringConverter_Write_WritesStringValue(DebugLevelDto value, string expected)
    {
        // Arrange
        var options = new JsonSerializerOptions();
        options.Converters.Add(new DebugLevelStringConverter());

        // Act
        var result = JsonSerializer.Serialize(value, options);

        // Assert
        result.Should().Be($"\"{expected}\"");
    }

    #endregion

    #region LogCategoryStringConverter Tests

    [Theory]
    [InlineData("\"System\"", LogCategoryDto.System)]
    [InlineData("\"Hardware\"", LogCategoryDto.Hardware)]
    [InlineData("\"Network\"", LogCategoryDto.Network)]
    [InlineData("\"Sensor\"", LogCategoryDto.Sensor)]
    [InlineData("\"GPS\"", LogCategoryDto.GPS)]
    [InlineData("\"API\"", LogCategoryDto.API)]
    [InlineData("\"Storage\"", LogCategoryDto.Storage)]
    [InlineData("\"Error\"", LogCategoryDto.Error)]
    [InlineData("\"system\"", LogCategoryDto.System)]
    [InlineData("\"hardware\"", LogCategoryDto.Hardware)]
    [InlineData("\"network\"", LogCategoryDto.Network)]
    public void LogCategoryStringConverter_Read_StringValue_ReturnsCorrectEnum(string json, LogCategoryDto expected)
    {
        // Arrange
        var options = new JsonSerializerOptions();
        options.Converters.Add(new LogCategoryStringConverter());

        // Act
        var result = JsonSerializer.Deserialize<LogCategoryDto>(json, options);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("0", LogCategoryDto.System)]
    [InlineData("1", LogCategoryDto.Hardware)]
    [InlineData("2", LogCategoryDto.Network)]
    [InlineData("3", LogCategoryDto.Sensor)]
    [InlineData("4", LogCategoryDto.GPS)]
    [InlineData("5", LogCategoryDto.API)]
    [InlineData("6", LogCategoryDto.Storage)]
    [InlineData("7", LogCategoryDto.Error)]
    public void LogCategoryStringConverter_Read_IntegerValue_ReturnsCorrectEnum(string json, LogCategoryDto expected)
    {
        // Arrange
        var options = new JsonSerializerOptions();
        options.Converters.Add(new LogCategoryStringConverter());

        // Act
        var result = JsonSerializer.Deserialize<LogCategoryDto>(json, options);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void LogCategoryStringConverter_Read_InvalidString_ReturnsDefault()
    {
        // Arrange
        var options = new JsonSerializerOptions();
        options.Converters.Add(new LogCategoryStringConverter());
        var json = "\"invalid\"";

        // Act
        var result = JsonSerializer.Deserialize<LogCategoryDto>(json, options);

        // Assert
        result.Should().Be(LogCategoryDto.System); // Default
    }

    [Theory]
    [InlineData(LogCategoryDto.System, "System")]
    [InlineData(LogCategoryDto.Hardware, "Hardware")]
    [InlineData(LogCategoryDto.Network, "Network")]
    [InlineData(LogCategoryDto.Sensor, "Sensor")]
    [InlineData(LogCategoryDto.GPS, "GPS")]
    [InlineData(LogCategoryDto.API, "API")]
    [InlineData(LogCategoryDto.Storage, "Storage")]
    [InlineData(LogCategoryDto.Error, "Error")]
    public void LogCategoryStringConverter_Write_WritesStringValue(LogCategoryDto value, string expected)
    {
        // Arrange
        var options = new JsonSerializerOptions();
        options.Converters.Add(new LogCategoryStringConverter());

        // Act
        var result = JsonSerializer.Serialize(value, options);

        // Assert
        result.Should().Be($"\"{expected}\"");
    }

    #endregion

    #region Integration Tests with DTOs

    [Fact]
    public void CreateNodeDebugLogDto_Deserialize_WithStringEnums()
    {
        // Arrange - This simulates what ESP32 firmware sends
        var json = """
        {
            "nodeTimestamp": 1234567890,
            "level": "Debug",
            "category": "Sensor",
            "message": "Test message"
        }
        """;

        // Act - Use options with converters
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new DebugLevelStringConverter());
        options.Converters.Add(new LogCategoryStringConverter());
        var result = JsonSerializer.Deserialize<TestDebugLogDto>(json, options);

        // Assert
        result.Should().NotBeNull();
        result!.NodeTimestamp.Should().Be(1234567890);
        result.Level.Should().Be(DebugLevelDto.Debug);
        result.Category.Should().Be(LogCategoryDto.Sensor);
        result.Message.Should().Be("Test message");
    }

    [Fact]
    public void CreateNodeDebugLogDto_Deserialize_WithIntEnums()
    {
        // Arrange - This simulates alternative format from firmware
        var json = """
        {
            "nodeTimestamp": 1234567890,
            "level": 2,
            "category": 3,
            "message": "Test message"
        }
        """;

        // Act - Use options with converters
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new DebugLevelStringConverter());
        options.Converters.Add(new LogCategoryStringConverter());
        var result = JsonSerializer.Deserialize<TestDebugLogDto>(json, options);

        // Assert
        result.Should().NotBeNull();
        result!.Level.Should().Be(DebugLevelDto.Debug);
        result.Category.Should().Be(LogCategoryDto.Sensor);
    }

    [Fact]
    public void CreateNodeDebugLogDto_Deserialize_WithMixedCaseEnums()
    {
        // Arrange
        var json = """
        {
            "nodeTimestamp": 1234567890,
            "level": "NORMAL",
            "category": "network",
            "message": "Test"
        }
        """;

        // Act - Use options with converters
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new DebugLevelStringConverter());
        options.Converters.Add(new LogCategoryStringConverter());
        var result = JsonSerializer.Deserialize<TestDebugLogDto>(json, options);

        // Assert
        result.Should().NotBeNull();
        result!.Level.Should().Be(DebugLevelDto.Normal);
        result.Category.Should().Be(LogCategoryDto.Network);
    }

    #endregion

    // Simple test DTO without attributes (converters added via options)
    private class TestDebugLogDto
    {
        public long NodeTimestamp { get; set; }
        public DebugLevelDto Level { get; set; }
        public LogCategoryDto Category { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
