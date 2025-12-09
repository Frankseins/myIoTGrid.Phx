using FluentAssertions;

namespace myIoTGrid.Hub.Service.Tests.Extensions;

/// <summary>
/// Tests for ReadingMappingExtensions.
/// New 3-tier model: Reading uses AssignmentId + MeasurementType instead of SensorTypeId.
/// </summary>
public class ReadingMappingExtensionsTests
{
    private readonly Guid _tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly Guid _nodeId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private readonly Guid _assignmentId = Guid.Parse("00000000-0000-0000-0000-000000000003");

    #region ToDto Tests

    [Fact]
    public void ToDto_WithFullReading_ReturnsCorrectDto()
    {
        // Arrange
        var reading = new Reading
        {
            Id = 12345,
            TenantId = _tenantId,
            NodeId = _nodeId,
            AssignmentId = _assignmentId,
            MeasurementType = "temperature",
            RawValue = 22.0,
            Value = 21.5,  // Calibrated
            Unit = "°C",
            Timestamp = new DateTime(2025, 11, 28, 10, 0, 0, DateTimeKind.Utc),
            IsSyncedToCloud = true,
            Node = new Node
            {
                Id = _nodeId,
                Name = "Living Room Node",
                Location = new Location { Name = "Living Room" }
            }
        };

        // Act
        var result = reading.ToDto();

        // Assert
        result.Id.Should().Be(12345);
        result.TenantId.Should().Be(_tenantId);
        result.NodeId.Should().Be(_nodeId);
        result.NodeName.Should().Be("Living Room Node");
        result.AssignmentId.Should().Be(_assignmentId);
        result.MeasurementType.Should().Be("temperature");
        result.RawValue.Should().Be(22.0);
        result.Value.Should().Be(21.5);
        result.Unit.Should().Be("°C");
        result.Timestamp.Should().Be(new DateTime(2025, 11, 28, 10, 0, 0, DateTimeKind.Utc));
        result.IsSyncedToCloud.Should().BeTrue();
        result.Location.Should().NotBeNull();
        result.Location!.Name.Should().Be("Living Room");
    }

    [Fact]
    public void ToDto_WithoutNode_HasEmptyNodeNameAndNullLocation()
    {
        // Arrange
        var reading = new Reading
        {
            Id = 12345,
            TenantId = _tenantId,
            NodeId = _nodeId,
            AssignmentId = _assignmentId,
            MeasurementType = "humidity",
            RawValue = 65.0,
            Value = 65.0,
            Unit = "%",
            Timestamp = DateTime.UtcNow,
            IsSyncedToCloud = false,
            Node = null
        };

        // Act
        var result = reading.ToDto();

        // Assert
        result.NodeName.Should().BeEmpty();
        result.Location.Should().BeNull();
    }

    [Fact]
    public void ToDto_WithNodeButNoLocation_HasNullLocation()
    {
        // Arrange
        var reading = new Reading
        {
            Id = 12345,
            TenantId = _tenantId,
            NodeId = _nodeId,
            AssignmentId = _assignmentId,
            MeasurementType = "temperature",
            RawValue = 21.5,
            Value = 21.5,
            Unit = "°C",
            Timestamp = DateTime.UtcNow,
            IsSyncedToCloud = false,
            Node = new Node
            {
                Id = _nodeId,
                Name = "Test Node",
                Location = null
            }
        };

        // Act
        var result = reading.ToDto();

        // Assert
        result.NodeName.Should().Be("Test Node");
        result.Location.Should().BeNull();
    }

    [Fact]
    public void ToDto_WithCalibration_ShowsBothRawAndCalibratedValues()
    {
        // Arrange
        var reading = new Reading
        {
            Id = 12345,
            TenantId = _tenantId,
            NodeId = _nodeId,
            AssignmentId = _assignmentId,
            MeasurementType = "temperature",
            RawValue = 20.0,  // Raw sensor value
            Value = 21.5,     // After applying calibration: (20.0 * 1.05) + 0.5 = 21.5
            Unit = "°C",
            Timestamp = DateTime.UtcNow,
            IsSyncedToCloud = false
        };

        // Act
        var result = reading.ToDto();

        // Assert
        result.RawValue.Should().Be(20.0);
        result.Value.Should().Be(21.5);
        result.Unit.Should().Be("°C");
    }

    [Fact]
    public void ToDto_WithDifferentMeasurementTypes_PreservesType()
    {
        // Arrange
        var reading = new Reading
        {
            Id = 12345,
            TenantId = _tenantId,
            NodeId = _nodeId,
            AssignmentId = _assignmentId,
            MeasurementType = "soil_moisture",
            RawValue = 45.0,
            Value = 45.0,
            Unit = "%",
            Timestamp = DateTime.UtcNow,
            IsSyncedToCloud = false
        };

        // Act
        var result = reading.ToDto();

        // Assert
        result.MeasurementType.Should().Be("soil_moisture");
    }

    #endregion

    #region ToEntity Tests

    [Fact]
    public void ToEntity_WithValidDto_ReturnsCorrectEntity()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var dto = new CreateReadingDto(
            NodeId: "node-01",
            EndpointId: 1,
            MeasurementType: "temperature",
            RawValue: 21.5,
            Timestamp: timestamp
        );
        var calibratedValue = 22.0;  // After calibration

        // Act
        var result = dto.ToEntity(_tenantId, _nodeId, _assignmentId, "°C", calibratedValue);

        // Assert
        result.TenantId.Should().Be(_tenantId);
        result.NodeId.Should().Be(_nodeId);
        result.AssignmentId.Should().Be(_assignmentId);
        result.MeasurementType.Should().Be("temperature");
        result.RawValue.Should().Be(21.5);
        result.Value.Should().Be(22.0);
        result.Unit.Should().Be("°C");
        result.Timestamp.Should().Be(timestamp);
        result.IsSyncedToCloud.Should().BeFalse();
    }

    [Fact]
    public void ToEntity_NormalizesTypeToLowercase()
    {
        // Arrange
        var dto = new CreateReadingDto(
            NodeId: "node-01",
            EndpointId: 1,
            MeasurementType: "TEMPERATURE",
            RawValue: 21.5
        );

        // Act
        var result = dto.ToEntity(_tenantId, _nodeId, _assignmentId, "°C", 21.5);

        // Assert
        result.MeasurementType.Should().Be("temperature");
    }

    [Fact]
    public void ToEntity_WithoutTimestamp_UsesCurrentUtcTime()
    {
        // Arrange
        var beforeTest = DateTime.UtcNow;
        var dto = new CreateReadingDto(
            NodeId: "node-01",
            EndpointId: 1,
            MeasurementType: "temperature",
            RawValue: 21.5,
            Timestamp: null
        );

        // Act
        var result = dto.ToEntity(_tenantId, _nodeId, _assignmentId, "°C", 21.5);
        var afterTest = DateTime.UtcNow;

        // Assert
        result.Timestamp.Should().BeOnOrAfter(beforeTest);
        result.Timestamp.Should().BeOnOrBefore(afterTest);
    }

    [Fact]
    public void ToEntity_SetsIsSyncedToCloudFalse()
    {
        // Arrange
        var dto = new CreateReadingDto(
            NodeId: "node-01",
            EndpointId: 1,
            MeasurementType: "temperature",
            RawValue: 21.5
        );

        // Act
        var result = dto.ToEntity(_tenantId, _nodeId, _assignmentId, "°C", 21.5);

        // Assert
        result.IsSyncedToCloud.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    [InlineData(100)]
    [InlineData(21.5)]
    [InlineData(-40.0)]
    public void ToEntity_PreservesRawValue(double value)
    {
        // Arrange
        var dto = new CreateReadingDto(
            NodeId: "node-01",
            EndpointId: 1,
            MeasurementType: "temperature",
            RawValue: value
        );

        // Act
        var result = dto.ToEntity(_tenantId, _nodeId, _assignmentId, "°C", value);

        // Assert
        result.RawValue.Should().Be(value);
    }

    [Fact]
    public void ToEntity_WithCalibration_StoresBothValues()
    {
        // Arrange
        var rawValue = 20.0;
        var calibratedValue = 21.5;  // (20.0 * 1.05) + 0.5 = 21.5
        var dto = new CreateReadingDto(
            NodeId: "node-01",
            EndpointId: 1,
            MeasurementType: "temperature",
            RawValue: rawValue
        );

        // Act
        var result = dto.ToEntity(_tenantId, _nodeId, _assignmentId, "°C", calibratedValue);

        // Assert
        result.RawValue.Should().Be(20.0);
        result.Value.Should().Be(21.5);
    }

    [Fact]
    public void ToEntity_PreservesUnit()
    {
        // Arrange
        var dto = new CreateReadingDto(
            NodeId: "node-01",
            EndpointId: 1,
            MeasurementType: "humidity",
            RawValue: 65.0
        );

        // Act
        var result = dto.ToEntity(_tenantId, _nodeId, _assignmentId, "%", 65.0);

        // Assert
        result.Unit.Should().Be("%");
    }

    #endregion

    #region ToDtos Tests

    [Fact]
    public void ToDtos_WithMultipleReadings_ConvertsAll()
    {
        // Arrange
        var readings = new List<Reading>
        {
            new()
            {
                Id = 1,
                TenantId = _tenantId,
                NodeId = _nodeId,
                AssignmentId = _assignmentId,
                MeasurementType = "temperature",
                RawValue = 21.5,
                Value = 21.5,
                Unit = "°C",
                Timestamp = DateTime.UtcNow,
                Node = new Node { Id = _nodeId, Name = "Node 1" }
            },
            new()
            {
                Id = 2,
                TenantId = _tenantId,
                NodeId = _nodeId,
                AssignmentId = _assignmentId,
                MeasurementType = "humidity",
                RawValue = 65.0,
                Value = 65.0,
                Unit = "%",
                Timestamp = DateTime.UtcNow,
                Node = new Node { Id = _nodeId, Name = "Node 1" }
            }
        };

        // Act
        var result = readings.ToDtos().ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].MeasurementType.Should().Be("temperature");
        result[0].RawValue.Should().Be(21.5);
        result[0].Value.Should().Be(21.5);
        result[0].Unit.Should().Be("°C");
        result[1].MeasurementType.Should().Be("humidity");
        result[1].RawValue.Should().Be(65.0);
        result[1].Value.Should().Be(65.0);
        result[1].Unit.Should().Be("%");
    }

    [Fact]
    public void ToDtos_WithEmptyList_ReturnsEmptyEnumerable()
    {
        // Arrange
        var readings = new List<Reading>();

        // Act
        var result = readings.ToDtos();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ToDtos_PreservesOrder()
    {
        // Arrange
        var readings = new List<Reading>
        {
            new() { Id = 3, TenantId = _tenantId, NodeId = _nodeId, AssignmentId = _assignmentId, MeasurementType = "c", RawValue = 3, Value = 3, Unit = "u", Timestamp = DateTime.UtcNow },
            new() { Id = 1, TenantId = _tenantId, NodeId = _nodeId, AssignmentId = _assignmentId, MeasurementType = "a", RawValue = 1, Value = 1, Unit = "u", Timestamp = DateTime.UtcNow },
            new() { Id = 2, TenantId = _tenantId, NodeId = _nodeId, AssignmentId = _assignmentId, MeasurementType = "b", RawValue = 2, Value = 2, Unit = "u", Timestamp = DateTime.UtcNow }
        };

        // Act
        var result = readings.ToDtos().ToList();

        // Assert
        result[0].Id.Should().Be(3);
        result[1].Id.Should().Be(1);
        result[2].Id.Should().Be(2);
    }

    [Fact]
    public void ToDtos_WithMixedCalibrations_PreservesBothValues()
    {
        // Arrange
        var readings = new List<Reading>
        {
            new()
            {
                Id = 1,
                TenantId = _tenantId,
                NodeId = _nodeId,
                AssignmentId = _assignmentId,
                MeasurementType = "temperature",
                RawValue = 20.0,
                Value = 21.5,  // Calibrated
                Unit = "°C",
                Timestamp = DateTime.UtcNow
            },
            new()
            {
                Id = 2,
                TenantId = _tenantId,
                NodeId = _nodeId,
                AssignmentId = _assignmentId,
                MeasurementType = "humidity",
                RawValue = 65.0,
                Value = 65.0,  // No calibration (1:1)
                Unit = "%",
                Timestamp = DateTime.UtcNow
            }
        };

        // Act
        var result = readings.ToDtos().ToList();

        // Assert
        result[0].RawValue.Should().Be(20.0);
        result[0].Value.Should().Be(21.5);
        result[1].RawValue.Should().Be(65.0);
        result[1].Value.Should().Be(65.0);
    }

    #endregion
}
