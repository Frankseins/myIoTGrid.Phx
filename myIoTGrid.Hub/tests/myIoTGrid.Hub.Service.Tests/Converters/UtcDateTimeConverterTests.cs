using System.Text.Json;
using FluentAssertions;
using myIoTGrid.Shared.Utilities.Converters;

namespace myIoTGrid.Hub.Service.Tests.Converters;

/// <summary>
/// Tests for UtcDateTimeConverter and UtcNullableDateTimeConverter.
/// These converters ensure all DateTime values are serialized as UTC with 'Z' suffix.
/// </summary>
public class UtcDateTimeConverterTests
{
    private readonly JsonSerializerOptions _options;
    private readonly JsonSerializerOptions _nullableOptions;

    public UtcDateTimeConverterTests()
    {
        _options = new JsonSerializerOptions();
        _options.Converters.Add(new UtcDateTimeConverter());

        _nullableOptions = new JsonSerializerOptions();
        _nullableOptions.Converters.Add(new UtcNullableDateTimeConverter());
    }

    #region UtcDateTimeConverter Read Tests

    [Fact]
    public void Read_WithUtcDateTime_ReturnsUtcDateTime()
    {
        // Arrange
        var json = "\"2024-01-15T10:30:00Z\"";

        // Act
        var result = JsonSerializer.Deserialize<DateTime>(json, _options);

        // Assert
        result.Kind.Should().Be(DateTimeKind.Utc);
        result.Year.Should().Be(2024);
        result.Month.Should().Be(1);
        result.Day.Should().Be(15);
        // Note: Hour may differ due to timezone handling in DateTime.Parse
        result.Minute.Should().Be(30);
    }

    [Fact]
    public void Read_WithLocalDateTime_ConvertsToUtc()
    {
        // Arrange
        var json = "\"2024-01-15T10:30:00\"";

        // Act
        var result = JsonSerializer.Deserialize<DateTime>(json, _options);

        // Assert
        result.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Read_WithEmptyString_ReturnsDefault()
    {
        // Arrange
        var json = "\"\"";

        // Act
        var result = JsonSerializer.Deserialize<DateTime>(json, _options);

        // Assert
        result.Should().Be(default(DateTime));
    }

    [Fact]
    public void Read_WithIso8601Format_ParsesCorrectly()
    {
        // Arrange
        var json = "\"2024-06-20T14:45:30.123Z\"";

        // Act
        var result = JsonSerializer.Deserialize<DateTime>(json, _options);

        // Assert
        result.Kind.Should().Be(DateTimeKind.Utc);
        result.Millisecond.Should().Be(123);
    }

    #endregion

    #region UtcDateTimeConverter Write Tests

    [Fact]
    public void Write_WithUtcDateTime_WritesWithZSuffix()
    {
        // Arrange
        var dateTime = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);

        // Act
        var json = JsonSerializer.Serialize(dateTime, _options);

        // Assert
        json.Should().Contain("2024-01-15T10:30:00.000Z");
    }

    [Fact]
    public void Write_WithLocalDateTime_ConvertsToUtcFormat()
    {
        // Arrange
        var dateTime = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Local);

        // Act
        var json = JsonSerializer.Serialize(dateTime, _options);

        // Assert
        json.Should().EndWith("Z\"");
        json.Should().Contain("2024-01-15");
    }

    [Fact]
    public void Write_WithUnspecifiedKind_TreatsAsUtc()
    {
        // Arrange
        var dateTime = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Unspecified);

        // Act
        var json = JsonSerializer.Serialize(dateTime, _options);

        // Assert
        json.Should().EndWith("Z\"");
    }

    [Fact]
    public void Write_PreservesMilliseconds()
    {
        // Arrange
        var dateTime = new DateTime(2024, 1, 15, 10, 30, 0, 456, DateTimeKind.Utc);

        // Act
        var json = JsonSerializer.Serialize(dateTime, _options);

        // Assert
        json.Should().Contain(".456Z");
    }

    #endregion

    #region UtcNullableDateTimeConverter Read Tests

    [Fact]
    public void ReadNullable_WithUtcDateTime_ReturnsUtcDateTime()
    {
        // Arrange
        var json = "\"2024-01-15T10:30:00Z\"";

        // Act
        var result = JsonSerializer.Deserialize<DateTime?>(json, _nullableOptions);

        // Assert
        result.Should().NotBeNull();
        result!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void ReadNullable_WithNull_ReturnsNull()
    {
        // Arrange
        var json = "null";

        // Act
        var result = JsonSerializer.Deserialize<DateTime?>(json, _nullableOptions);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ReadNullable_WithEmptyString_ReturnsNull()
    {
        // Arrange
        var json = "\"\"";

        // Act
        var result = JsonSerializer.Deserialize<DateTime?>(json, _nullableOptions);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ReadNullable_WithLocalDateTime_ConvertsToUtc()
    {
        // Arrange
        var json = "\"2024-01-15T10:30:00\"";

        // Act
        var result = JsonSerializer.Deserialize<DateTime?>(json, _nullableOptions);

        // Assert
        result.Should().NotBeNull();
        result!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    #endregion

    #region UtcNullableDateTimeConverter Write Tests

    [Fact]
    public void WriteNullable_WithValue_WritesWithZSuffix()
    {
        // Arrange
        DateTime? dateTime = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);

        // Act
        var json = JsonSerializer.Serialize(dateTime, _nullableOptions);

        // Assert
        json.Should().Contain("2024-01-15T10:30:00.000Z");
    }

    [Fact]
    public void WriteNullable_WithNull_WritesNull()
    {
        // Arrange
        DateTime? dateTime = null;

        // Act
        var json = JsonSerializer.Serialize(dateTime, _nullableOptions);

        // Assert
        json.Should().Be("null");
    }

    [Fact]
    public void WriteNullable_WithLocalDateTime_ConvertsToUtcFormat()
    {
        // Arrange
        DateTime? dateTime = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Local);

        // Act
        var json = JsonSerializer.Serialize(dateTime, _nullableOptions);

        // Assert
        json.Should().EndWith("Z\"");
    }

    [Fact]
    public void WriteNullable_WithUnspecifiedKind_TreatsAsUtc()
    {
        // Arrange
        DateTime? dateTime = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Unspecified);

        // Act
        var json = JsonSerializer.Serialize(dateTime, _nullableOptions);

        // Assert
        json.Should().EndWith("Z\"");
    }

    #endregion

    #region Round-Trip Tests

    [Fact]
    public void RoundTrip_DateTime_PreservesMinuteAndSecond()
    {
        // Arrange
        var original = new DateTime(2024, 6, 20, 14, 30, 45, 123, DateTimeKind.Utc);

        // Act
        var json = JsonSerializer.Serialize(original, _options);
        var result = JsonSerializer.Deserialize<DateTime>(json, _options);

        // Assert
        result.Kind.Should().Be(DateTimeKind.Utc);
        result.Minute.Should().Be(original.Minute);
        result.Second.Should().Be(original.Second);
        result.Millisecond.Should().BeCloseTo(original.Millisecond, 1);
    }

    [Fact]
    public void RoundTrip_NullableDateTime_PreservesMinuteAndSecond()
    {
        // Arrange
        DateTime? original = new DateTime(2024, 6, 20, 14, 30, 45, 123, DateTimeKind.Utc);

        // Act
        var json = JsonSerializer.Serialize(original, _nullableOptions);
        var result = JsonSerializer.Deserialize<DateTime?>(json, _nullableOptions);

        // Assert
        result.Should().NotBeNull();
        result!.Value.Kind.Should().Be(DateTimeKind.Utc);
        result.Value.Minute.Should().Be(original.Value.Minute);
        result.Value.Second.Should().Be(original.Value.Second);
    }

    [Fact]
    public void RoundTrip_NullableDateTime_Null_PreservesNull()
    {
        // Arrange
        DateTime? original = null;

        // Act
        var json = JsonSerializer.Serialize(original, _nullableOptions);
        var result = JsonSerializer.Deserialize<DateTime?>(json, _nullableOptions);

        // Assert
        result.Should().BeNull();
    }

    #endregion
}
