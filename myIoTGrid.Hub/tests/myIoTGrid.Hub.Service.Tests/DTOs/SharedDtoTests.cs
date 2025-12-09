using FluentAssertions;

namespace myIoTGrid.Hub.Service.Tests.DTOs;

/// <summary>
/// Tests for Shared DTOs to ensure 100% coverage.
/// Tests record properties, equality, and deconstruction.
/// </summary>
public class SharedDtoTests
{
    #region HubProvisioningSettingsDto Tests

    [Fact]
    public void HubProvisioningSettingsDto_Constructor_SetsAllProperties()
    {
        // Act
        var dto = new HubProvisioningSettingsDto(
            DefaultWifiSsid: "TestSSID",
            DefaultWifiPassword: "TestPassword",
            ApiUrl: "http://localhost:5000",
            ApiPort: 5000);

        // Assert
        dto.DefaultWifiSsid.Should().Be("TestSSID");
        dto.DefaultWifiPassword.Should().Be("TestPassword");
        dto.ApiUrl.Should().Be("http://localhost:5000");
        dto.ApiPort.Should().Be(5000);
    }

    [Fact]
    public void HubProvisioningSettingsDto_WithNullValues_AllowsNulls()
    {
        // Act
        var dto = new HubProvisioningSettingsDto(null, null, null, 8080);

        // Assert
        dto.DefaultWifiSsid.Should().BeNull();
        dto.DefaultWifiPassword.Should().BeNull();
        dto.ApiUrl.Should().BeNull();
        dto.ApiPort.Should().Be(8080);
    }

    [Fact]
    public void HubProvisioningSettingsDto_Equality_WorksCorrectly()
    {
        // Arrange
        var dto1 = new HubProvisioningSettingsDto("SSID", "Pass", "http://api", 5000);
        var dto2 = new HubProvisioningSettingsDto("SSID", "Pass", "http://api", 5000);

        // Assert
        dto1.Should().Be(dto2);
        (dto1 == dto2).Should().BeTrue();
    }

    #endregion

    #region BleProvisioningDataDto Tests

    [Fact]
    public void BleProvisioningDataDto_Constructor_SetsAllProperties()
    {
        // Arrange
        var nodeId = Guid.NewGuid();

        // Act
        var dto = new BleProvisioningDataDto(
            WifiSsid: "MyWifi",
            WifiPassword: "SecurePass123",
            ApiUrl: "https://hub.local:5001",
            NodeId: nodeId,
            NodeName: "Living Room Sensor",
            ApiKey: "api-key-123");

        // Assert
        dto.WifiSsid.Should().Be("MyWifi");
        dto.WifiPassword.Should().Be("SecurePass123");
        dto.ApiUrl.Should().Be("https://hub.local:5001");
        dto.NodeId.Should().Be(nodeId);
        dto.NodeName.Should().Be("Living Room Sensor");
        dto.ApiKey.Should().Be("api-key-123");
    }

    [Fact]
    public void BleProvisioningDataDto_Deconstruction_WorksCorrectly()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var dto = new BleProvisioningDataDto("SSID", "Pass", "http://api", nodeId, "Node", "Key");

        // Act
        var (ssid, password, url, id, name, key) = dto;

        // Assert
        ssid.Should().Be("SSID");
        password.Should().Be("Pass");
        url.Should().Be("http://api");
        id.Should().Be(nodeId);
        name.Should().Be("Node");
        key.Should().Be("Key");
    }

    #endregion

    #region NodeGpsStatusDto Tests

    [Fact]
    public void NodeGpsStatusDto_Constructor_SetsAllProperties()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var lastUpdate = DateTime.UtcNow;

        // Act
        var dto = new NodeGpsStatusDto(
            NodeId: nodeId,
            NodeName: "Mobile Sensor",
            HasGps: true,
            Satellites: 8,
            FixType: 3,
            FixTypeText: "3D Fix",
            Hdop: 1.2,
            HdopQuality: "Excellent",
            Latitude: 50.9375,
            Longitude: 6.9603,
            Altitude: 56.7,
            Speed: 0.5,
            LastUpdate: lastUpdate);

        // Assert
        dto.NodeId.Should().Be(nodeId);
        dto.NodeName.Should().Be("Mobile Sensor");
        dto.HasGps.Should().BeTrue();
        dto.Satellites.Should().Be(8);
        dto.FixType.Should().Be(3);
        dto.FixTypeText.Should().Be("3D Fix");
        dto.Hdop.Should().Be(1.2);
        dto.HdopQuality.Should().Be("Excellent");
        dto.Latitude.Should().Be(50.9375);
        dto.Longitude.Should().Be(6.9603);
        dto.Altitude.Should().Be(56.7);
        dto.Speed.Should().Be(0.5);
        dto.LastUpdate.Should().Be(lastUpdate);
    }

    [Fact]
    public void NodeGpsStatusDto_WithNoGps_AllowsNullCoordinates()
    {
        // Act
        var dto = new NodeGpsStatusDto(
            Guid.NewGuid(), "Sensor", false, 0, 0, "No Fix", 99.99, "Poor",
            null, null, null, null, DateTime.UtcNow);

        // Assert
        dto.HasGps.Should().BeFalse();
        dto.Latitude.Should().BeNull();
        dto.Longitude.Should().BeNull();
        dto.Altitude.Should().BeNull();
        dto.Speed.Should().BeNull();
    }

    #endregion

    #region GpsPositionDto Tests

    [Fact]
    public void GpsPositionDto_Constructor_SetsAllProperties()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act
        var dto = new GpsPositionDto(
            Latitude: 50.9375,
            Longitude: 6.9603,
            Altitude: 56.7,
            Speed: 12.5,
            Timestamp: timestamp);

        // Assert
        dto.Latitude.Should().Be(50.9375);
        dto.Longitude.Should().Be(6.9603);
        dto.Altitude.Should().Be(56.7);
        dto.Speed.Should().Be(12.5);
        dto.Timestamp.Should().Be(timestamp);
    }

    [Fact]
    public void GpsPositionDto_WithNullOptionalValues_AllowsNulls()
    {
        // Act
        var dto = new GpsPositionDto(50.0, 6.0, null, null, DateTime.UtcNow);

        // Assert
        dto.Altitude.Should().BeNull();
        dto.Speed.Should().BeNull();
    }

    [Fact]
    public void GpsPositionDto_Equality_WorksCorrectly()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var dto1 = new GpsPositionDto(50.0, 6.0, 100.0, 5.0, timestamp);
        var dto2 = new GpsPositionDto(50.0, 6.0, 100.0, 5.0, timestamp);

        // Assert
        dto1.Should().Be(dto2);
    }

    #endregion

    #region MinMaxDto Tests

    [Fact]
    public void MinMaxDto_Constructor_SetsAllProperties()
    {
        // Arrange
        var minTime = DateTime.UtcNow.AddHours(-2);
        var maxTime = DateTime.UtcNow.AddHours(-1);

        // Act
        var dto = new MinMaxDto(18.5, minTime, 26.3, maxTime);

        // Assert
        dto.MinValue.Should().Be(18.5);
        dto.MinTimestamp.Should().Be(minTime);
        dto.MaxValue.Should().Be(26.3);
        dto.MaxTimestamp.Should().Be(maxTime);
    }

    [Fact]
    public void MinMaxDto_Deconstruction_WorksCorrectly()
    {
        // Arrange
        var minTime = DateTime.UtcNow.AddHours(-2);
        var maxTime = DateTime.UtcNow.AddHours(-1);
        var dto = new MinMaxDto(10.0, minTime, 30.0, maxTime);

        // Act
        var (minVal, minT, maxVal, maxT) = dto;

        // Assert
        minVal.Should().Be(10.0);
        minT.Should().Be(minTime);
        maxVal.Should().Be(30.0);
        maxT.Should().Be(maxTime);
    }

    #endregion

    #region SparklinePointDto Tests

    [Fact]
    public void SparklinePointDto_Constructor_SetsAllProperties()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act
        var dto = new SparklinePointDto(timestamp, 21.5);

        // Assert
        dto.Timestamp.Should().Be(timestamp);
        dto.Value.Should().Be(21.5);
    }

    [Fact]
    public void SparklinePointDto_Deconstruction_WorksCorrectly()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var dto = new SparklinePointDto(timestamp, 25.0);

        // Act
        var (ts, value) = dto;

        // Assert
        ts.Should().Be(timestamp);
        value.Should().Be(25.0);
    }

    #endregion

    #region ChartPointDto Tests

    [Fact]
    public void ChartPointDto_Constructor_SetsAllProperties()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act
        var dto = new ChartPointDto(timestamp, 21.5, 20.0, 23.0);

        // Assert
        dto.Timestamp.Should().Be(timestamp);
        dto.Value.Should().Be(21.5);
        dto.Min.Should().Be(20.0);
        dto.Max.Should().Be(23.0);
    }

    [Fact]
    public void ChartPointDto_WithNullMinMax_AllowsNulls()
    {
        // Act
        var dto = new ChartPointDto(DateTime.UtcNow, 21.5, null, null);

        // Assert
        dto.Min.Should().BeNull();
        dto.Max.Should().BeNull();
    }

    [Fact]
    public void ChartPointDto_Deconstruction_WorksCorrectly()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var dto = new ChartPointDto(timestamp, 21.5, 20.0, 23.0);

        // Act
        var (ts, value, min, max) = dto;

        // Assert
        ts.Should().Be(timestamp);
        value.Should().Be(21.5);
        min.Should().Be(20.0);
        max.Should().Be(23.0);
    }

    #endregion

    #region TrendDto Tests

    [Fact]
    public void TrendDto_Constructor_SetsAllProperties()
    {
        // Act
        var dto = new TrendDto(2.5, 10.5, "up");

        // Assert
        dto.Change.Should().Be(2.5);
        dto.ChangePercent.Should().Be(10.5);
        dto.Direction.Should().Be("up");
    }

    [Fact]
    public void TrendDto_Deconstruction_WorksCorrectly()
    {
        // Arrange
        var dto = new TrendDto(-1.5, -5.0, "down");

        // Act
        var (change, percent, direction) = dto;

        // Assert
        change.Should().Be(-1.5);
        percent.Should().Be(-5.0);
        direction.Should().Be("down");
    }

    [Theory]
    [InlineData(0.0, 0.0, "stable")]
    [InlineData(5.0, 20.0, "up")]
    [InlineData(-3.0, -10.0, "down")]
    public void TrendDto_VariousDirections_AllValid(double change, double percent, string direction)
    {
        // Act
        var dto = new TrendDto(change, percent, direction);

        // Assert
        dto.Direction.Should().Be(direction);
    }

    #endregion

    #region ChartStatsDto Tests

    [Fact]
    public void ChartStatsDto_Constructor_SetsAllProperties()
    {
        // Arrange
        var minTime = DateTime.UtcNow.AddHours(-5);
        var maxTime = DateTime.UtcNow.AddHours(-2);

        // Act
        var dto = new ChartStatsDto(15.0, minTime, 28.0, maxTime, 21.5);

        // Assert
        dto.MinValue.Should().Be(15.0);
        dto.MinTimestamp.Should().Be(minTime);
        dto.MaxValue.Should().Be(28.0);
        dto.MaxTimestamp.Should().Be(maxTime);
        dto.AvgValue.Should().Be(21.5);
    }

    [Fact]
    public void ChartStatsDto_Deconstruction_WorksCorrectly()
    {
        // Arrange
        var minTime = DateTime.UtcNow.AddHours(-5);
        var maxTime = DateTime.UtcNow.AddHours(-2);
        var dto = new ChartStatsDto(15.0, minTime, 28.0, maxTime, 21.5);

        // Act
        var (minVal, minT, maxVal, maxT, avg) = dto;

        // Assert
        minVal.Should().Be(15.0);
        minT.Should().Be(minTime);
        maxVal.Should().Be(28.0);
        maxT.Should().Be(maxTime);
        avg.Should().Be(21.5);
    }

    #endregion

    #region ReadingListItemDto Tests

    [Fact]
    public void ReadingListItemDto_Constructor_SetsAllProperties()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act
        var dto = new ReadingListItemDto(123, timestamp, 21.5, "°C", "up");

        // Assert
        dto.Id.Should().Be(123);
        dto.Timestamp.Should().Be(timestamp);
        dto.Value.Should().Be(21.5);
        dto.Unit.Should().Be("°C");
        dto.TrendDirection.Should().Be("up");
    }

    [Fact]
    public void ReadingListItemDto_WithNullTrend_AllowsNull()
    {
        // Act
        var dto = new ReadingListItemDto(1, DateTime.UtcNow, 20.0, "°C", null);

        // Assert
        dto.TrendDirection.Should().BeNull();
    }

    [Fact]
    public void ReadingListItemDto_Deconstruction_WorksCorrectly()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var dto = new ReadingListItemDto(456, timestamp, 25.0, "%", "stable");

        // Act
        var (id, ts, value, unit, trend) = dto;

        // Assert
        id.Should().Be(456);
        ts.Should().Be(timestamp);
        value.Should().Be(25.0);
        unit.Should().Be("%");
        trend.Should().Be("stable");
    }

    #endregion

    #region ReadingsListDto Tests

    [Fact]
    public void ReadingsListDto_Constructor_SetsAllProperties()
    {
        // Arrange
        var items = new List<ReadingListItemDto>
        {
            new(1, DateTime.UtcNow, 21.0, "°C", "up"),
            new(2, DateTime.UtcNow.AddMinutes(-5), 20.5, "°C", "stable")
        };

        // Act
        var dto = new ReadingsListDto(items, 100, 1, 20, 5);

        // Assert
        dto.Items.Should().HaveCount(2);
        dto.TotalCount.Should().Be(100);
        dto.Page.Should().Be(1);
        dto.PageSize.Should().Be(20);
        dto.TotalPages.Should().Be(5);
    }

    [Fact]
    public void ReadingsListDto_Deconstruction_WorksCorrectly()
    {
        // Arrange
        var items = new List<ReadingListItemDto>();
        var dto = new ReadingsListDto(items, 50, 2, 10, 5);

        // Act
        var (resultItems, total, page, pageSize, totalPages) = dto;

        // Assert
        resultItems.Should().BeSameAs(items);
        total.Should().Be(50);
        page.Should().Be(2);
        pageSize.Should().Be(10);
        totalPages.Should().Be(5);
    }

    #endregion

    #region DebugLogBatchDto Tests

    [Fact]
    public void DebugLogBatchDto_DefaultConstructor_HasEmptyNodeId()
    {
        // Act
        var dto = new DebugLogBatchDto();

        // Assert
        dto.NodeId.Should().BeEmpty();
        dto.Logs.Should().BeEmpty();
    }

    [Fact]
    public void DebugLogBatchDto_PropertySetter_SetsValues()
    {
        // Arrange
        var logs = new List<CreateNodeDebugLogDto>
        {
            new() { NodeTimestamp = 1000, Level = DebugLevelDto.Normal, Category = LogCategoryDto.System, Message = "Test" }
        };

        // Act
        var dto = new DebugLogBatchDto { NodeId = "AA:BB:CC:DD:EE:FF", Logs = logs };

        // Assert
        dto.NodeId.Should().Be("AA:BB:CC:DD:EE:FF");
        dto.Logs.Should().HaveCount(1);
    }

    #endregion

    #region DebugLogCleanupResultDto Tests

    [Fact]
    public void DebugLogCleanupResultDto_Constructor_SetsAllProperties()
    {
        // Arrange
        var cleanupBefore = DateTime.UtcNow.AddDays(-7);

        // Act
        var dto = new DebugLogCleanupResultDto(150, cleanupBefore);

        // Assert
        dto.DeletedCount.Should().Be(150);
        dto.CleanupBefore.Should().Be(cleanupBefore);
    }

    [Fact]
    public void DebugLogCleanupResultDto_Deconstruction_WorksCorrectly()
    {
        // Arrange
        var cleanupBefore = DateTime.UtcNow.AddDays(-14);
        var dto = new DebugLogCleanupResultDto(50, cleanupBefore);

        // Act
        var (deleted, date) = dto;

        // Assert
        deleted.Should().Be(50);
        date.Should().Be(cleanupBefore);
    }

    #endregion

    #region DeleteReadingsResultDto Tests

    [Fact]
    public void DeleteReadingsResultDto_Constructor_SetsAllProperties()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var from = DateTime.UtcNow.AddDays(-30);
        var to = DateTime.UtcNow;
        var assignmentId = Guid.NewGuid();

        // Act
        var dto = new DeleteReadingsResultDto(250, nodeId, from, to, assignmentId, "temperature");

        // Assert
        dto.DeletedCount.Should().Be(250);
        dto.NodeId.Should().Be(nodeId);
        dto.From.Should().Be(from);
        dto.To.Should().Be(to);
        dto.AssignmentId.Should().Be(assignmentId);
        dto.MeasurementType.Should().Be("temperature");
    }

    [Fact]
    public void DeleteReadingsResultDto_WithNullOptionals_AllowsNulls()
    {
        // Act
        var dto = new DeleteReadingsResultDto(100, Guid.NewGuid(), DateTime.UtcNow.AddDays(-7), DateTime.UtcNow, null, null);

        // Assert
        dto.AssignmentId.Should().BeNull();
        dto.MeasurementType.Should().BeNull();
    }

    [Fact]
    public void DeleteReadingsResultDto_Deconstruction_WorksCorrectly()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;
        var dto = new DeleteReadingsResultDto(75, nodeId, from, to, null, null);

        // Act
        var (deleted, node, f, t, assignment, type) = dto;

        // Assert
        deleted.Should().Be(75);
        node.Should().Be(nodeId);
        f.Should().Be(from);
        t.Should().Be(to);
        assignment.Should().BeNull();
        type.Should().BeNull();
    }

    #endregion

    #region BatchReadingsResultDto Tests

    [Fact]
    public void BatchReadingsResultDto_Constructor_SetsAllProperties()
    {
        // Arrange
        var processedAt = DateTime.UtcNow;
        var errors = new List<string> { "Error 1", "Error 2" };

        // Act
        var dto = new BatchReadingsResultDto(8, 2, 10, "node-01", processedAt, errors);

        // Assert
        dto.SuccessCount.Should().Be(8);
        dto.FailedCount.Should().Be(2);
        dto.TotalCount.Should().Be(10);
        dto.NodeId.Should().Be("node-01");
        dto.ProcessedAt.Should().Be(processedAt);
        dto.Errors.Should().HaveCount(2);
    }

    [Fact]
    public void BatchReadingsResultDto_WithNullErrors_AllowsNull()
    {
        // Act
        var dto = new BatchReadingsResultDto(5, 0, 5, "node-01", DateTime.UtcNow, null);

        // Assert
        dto.Errors.Should().BeNull();
    }

    [Fact]
    public void BatchReadingsResultDto_Deconstruction_WorksCorrectly()
    {
        // Arrange
        var processedAt = DateTime.UtcNow;
        var dto = new BatchReadingsResultDto(10, 0, 10, "node-02", processedAt, null);

        // Act
        var (success, failed, total, nodeId, processed, errors) = dto;

        // Assert
        success.Should().Be(10);
        failed.Should().Be(0);
        total.Should().Be(10);
        nodeId.Should().Be("node-02");
        processed.Should().Be(processedAt);
        errors.Should().BeNull();
    }

    #endregion

    #region CreateNodeDebugLogDto Tests

    [Fact]
    public void CreateNodeDebugLogDto_DefaultConstructor_HasDefaultValues()
    {
        // Act
        var dto = new CreateNodeDebugLogDto();

        // Assert
        dto.NodeTimestamp.Should().Be(0);
        dto.Level.Should().Be(default(DebugLevelDto));
        dto.Category.Should().Be(default(LogCategoryDto));
        dto.Message.Should().BeEmpty();
        dto.StackTrace.Should().BeNull();
    }

    [Fact]
    public void CreateNodeDebugLogDto_PropertySetter_SetsAllValues()
    {
        // Act
        var dto = new CreateNodeDebugLogDto
        {
            NodeTimestamp = 1234567890,
            Level = DebugLevelDto.Debug,
            Category = LogCategoryDto.Sensor,
            Message = "Test message",
            StackTrace = "Error stack trace"
        };

        // Assert
        dto.NodeTimestamp.Should().Be(1234567890);
        dto.Level.Should().Be(DebugLevelDto.Debug);
        dto.Category.Should().Be(LogCategoryDto.Sensor);
        dto.Message.Should().Be("Test message");
        dto.StackTrace.Should().Be("Error stack trace");
    }

    [Fact]
    public void CreateNodeDebugLogDto_AlternativeTimestamp_SetsNodeTimestamp()
    {
        // Act
        var dto = new CreateNodeDebugLogDto { Timestamp = 9876543210 };

        // Assert
        dto.NodeTimestamp.Should().Be(9876543210);
        dto.Timestamp.Should().BeNull(); // Getter always returns null
    }

    [Fact]
    public void CreateNodeDebugLogDto_NullTimestamp_DoesNotChangeNodeTimestamp()
    {
        // Arrange
        var dto = new CreateNodeDebugLogDto { NodeTimestamp = 1000 };

        // Act
        dto.Timestamp = null;

        // Assert
        dto.NodeTimestamp.Should().Be(1000);
    }

    #endregion

    #region DeleteReadingsRangeDto Tests

    [Fact]
    public void DeleteReadingsRangeDto_Constructor_SetsAllProperties()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var from = DateTime.UtcNow.AddDays(-30);
        var to = DateTime.UtcNow;

        // Act
        var dto = new DeleteReadingsRangeDto(nodeId, from, to);

        // Assert
        dto.NodeId.Should().Be(nodeId);
        dto.From.Should().Be(from);
        dto.To.Should().Be(to);
    }

    [Fact]
    public void DeleteReadingsRangeDto_WithOptionalParameters_SetsDefaults()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;
        var assignmentId = Guid.NewGuid();

        // Act
        var dto = new DeleteReadingsRangeDto(nodeId, from, to, assignmentId, "humidity");

        // Assert
        dto.AssignmentId.Should().Be(assignmentId);
        dto.MeasurementType.Should().Be("humidity");
    }

    [Fact]
    public void DeleteReadingsRangeDto_Deconstruction_WorksCorrectly()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var from = DateTime.UtcNow.AddDays(-14);
        var to = DateTime.UtcNow;
        var dto = new DeleteReadingsRangeDto(nodeId, from, to);

        // Act
        var (node, f, t, _, _) = dto;

        // Assert
        node.Should().Be(nodeId);
        f.Should().Be(from);
        t.Should().Be(to);
    }

    #endregion

    #region Alternative Property Setters Tests

    [Fact]
    public void DebugLogBatchDto_SerialNumberSetter_SetsNodeId()
    {
        // Arrange & Act
        var dto = new DebugLogBatchDto();
        // Use reflection to set SerialNumber since it's a write-only property
        var property = typeof(DebugLogBatchDto).GetProperty("SerialNumber");
        property!.SetValue(dto, "AA:BB:CC:DD:EE:FF");

        // Assert
        dto.NodeId.Should().Be("AA:BB:CC:DD:EE:FF");
    }

    [Fact]
    public void DebugLogBatchDto_SerialNumberGetter_ReturnsNull()
    {
        // Arrange
        var dto = new DebugLogBatchDto { NodeId = "AA:BB:CC:DD:EE:FF" };

        // Act
        var property = typeof(DebugLogBatchDto).GetProperty("SerialNumber");
        var serialNumber = property!.GetValue(dto);

        // Assert
        serialNumber.Should().BeNull();
    }

    [Fact]
    public void DebugLogBatchDto_SerialNumberSetterWithEmptyValue_DoesNotSetNodeId()
    {
        // Arrange
        var dto = new DebugLogBatchDto { NodeId = "original-id" };

        // Act
        var property = typeof(DebugLogBatchDto).GetProperty("SerialNumber");
        property!.SetValue(dto, "");

        // Assert
        dto.NodeId.Should().Be("original-id");
    }

    [Fact]
    public void DebugLogBatchDto_SerialNumberSetterWithNull_DoesNotSetNodeId()
    {
        // Arrange
        var dto = new DebugLogBatchDto { NodeId = "original-id" };

        // Act
        var property = typeof(DebugLogBatchDto).GetProperty("SerialNumber");
        property!.SetValue(dto, null);

        // Assert
        dto.NodeId.Should().Be("original-id");
    }

    [Fact]
    public void CreateNodeDebugLogDto_TimestampSetter_SetsNodeTimestamp()
    {
        // Arrange & Act
        var dto = new CreateNodeDebugLogDto();
        var property = typeof(CreateNodeDebugLogDto).GetProperty("Timestamp");
        property!.SetValue(dto, 1234567890L);

        // Assert
        dto.NodeTimestamp.Should().Be(1234567890);
    }

    [Fact]
    public void CreateNodeDebugLogDto_TimestampGetter_ReturnsNull()
    {
        // Arrange
        var dto = new CreateNodeDebugLogDto { NodeTimestamp = 1234567890 };

        // Act
        var property = typeof(CreateNodeDebugLogDto).GetProperty("Timestamp");
        var timestamp = property!.GetValue(dto);

        // Assert
        timestamp.Should().BeNull();
    }

    [Fact]
    public void CreateNodeDebugLogDto_TimestampSetterWithNull_DoesNotSetNodeTimestamp()
    {
        // Arrange
        var dto = new CreateNodeDebugLogDto { NodeTimestamp = 999 };

        // Act
        var property = typeof(CreateNodeDebugLogDto).GetProperty("Timestamp");
        property!.SetValue(dto, null);

        // Assert
        dto.NodeTimestamp.Should().Be(999);
    }

    #endregion

    #region CreateSyncedNodeDto Tests

    [Fact]
    public void CreateSyncedNodeDto_Constructor_WithOptionalParameters()
    {
        // Arrange
        var cloudNodeId = Guid.NewGuid();
        var location = new LocationDto("Test Location", 52.52, 13.405);

        // Act
        var dto = new CreateSyncedNodeDto(
            cloudNodeId,
            "node-001",
            "Test Node",
            SyncedNodeSourceDto.OtherHub,
            "Hub-123",
            location,
            true
        );

        // Assert
        dto.CloudNodeId.Should().Be(cloudNodeId);
        dto.NodeId.Should().Be("node-001");
        dto.Name.Should().Be("Test Node");
        dto.Source.Should().Be(SyncedNodeSourceDto.OtherHub);
        dto.SourceDetails.Should().Be("Hub-123");
        dto.Location.Should().Be(location);
        dto.IsOnline.Should().BeTrue();
    }

    [Fact]
    public void CreateSyncedNodeDto_Constructor_WithDefaultParameters()
    {
        // Arrange
        var cloudNodeId = Guid.NewGuid();

        // Act
        var dto = new CreateSyncedNodeDto(
            cloudNodeId,
            "node-002",
            "Default Node",
            SyncedNodeSourceDto.Virtual
        );

        // Assert
        dto.CloudNodeId.Should().Be(cloudNodeId);
        dto.SourceDetails.Should().BeNull();
        dto.Location.Should().BeNull();
        dto.IsOnline.Should().BeFalse();
    }

    #endregion

    #region AlertDto Additional Tests

    [Fact]
    public void AlertDto_WithAllNullableProperties_CreatesValidDto()
    {
        // Arrange & Act
        var dto = new AlertDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null, // HubId
            null, // HubName
            null, // NodeId
            null, // NodeName
            Guid.NewGuid(),
            "test_alert",
            "Test Alert",
            AlertLevelDto.Warning,
            "Test message",
            null, // Recommendation
            AlertSourceDto.Local,
            DateTime.UtcNow,
            null, // ExpiresAt
            null, // AcknowledgedAt
            true
        );

        // Assert
        dto.HubId.Should().BeNull();
        dto.HubName.Should().BeNull();
        dto.NodeId.Should().BeNull();
        dto.NodeName.Should().BeNull();
        dto.Recommendation.Should().BeNull();
        dto.ExpiresAt.Should().BeNull();
        dto.AcknowledgedAt.Should().BeNull();
    }

    #endregion

    #region ChartDataDto Additional Tests

    [Fact]
    public void ChartDataDto_WithAllNullableProperties_CreatesValidDto()
    {
        // Arrange
        var stats = new ChartStatsDto(10.0, DateTime.UtcNow, 30.0, DateTime.UtcNow, 20.0);
        var trend = new TrendDto(2.5, 12.5, "up");
        var dataPoints = new List<ChartPointDto>();

        // Act
        var dto = new ChartDataDto(
            Guid.NewGuid(),
            "Node 1",
            null, // AssignmentId
            null, // SensorId
            "DHT22",
            "temperature",
            "Wohnzimmer",
            "°C",
            "#FF5722",
            21.5,
            DateTime.UtcNow,
            stats,
            trend,
            dataPoints
        );

        // Assert
        dto.AssignmentId.Should().BeNull();
        dto.SensorId.Should().BeNull();
    }

    #endregion

    #region SensorWidgetDto Additional Tests

    [Fact]
    public void SensorWidgetDto_WithAllNullableProperties_CreatesValidDto()
    {
        // Arrange
        var minMax = new MinMaxDto(15.0, DateTime.UtcNow.AddHours(-12), 25.0, DateTime.UtcNow.AddHours(-6));
        var dataPoints = new List<SparklinePointDto>();

        // Act
        var dto = new SensorWidgetDto(
            "widget-1",
            Guid.NewGuid(),
            "Node 1",
            null, // AssignmentId
            null, // SensorId
            "temperature",
            "DHT22",
            "Wohnzimmer",
            "Wohnzimmer Temperature",
            "°C",
            "#FF5722",
            21.5,
            DateTime.UtcNow,
            minMax,
            dataPoints
        );

        // Assert
        dto.AssignmentId.Should().BeNull();
        dto.SensorId.Should().BeNull();
        dto.WidgetId.Should().Be("widget-1");
    }

    #endregion

    #region AlertTypeDto Tests

    [Fact]
    public void AlertTypeDto_WithAllNullableProperties_CreatesValidDto()
    {
        // Act
        var dto = new AlertTypeDto(
            Guid.NewGuid(),
            "test_alert",
            "Test Alert",
            null, // Description
            AlertLevelDto.Info,
            null, // IconName
            true,
            DateTime.UtcNow
        );

        // Assert
        dto.Description.Should().BeNull();
        dto.IconName.Should().BeNull();
        dto.IsGlobal.Should().BeTrue();
    }

    #endregion

    #region RegisterNodeDto Tests

    [Fact]
    public void RegisterNodeDto_Constructor_WithAllOptionalParameters()
    {
        // Arrange
        var location = new LocationDto("Test", 52.52, 13.405);
        var capabilities = new List<string> { "temperature", "humidity" };

        // Act
        var dto = new RegisterNodeDto(
            "ESP32-001",
            "1.0.0",
            "ESP32-DevKit",
            capabilities,
            "Living Room Sensor",
            location
        );

        // Assert
        dto.SerialNumber.Should().Be("ESP32-001");
        dto.FirmwareVersion.Should().Be("1.0.0");
        dto.HardwareType.Should().Be("ESP32-DevKit");
        dto.Capabilities.Should().HaveCount(2);
        dto.Name.Should().Be("Living Room Sensor");
        dto.Location.Should().Be(location);
    }

    [Fact]
    public void RegisterNodeDto_Constructor_WithDefaultOptionalParameters()
    {
        // Act
        var dto = new RegisterNodeDto("ESP32-002");

        // Assert
        dto.SerialNumber.Should().Be("ESP32-002");
        dto.FirmwareVersion.Should().BeNull();
        dto.HardwareType.Should().BeNull();
        dto.Capabilities.Should().BeNull();
        dto.Name.Should().BeNull();
        dto.Location.Should().BeNull();
    }

    #endregion

    #region SerialOutputBatchDto Tests

    [Fact]
    public void SerialOutputBatchDto_DefaultConstructor_HasDefaults()
    {
        // Act
        var dto = new SerialOutputBatchDto();

        // Assert
        dto.SerialNumber.Should().BeEmpty();
        dto.Timestamp.Should().Be(0);
        dto.Lines.Should().BeEmpty();
    }

    [Fact]
    public void SerialOutputBatchDto_PropertySetter_SetsValues()
    {
        // Act
        var dto = new SerialOutputBatchDto
        {
            SerialNumber = "ESP32-12345",
            Timestamp = 1234567890,
            Lines = new List<string> { "Line 1", "Line 2", "Line 3" }
        };

        // Assert
        dto.SerialNumber.Should().Be("ESP32-12345");
        dto.Timestamp.Should().Be(1234567890);
        dto.Lines.Should().HaveCount(3);
    }

    #endregion

    #region SerialOutputLineDto Tests

    [Fact]
    public void SerialOutputLineDto_Constructor_SetsAllProperties()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var receivedAt = DateTime.UtcNow;

        // Act
        var dto = new SerialOutputLineDto(nodeId, "Node 1", "Serial output line", receivedAt);

        // Assert
        dto.NodeId.Should().Be(nodeId);
        dto.NodeName.Should().Be("Node 1");
        dto.Line.Should().Be("Serial output line");
        dto.ReceivedAt.Should().Be(receivedAt);
    }

    #endregion

    #region NodeDebugLogDto Tests

    [Fact]
    public void NodeDebugLogDto_Constructor_SetsAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var nodeId = Guid.NewGuid();
        var receivedAt = DateTime.UtcNow;

        // Act
        var dto = new NodeDebugLogDto(
            id,
            nodeId,
            1234567890,
            receivedAt,
            DebugLevelDto.Debug,
            LogCategoryDto.Sensor,
            "Test debug message",
            "Stack trace here"
        );

        // Assert
        dto.Id.Should().Be(id);
        dto.NodeId.Should().Be(nodeId);
        dto.NodeTimestamp.Should().Be(1234567890);
        dto.ReceivedAt.Should().Be(receivedAt);
        dto.Level.Should().Be(DebugLevelDto.Debug);
        dto.Category.Should().Be(LogCategoryDto.Sensor);
        dto.Message.Should().Be("Test debug message");
        dto.StackTrace.Should().Be("Stack trace here");
    }

    [Fact]
    public void NodeDebugLogDto_WithNullStackTrace_IsValid()
    {
        // Act
        var dto = new NodeDebugLogDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1000,
            DateTime.UtcNow,
            DebugLevelDto.Normal,
            LogCategoryDto.System,
            "Test",
            null
        );

        // Assert
        dto.StackTrace.Should().BeNull();
    }

    #endregion

    #region NodeErrorStatisticsDto Tests

    [Fact]
    public void NodeErrorStatisticsDto_Constructor_SetsAllProperties()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var errorsByCategory = new Dictionary<string, int>
        {
            { "Network", 5 },
            { "Sensor", 3 }
        };
        var lastErrorAt = DateTime.UtcNow.AddMinutes(-30);

        // Act
        var dto = new NodeErrorStatisticsDto(
            nodeId,
            "Test Node",
            100,
            10,
            25,
            65,
            errorsByCategory,
            lastErrorAt,
            "Connection timeout"
        );

        // Assert
        dto.NodeId.Should().Be(nodeId);
        dto.NodeName.Should().Be("Test Node");
        dto.TotalLogs.Should().Be(100);
        dto.ErrorCount.Should().Be(10);
        dto.WarningCount.Should().Be(25);
        dto.InfoCount.Should().Be(65);
        dto.ErrorsByCategory.Should().HaveCount(2);
        dto.LastErrorAt.Should().Be(lastErrorAt);
        dto.LastErrorMessage.Should().Be("Connection timeout");
    }

    [Fact]
    public void NodeErrorStatisticsDto_WithNullOptionalProperties_IsValid()
    {
        // Act
        var dto = new NodeErrorStatisticsDto(
            Guid.NewGuid(),
            "Node",
            50,
            0,
            5,
            45,
            new Dictionary<string, int>(),
            null,
            null
        );

        // Assert
        dto.LastErrorAt.Should().BeNull();
        dto.LastErrorMessage.Should().BeNull();
    }

    #endregion

    #region NodeDto Tests

    [Fact]
    public void NodeDto_Constructor_SetsAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var hubId = Guid.NewGuid();
        var location = new LocationDto("Test", 52.52, 13.405);
        var lastSeen = DateTime.UtcNow;
        var createdAt = DateTime.UtcNow.AddDays(-1);
        var lastSyncAt = DateTime.UtcNow.AddHours(-1);
        var lastDebugChange = DateTime.UtcNow.AddMinutes(-30);

        // Act
        var dto = new NodeDto(
            id,
            hubId,
            "node-001",
            "Test Node",
            ProtocolDto.WLAN,
            location,
            5,
            lastSeen,
            true,
            "1.0.0",
            85,
            createdAt,
            "AA:BB:CC:DD:EE:FF",
            NodeProvisioningStatusDto.Configured,
            false,
            StorageModeDto.RemoteOnly,
            10,
            lastSyncAt,
            null,
            DebugLevelDto.Normal,
            true,
            lastDebugChange
        );

        // Assert
        dto.Id.Should().Be(id);
        dto.HubId.Should().Be(hubId);
        dto.NodeId.Should().Be("node-001");
        dto.Name.Should().Be("Test Node");
        dto.Protocol.Should().Be(ProtocolDto.WLAN);
        dto.Location.Should().Be(location);
        dto.AssignmentCount.Should().Be(5);
        dto.LastSeen.Should().Be(lastSeen);
        dto.IsOnline.Should().BeTrue();
        dto.FirmwareVersion.Should().Be("1.0.0");
        dto.BatteryLevel.Should().Be(85);
        dto.CreatedAt.Should().Be(createdAt);
        dto.MacAddress.Should().Be("AA:BB:CC:DD:EE:FF");
        dto.Status.Should().Be(NodeProvisioningStatusDto.Configured);
        dto.IsSimulation.Should().BeFalse();
        dto.StorageMode.Should().Be(StorageModeDto.RemoteOnly);
        dto.PendingSyncCount.Should().Be(10);
        dto.LastSyncAt.Should().Be(lastSyncAt);
        dto.LastSyncError.Should().BeNull();
        dto.DebugLevel.Should().Be(DebugLevelDto.Normal);
        dto.EnableRemoteLogging.Should().BeTrue();
        dto.LastDebugChange.Should().Be(lastDebugChange);
    }

    [Fact]
    public void NodeDto_WithAllNullOptionalProperties_IsValid()
    {
        // Act
        var dto = new NodeDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "node-002",
            "Minimal Node",
            ProtocolDto.LoRaWAN,
            null,
            0,
            null,
            false,
            null,
            null,
            DateTime.UtcNow,
            "00:00:00:00:00:00",
            NodeProvisioningStatusDto.Unconfigured,
            true,
            StorageModeDto.LocalAndRemote,
            0,
            null,
            "Error message",
            DebugLevelDto.Production,
            false,
            null
        );

        // Assert
        dto.Location.Should().BeNull();
        dto.LastSeen.Should().BeNull();
        dto.FirmwareVersion.Should().BeNull();
        dto.BatteryLevel.Should().BeNull();
        dto.LastSyncAt.Should().BeNull();
        dto.LastSyncError.Should().Be("Error message");
        dto.LastDebugChange.Should().BeNull();
    }

    #endregion

    #region NodeSensorAssignmentDto Tests

    [Fact]
    public void NodeSensorAssignmentDto_Constructor_SetsAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var nodeId = Guid.NewGuid();
        var sensorId = Guid.NewGuid();
        var assignedAt = DateTime.UtcNow;
        var lastSeenAt = DateTime.UtcNow.AddMinutes(-5);
        var effectiveConfig = new EffectiveConfigDto(
            60, "0x76", 21, 22, null, null, null, null, null, null, 0.0, 1.0);

        // Act
        var dto = new NodeSensorAssignmentDto(
            id,
            nodeId,
            "Node 1",
            sensorId,
            "DHT22",
            "Temperature Sensor",
            1,
            "Living Room",
            "0x77",
            21,
            22,
            null,
            null,
            null,
            null,
            null,
            null,
            120,
            true,
            lastSeenAt,
            assignedAt,
            effectiveConfig
        );

        // Assert
        dto.Id.Should().Be(id);
        dto.NodeId.Should().Be(nodeId);
        dto.NodeName.Should().Be("Node 1");
        dto.SensorId.Should().Be(sensorId);
        dto.SensorCode.Should().Be("DHT22");
        dto.SensorName.Should().Be("Temperature Sensor");
        dto.EndpointId.Should().Be(1);
        dto.Alias.Should().Be("Living Room");
        dto.I2CAddressOverride.Should().Be("0x77");
        dto.SdaPinOverride.Should().Be(21);
        dto.SclPinOverride.Should().Be(22);
        dto.IntervalSecondsOverride.Should().Be(120);
        dto.IsActive.Should().BeTrue();
        dto.LastSeenAt.Should().Be(lastSeenAt);
        dto.AssignedAt.Should().Be(assignedAt);
        dto.EffectiveConfig.Should().Be(effectiveConfig);
    }

    [Fact]
    public void NodeSensorAssignmentDto_WithNullOptionalProperties_IsValid()
    {
        // Arrange
        var effectiveConfig = new EffectiveConfigDto(
            60, null, null, null, null, null, null, null, null, null, 0.0, 1.0);

        // Act
        var dto = new NodeSensorAssignmentDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Node",
            Guid.NewGuid(),
            "BME280",
            "Sensor",
            0,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            false,
            null,
            DateTime.UtcNow,
            effectiveConfig
        );

        // Assert
        dto.Alias.Should().BeNull();
        dto.I2CAddressOverride.Should().BeNull();
        dto.SdaPinOverride.Should().BeNull();
        dto.SclPinOverride.Should().BeNull();
        dto.OneWirePinOverride.Should().BeNull();
        dto.AnalogPinOverride.Should().BeNull();
        dto.DigitalPinOverride.Should().BeNull();
        dto.TriggerPinOverride.Should().BeNull();
        dto.EchoPinOverride.Should().BeNull();
        dto.BaudRateOverride.Should().BeNull();
        dto.IntervalSecondsOverride.Should().BeNull();
        dto.LastSeenAt.Should().BeNull();
    }

    #endregion

    #region SensorDto Tests

    [Fact]
    public void SensorDto_Constructor_SetsAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow.AddDays(-1);
        var updatedAt = DateTime.UtcNow;
        var calibratedAt = DateTime.UtcNow.AddDays(-7);
        var calibrationDueAt = DateTime.UtcNow.AddMonths(6);
        var capabilities = new List<SensorCapabilityDto>();

        // Act
        var dto = new SensorDto(
            id,
            tenantId,
            "DHT22",
            "Temperature/Humidity Sensor",
            "High precision sensor",
            "SN123456",
            "AOSONG",
            "DHT22",
            "https://example.com/datasheet.pdf",
            CommunicationProtocolDto.I2C,
            "0x76",
            21,
            22,
            null,
            null,
            null,
            null,
            null,
            null,
            60,
            1,
            100,
            0.5,
            1.0,
            calibratedAt,
            "Factory calibrated",
            calibrationDueAt,
            "Environmental",
            "thermometer",
            "#FF5722",
            capabilities,
            true,
            createdAt,
            updatedAt
        );

        // Assert
        dto.Id.Should().Be(id);
        dto.TenantId.Should().Be(tenantId);
        dto.Code.Should().Be("DHT22");
        dto.Name.Should().Be("Temperature/Humidity Sensor");
        dto.Description.Should().Be("High precision sensor");
        dto.SerialNumber.Should().Be("SN123456");
        dto.Manufacturer.Should().Be("AOSONG");
        dto.Model.Should().Be("DHT22");
        dto.DatasheetUrl.Should().Be("https://example.com/datasheet.pdf");
        dto.Protocol.Should().Be(CommunicationProtocolDto.I2C);
        dto.I2CAddress.Should().Be("0x76");
        dto.SdaPin.Should().Be(21);
        dto.SclPin.Should().Be(22);
        dto.IntervalSeconds.Should().Be(60);
        dto.MinIntervalSeconds.Should().Be(1);
        dto.WarmupTimeMs.Should().Be(100);
        dto.OffsetCorrection.Should().Be(0.5);
        dto.GainCorrection.Should().Be(1.0);
        dto.LastCalibratedAt.Should().Be(calibratedAt);
        dto.CalibrationNotes.Should().Be("Factory calibrated");
        dto.CalibrationDueAt.Should().Be(calibrationDueAt);
        dto.Category.Should().Be("Environmental");
        dto.Icon.Should().Be("thermometer");
        dto.Color.Should().Be("#FF5722");
        dto.Capabilities.Should().BeEmpty();
        dto.IsActive.Should().BeTrue();
        dto.CreatedAt.Should().Be(createdAt);
        dto.UpdatedAt.Should().Be(updatedAt);
    }

    [Fact]
    public void SensorDto_WithNullOptionalProperties_IsValid()
    {
        // Act
        var dto = new SensorDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "BME280",
            "Minimal Sensor",
            null,
            null,
            null,
            null,
            null,
            CommunicationProtocolDto.OneWire,
            null,
            null,
            null,
            15,
            null,
            null,
            null,
            null,
            null,
            30,
            1,
            0,
            0.0,
            1.0,
            null,
            null,
            null,
            "Environmental",
            null,
            null,
            new List<SensorCapabilityDto>(),
            true,
            DateTime.UtcNow,
            DateTime.UtcNow
        );

        // Assert
        dto.Description.Should().BeNull();
        dto.SerialNumber.Should().BeNull();
        dto.Manufacturer.Should().BeNull();
        dto.Model.Should().BeNull();
        dto.DatasheetUrl.Should().BeNull();
        dto.I2CAddress.Should().BeNull();
        dto.SdaPin.Should().BeNull();
        dto.SclPin.Should().BeNull();
        dto.OneWirePin.Should().Be(15);
        dto.LastCalibratedAt.Should().BeNull();
        dto.CalibrationNotes.Should().BeNull();
        dto.CalibrationDueAt.Should().BeNull();
        dto.Icon.Should().BeNull();
        dto.Color.Should().BeNull();
    }

    #endregion

    #region SensorCapabilityConfigDto Tests

    [Fact]
    public void SensorCapabilityConfigDto_Constructor_SetsAllProperties()
    {
        // Act
        var dto = new SensorCapabilityConfigDto(
            "temperature",
            "Temperature",
            "°C"
        );

        // Assert
        dto.MeasurementType.Should().Be("temperature");
        dto.DisplayName.Should().Be("Temperature");
        dto.Unit.Should().Be("°C");
    }

    #endregion

    #region SensorLatestReadingDto Tests

    [Fact]
    public void SensorLatestReadingDto_Constructor_SetsAllProperties()
    {
        // Arrange
        var assignmentId = Guid.NewGuid();
        var sensorId = Guid.NewGuid();
        var measurements = new List<LatestMeasurementDto>();

        // Act
        var dto = new SensorLatestReadingDto(
            assignmentId,
            sensorId,
            "DHT22",
            "DHT22 Temperature",
            "Temp",
            "dht22",
            "DHT22",
            0,
            "thermometer",
            "#FF5722",
            true,
            measurements
        );

        // Assert
        dto.AssignmentId.Should().Be(assignmentId);
        dto.SensorId.Should().Be(sensorId);
        dto.DisplayName.Should().Be("DHT22");
        dto.FullName.Should().Be("DHT22 Temperature");
        dto.Alias.Should().Be("Temp");
        dto.SensorCode.Should().Be("dht22");
        dto.SensorModel.Should().Be("DHT22");
        dto.EndpointId.Should().Be(0);
        dto.Icon.Should().Be("thermometer");
        dto.Color.Should().Be("#FF5722");
        dto.IsActive.Should().BeTrue();
        dto.Measurements.Should().BeEmpty();
    }

    [Fact]
    public void SensorLatestReadingDto_WithNullOptionalProperties_IsValid()
    {
        // Act
        var dto = new SensorLatestReadingDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "BME280",
            "BME280 Sensor",
            null,
            "bme280",
            "BME280",
            1,
            null,
            null,
            false,
            new List<LatestMeasurementDto>()
        );

        // Assert
        dto.Alias.Should().BeNull();
        dto.Icon.Should().BeNull();
        dto.Color.Should().BeNull();
    }

    [Fact]
    public void LatestMeasurementDto_Constructor_SetsAllProperties()
    {
        // Act
        var dto = new LatestMeasurementDto(
            12345,
            "temperature",
            "Temperature",
            21.45,
            21.5,
            "°C",
            DateTime.UtcNow
        );

        // Assert
        dto.ReadingId.Should().Be(12345);
        dto.MeasurementType.Should().Be("temperature");
        dto.DisplayName.Should().Be("Temperature");
        dto.RawValue.Should().Be(21.45);
        dto.Value.Should().Be(21.5);
        dto.Unit.Should().Be("°C");
    }

    #endregion

    #region NodeDebugConfigurationDto Tests

    [Fact]
    public void NodeDebugConfigurationDto_Constructor_SetsAllProperties()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var lastDebugChange = DateTime.UtcNow;

        // Act
        var dto = new NodeDebugConfigurationDto(
            nodeId,
            "ESP32-001",
            DebugLevelDto.Debug,
            true,
            lastDebugChange
        );

        // Assert
        dto.NodeId.Should().Be(nodeId);
        dto.SerialNumber.Should().Be("ESP32-001");
        dto.DebugLevel.Should().Be(DebugLevelDto.Debug);
        dto.EnableRemoteLogging.Should().BeTrue();
        dto.LastDebugChange.Should().Be(lastDebugChange);
    }

    [Fact]
    public void NodeDebugConfigurationDto_WithNullOptionalProperties_IsValid()
    {
        // Act
        var dto = new NodeDebugConfigurationDto(
            Guid.NewGuid(),
            "ESP32-002",
            DebugLevelDto.Production,
            false,
            null
        );

        // Assert
        dto.LastDebugChange.Should().BeNull();
    }

    #endregion

    #region HubDto Tests

    [Fact]
    public void HubDto_Constructor_SetsAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var lastSeen = DateTime.UtcNow;
        var createdAt = DateTime.UtcNow.AddDays(-1);

        // Act
        var dto = new HubDto(
            id,
            tenantId,
            "hub-001",
            "Test Hub",
            "A test hub",
            lastSeen,
            true,
            createdAt,
            5,
            "MyNetwork",
            "password123",
            "http://192.168.1.100:5002",
            5002
        );

        // Assert
        dto.Id.Should().Be(id);
        dto.TenantId.Should().Be(tenantId);
        dto.HubId.Should().Be("hub-001");
        dto.Name.Should().Be("Test Hub");
        dto.Description.Should().Be("A test hub");
        dto.LastSeen.Should().Be(lastSeen);
        dto.IsOnline.Should().BeTrue();
        dto.CreatedAt.Should().Be(createdAt);
        dto.SensorCount.Should().Be(5);
        dto.DefaultWifiSsid.Should().Be("MyNetwork");
        dto.DefaultWifiPassword.Should().Be("password123");
        dto.ApiUrl.Should().Be("http://192.168.1.100:5002");
        dto.ApiPort.Should().Be(5002);
    }

    [Fact]
    public void HubDto_WithDefaultOptionalProperties_UsesDefaults()
    {
        // Act - only required parameters
        var dto = new HubDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "hub-002",
            "Minimal Hub",
            null,
            null,
            false,
            DateTime.UtcNow,
            0
        );

        // Assert
        dto.Description.Should().BeNull();
        dto.LastSeen.Should().BeNull();
        dto.IsOnline.Should().BeFalse();
        dto.SensorCount.Should().Be(0);
        dto.DefaultWifiSsid.Should().BeNull();
        dto.DefaultWifiPassword.Should().BeNull();
        dto.ApiUrl.Should().BeNull();
        dto.ApiPort.Should().Be(5002); // Default value
    }

    [Fact]
    public void CreateHubDto_Constructor_SetsAllProperties()
    {
        // Act
        var dto = new CreateHubDto("hub-001", "Test Hub", "Description");

        // Assert
        dto.HubId.Should().Be("hub-001");
        dto.Name.Should().Be("Test Hub");
        dto.Description.Should().Be("Description");
    }

    [Fact]
    public void UpdateHubDto_Constructor_SetsAllProperties()
    {
        // Act
        var dto = new UpdateHubDto(
            "Updated Hub",
            "New Description",
            "NewSSID",
            "NewPassword",
            "http://new-url",
            5003
        );

        // Assert
        dto.Name.Should().Be("Updated Hub");
        dto.Description.Should().Be("New Description");
        dto.DefaultWifiSsid.Should().Be("NewSSID");
        dto.DefaultWifiPassword.Should().Be("NewPassword");
        dto.ApiUrl.Should().Be("http://new-url");
        dto.ApiPort.Should().Be(5003);
    }

    #endregion

    #region LocationDto Tests

    [Fact]
    public void LocationDto_HasCoordinates_ReturnsTrueWhenBothPresent()
    {
        // Act
        var dto = new LocationDto("Home", 52.52, 13.405);

        // Assert
        dto.HasCoordinates.Should().BeTrue();
        dto.Name.Should().Be("Home");
        dto.Latitude.Should().Be(52.52);
        dto.Longitude.Should().Be(13.405);
    }

    [Fact]
    public void LocationDto_HasCoordinates_ReturnsFalseWhenLatitudeMissing()
    {
        // Act
        var dto = new LocationDto("Home", null, 13.405);

        // Assert
        dto.HasCoordinates.Should().BeFalse();
    }

    [Fact]
    public void LocationDto_HasCoordinates_ReturnsFalseWhenLongitudeMissing()
    {
        // Act
        var dto = new LocationDto("Home", 52.52, null);

        // Assert
        dto.HasCoordinates.Should().BeFalse();
    }

    [Fact]
    public void LocationDto_HasCoordinates_ReturnsFalseWhenBothMissing()
    {
        // Act
        var dto = new LocationDto("Home");

        // Assert
        dto.HasCoordinates.Should().BeFalse();
        dto.Latitude.Should().BeNull();
        dto.Longitude.Should().BeNull();
    }

    #endregion

    #region AlertDto Tests

    [Fact]
    public void AlertDto_Constructor_SetsAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var hubId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();
        var alertTypeId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var expiresAt = DateTime.UtcNow.AddHours(1);
        var acknowledgedAt = DateTime.UtcNow.AddMinutes(30);

        // Act
        var dto = new AlertDto(
            id,
            tenantId,
            hubId,
            "Hub 1",
            nodeId,
            "Node 1",
            alertTypeId,
            "mold_risk",
            "Mold Risk",
            AlertLevelDto.Warning,
            "High humidity detected",
            "Increase ventilation",
            AlertSourceDto.Cloud,
            createdAt,
            expiresAt,
            acknowledgedAt,
            true
        );

        // Assert
        dto.Id.Should().Be(id);
        dto.TenantId.Should().Be(tenantId);
        dto.HubId.Should().Be(hubId);
        dto.HubName.Should().Be("Hub 1");
        dto.NodeId.Should().Be(nodeId);
        dto.NodeName.Should().Be("Node 1");
        dto.AlertTypeId.Should().Be(alertTypeId);
        dto.AlertTypeCode.Should().Be("mold_risk");
        dto.AlertTypeName.Should().Be("Mold Risk");
        dto.Level.Should().Be(AlertLevelDto.Warning);
        dto.Message.Should().Be("High humidity detected");
        dto.Recommendation.Should().Be("Increase ventilation");
        dto.Source.Should().Be(AlertSourceDto.Cloud);
        dto.CreatedAt.Should().Be(createdAt);
        dto.ExpiresAt.Should().Be(expiresAt);
        dto.AcknowledgedAt.Should().Be(acknowledgedAt);
        dto.IsActive.Should().BeTrue();
    }

    [Fact]
    public void AlertDto_WithNullOptionalProperties_IsValid()
    {
        // Act
        var dto = new AlertDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null,
            null,
            null,
            Guid.NewGuid(),
            "hub_offline",
            "Hub Offline",
            AlertLevelDto.Critical,
            "Hub is offline",
            null,
            AlertSourceDto.Local,
            DateTime.UtcNow,
            null,
            null,
            false
        );

        // Assert
        dto.HubId.Should().BeNull();
        dto.HubName.Should().BeNull();
        dto.NodeId.Should().BeNull();
        dto.NodeName.Should().BeNull();
        dto.Recommendation.Should().BeNull();
        dto.ExpiresAt.Should().BeNull();
        dto.AcknowledgedAt.Should().BeNull();
    }

    [Fact]
    public void CreateAlertDto_WithDefaultValues_UsesDefaults()
    {
        // Act
        var dto = new CreateAlertDto("mold_risk");

        // Assert
        dto.AlertTypeCode.Should().Be("mold_risk");
        dto.HubId.Should().BeNull();
        dto.NodeId.Should().BeNull();
        dto.Level.Should().Be(AlertLevelDto.Warning);
        dto.Message.Should().Be("");
        dto.Recommendation.Should().BeNull();
        dto.ExpiresAt.Should().BeNull();
    }

    [Fact]
    public void AcknowledgeAlertDto_Constructor_SetsAlertId()
    {
        // Arrange
        var alertId = Guid.NewGuid();

        // Act
        var dto = new AcknowledgeAlertDto(alertId);

        // Assert
        dto.AlertId.Should().Be(alertId);
    }

    [Fact]
    public void AlertFilterDto_WithDefaultValues_UsesDefaults()
    {
        // Act
        var dto = new AlertFilterDto();

        // Assert
        dto.HubId.Should().BeNull();
        dto.NodeId.Should().BeNull();
        dto.AlertTypeCode.Should().BeNull();
        dto.Level.Should().BeNull();
        dto.Source.Should().BeNull();
        dto.IsActive.Should().BeNull();
        dto.IsAcknowledged.Should().BeNull();
        dto.From.Should().BeNull();
        dto.To.Should().BeNull();
        dto.Page.Should().Be(1);
        dto.PageSize.Should().Be(50);
    }

    #endregion

    #region AlertTypeDto Tests

    [Fact]
    public void AlertTypeDto_Constructor_SetsAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        // Act
        var dto = new AlertTypeDto(
            id,
            "mold_risk",
            "Schimmelrisiko",
            "Risk of mold growth",
            AlertLevelDto.Warning,
            "warning",
            true,
            createdAt
        );

        // Assert
        dto.Id.Should().Be(id);
        dto.Code.Should().Be("mold_risk");
        dto.Name.Should().Be("Schimmelrisiko");
        dto.Description.Should().Be("Risk of mold growth");
        dto.DefaultLevel.Should().Be(AlertLevelDto.Warning);
        dto.IconName.Should().Be("warning");
        dto.IsGlobal.Should().BeTrue();
        dto.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void AlertTypeDto_WithNullOptionalProperties_IsValid()
    {
        // Act
        var dto = new AlertTypeDto(
            Guid.NewGuid(),
            "custom",
            "Custom Alert",
            null,
            AlertLevelDto.Info,
            null,
            false,
            DateTime.UtcNow
        );

        // Assert
        dto.Description.Should().BeNull();
        dto.IconName.Should().BeNull();
    }

    [Fact]
    public void CreateAlertTypeDto_WithDefaultValues_UsesDefaults()
    {
        // Act
        var dto = new CreateAlertTypeDto("new_alert", "New Alert");

        // Assert
        dto.Code.Should().Be("new_alert");
        dto.Name.Should().Be("New Alert");
        dto.Description.Should().BeNull();
        dto.DefaultLevel.Should().Be(AlertLevelDto.Info); // Default is Info, not Warning
        dto.IconName.Should().BeNull();
    }

    #endregion

    #region PaginatedResultDto Tests

    [Fact]
    public void PaginatedResultDto_TotalPages_CalculatesCorrectly()
    {
        // Arrange
        var items = new List<string> { "a", "b", "c" };

        // Act
        var dto = new PaginatedResultDto<string>(items, 10, 1, 3);

        // Assert
        dto.TotalPages.Should().Be(4); // Ceiling(10/3) = 4
    }

    [Fact]
    public void PaginatedResultDto_HasNextPage_ReturnsTrueWhenMorePages()
    {
        // Act
        var dto = new PaginatedResultDto<int>(new List<int> { 1, 2, 3 }, 10, 1, 3);

        // Assert
        dto.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void PaginatedResultDto_HasNextPage_ReturnsFalseOnLastPage()
    {
        // Act
        var dto = new PaginatedResultDto<int>(new List<int> { 10 }, 10, 4, 3);

        // Assert
        dto.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void PaginatedResultDto_HasPreviousPage_ReturnsTrueWhenNotFirstPage()
    {
        // Act
        var dto = new PaginatedResultDto<int>(new List<int> { 1, 2, 3 }, 10, 2, 3);

        // Assert
        dto.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public void PaginatedResultDto_HasPreviousPage_ReturnsFalseOnFirstPage()
    {
        // Act
        var dto = new PaginatedResultDto<int>(new List<int> { 1, 2, 3 }, 10, 1, 3);

        // Assert
        dto.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void PaginatedResultDto_Empty_CreatesEmptyResult()
    {
        // Act
        var dto = PaginatedResultDto<string>.Empty(2, 25);

        // Assert
        dto.Items.Should().BeEmpty();
        dto.TotalCount.Should().Be(0);
        dto.Page.Should().Be(2);
        dto.PageSize.Should().Be(25);
        dto.TotalPages.Should().Be(0);
        dto.HasNextPage.Should().BeFalse();
        dto.HasPreviousPage.Should().BeTrue();
    }

    #endregion

    #region ReadingDto Tests

    [Fact]
    public void ReadingDto_Constructor_SetsAllProperties()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
        var sensorId = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;
        var location = new LocationDto("Room", 52.52, 13.405);

        // Act
        var dto = new ReadingDto(
            12345,
            tenantId,
            nodeId,
            "Node 1",
            assignmentId,
            sensorId,
            "DHT22",
            "Temperature Sensor",
            "thermometer",
            "#FF5722",
            "temperature",
            "Temperature",
            21.5,
            21.6,
            "°C",
            timestamp,
            location,
            true
        );

        // Assert
        dto.Id.Should().Be(12345);
        dto.TenantId.Should().Be(tenantId);
        dto.NodeId.Should().Be(nodeId);
        dto.NodeName.Should().Be("Node 1");
        dto.AssignmentId.Should().Be(assignmentId);
        dto.SensorId.Should().Be(sensorId);
        dto.SensorCode.Should().Be("DHT22");
        dto.SensorName.Should().Be("Temperature Sensor");
        dto.SensorIcon.Should().Be("thermometer");
        dto.SensorColor.Should().Be("#FF5722");
        dto.MeasurementType.Should().Be("temperature");
        dto.DisplayName.Should().Be("Temperature");
        dto.RawValue.Should().Be(21.5);
        dto.Value.Should().Be(21.6);
        dto.Unit.Should().Be("°C");
        dto.Timestamp.Should().Be(timestamp);
        dto.Location.Should().Be(location);
        dto.IsSyncedToCloud.Should().BeTrue();
    }

    [Fact]
    public void ReadingDto_WithNullOptionalProperties_IsValid()
    {
        // Act
        var dto = new ReadingDto(
            1,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Node",
            null,
            null,
            "DHT22",
            "Sensor",
            null,
            null,
            "temp",
            "Temp",
            20.0,
            20.0,
            "°C",
            DateTime.UtcNow,
            null,
            false
        );

        // Assert
        dto.AssignmentId.Should().BeNull();
        dto.SensorId.Should().BeNull();
        dto.SensorIcon.Should().BeNull();
        dto.SensorColor.Should().BeNull();
        dto.Location.Should().BeNull();
    }

    [Fact]
    public void CreateReadingDto_WithDefaultValues_UsesDefaults()
    {
        // Act
        var dto = new CreateReadingDto("node-001", 1, "temperature", 21.5);

        // Assert
        dto.NodeId.Should().Be("node-001");
        dto.EndpointId.Should().Be(1);
        dto.MeasurementType.Should().Be("temperature");
        dto.RawValue.Should().Be(21.5);
        dto.HubId.Should().BeNull();
        dto.Timestamp.Should().BeNull();
    }

    [Fact]
    public void ReadingFilterDto_WithDefaultValues_UsesDefaults()
    {
        // Act
        var dto = new ReadingFilterDto();

        // Assert
        dto.NodeId.Should().BeNull();
        dto.NodeIdentifier.Should().BeNull();
        dto.HubId.Should().BeNull();
        dto.AssignmentId.Should().BeNull();
        dto.MeasurementType.Should().BeNull();
        dto.From.Should().BeNull();
        dto.To.Should().BeNull();
        dto.IsSyncedToCloud.Should().BeNull();
        dto.Page.Should().Be(1);
        dto.PageSize.Should().Be(50);
    }

    [Fact]
    public void ReadingValueDto_Constructor_SetsAllProperties()
    {
        // Act
        var dto = new ReadingValueDto(1, "temperature", 21.5);

        // Assert
        dto.EndpointId.Should().Be(1);
        dto.MeasurementType.Should().Be("temperature");
        dto.RawValue.Should().Be(21.5);
    }

    [Fact]
    public void CreateBatchReadingsDto_Constructor_SetsAllProperties()
    {
        // Arrange
        var readings = new List<ReadingValueDto>
        {
            new(1, "temperature", 21.5),
            new(2, "humidity", 65.0)
        };
        var timestamp = DateTime.UtcNow;

        // Act
        var dto = new CreateBatchReadingsDto("node-001", "hub-001", readings, timestamp);

        // Assert
        dto.NodeId.Should().Be("node-001");
        dto.HubId.Should().Be("hub-001");
        dto.Readings.Should().HaveCount(2);
        dto.Timestamp.Should().Be(timestamp);
    }

    [Fact]
    public void CreateSensorReadingDto_WithDefaultValues_UsesDefaults()
    {
        // Act
        var dto = new CreateSensorReadingDto("device-001", "temperature", 21.5);

        // Assert
        dto.DeviceId.Should().Be("device-001");
        dto.Type.Should().Be("temperature");
        dto.Value.Should().Be(21.5);
        dto.Unit.Should().BeNull();
        dto.Timestamp.Should().BeNull();
        dto.EndpointId.Should().BeNull();
    }

    #endregion

    #region SensorCapabilityDto Tests

    [Fact]
    public void SensorCapabilityDto_Constructor_SetsAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var sensorId = Guid.NewGuid();

        // Act
        var dto = new SensorCapabilityDto(
            id,
            sensorId,
            "temperature",
            "Temperature",
            "°C",
            -40.0,
            125.0,
            0.01,
            0.5,
            1026u,
            "TemperatureMeasurement",
            1,
            true
        );

        // Assert
        dto.Id.Should().Be(id);
        dto.SensorId.Should().Be(sensorId);
        dto.MeasurementType.Should().Be("temperature");
        dto.DisplayName.Should().Be("Temperature");
        dto.Unit.Should().Be("°C");
        dto.MinValue.Should().Be(-40.0);
        dto.MaxValue.Should().Be(125.0);
        dto.Resolution.Should().Be(0.01);
        dto.Accuracy.Should().Be(0.5);
        dto.MatterClusterId.Should().Be(1026u);
        dto.MatterClusterName.Should().Be("TemperatureMeasurement");
        dto.SortOrder.Should().Be(1);
        dto.IsActive.Should().BeTrue();
    }

    [Fact]
    public void SensorCapabilityDto_WithNullOptionalProperties_IsValid()
    {
        // Act
        var dto = new SensorCapabilityDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "custom",
            "Custom",
            "unit",
            null,
            null,
            0.1,
            1.0,
            null,
            null,
            0,
            true
        );

        // Assert
        dto.MinValue.Should().BeNull();
        dto.MaxValue.Should().BeNull();
        dto.MatterClusterId.Should().BeNull();
        dto.MatterClusterName.Should().BeNull();
    }

    [Fact]
    public void CreateSensorCapabilityDto_WithDefaultValues_UsesDefaults()
    {
        // Act
        var dto = new CreateSensorCapabilityDto("temperature", "Temperature", "°C");

        // Assert
        dto.MeasurementType.Should().Be("temperature");
        dto.DisplayName.Should().Be("Temperature");
        dto.Unit.Should().Be("°C");
        dto.MinValue.Should().BeNull();
        dto.MaxValue.Should().BeNull();
        dto.Resolution.Should().Be(0.01);
        dto.Accuracy.Should().Be(0.5);
        dto.MatterClusterId.Should().BeNull();
        dto.MatterClusterName.Should().BeNull();
        dto.SortOrder.Should().Be(0);
    }

    #endregion

    #region TenantDto Tests

    [Fact]
    public void TenantDto_Constructor_SetsAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var createdAt = DateTime.UtcNow.AddDays(-30);
        var lastSyncAt = DateTime.UtcNow;

        // Act
        var dto = new TenantDto(
            id,
            "Test Tenant",
            "cloud-api-key-123",
            createdAt,
            lastSyncAt,
            true
        );

        // Assert
        dto.Id.Should().Be(id);
        dto.Name.Should().Be("Test Tenant");
        dto.CloudApiKey.Should().Be("cloud-api-key-123");
        dto.CreatedAt.Should().Be(createdAt);
        dto.LastSyncAt.Should().Be(lastSyncAt);
        dto.IsActive.Should().BeTrue();
    }

    [Fact]
    public void TenantDto_WithNullOptionalProperties_IsValid()
    {
        // Act
        var dto = new TenantDto(
            Guid.NewGuid(),
            "Minimal Tenant",
            null,
            DateTime.UtcNow,
            null,
            true
        );

        // Assert
        dto.CloudApiKey.Should().BeNull();
        dto.LastSyncAt.Should().BeNull();
    }

    [Fact]
    public void CreateTenantDto_Constructor_SetsAllProperties()
    {
        // Act
        var dto = new CreateTenantDto("New Tenant", "api-key");

        // Assert
        dto.Name.Should().Be("New Tenant");
        dto.CloudApiKey.Should().Be("api-key");
    }

    [Fact]
    public void UpdateTenantDto_Constructor_SetsAllProperties()
    {
        // Act
        var dto = new UpdateTenantDto("Updated Name", "new-api-key", false);

        // Assert
        dto.Name.Should().Be("Updated Name");
        dto.CloudApiKey.Should().Be("new-api-key");
        dto.IsActive.Should().BeFalse();
    }

    #endregion

    #region SyncedNodeDto Tests

    [Fact]
    public void SyncedNodeDto_Constructor_SetsAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var cloudNodeId = Guid.NewGuid();
        var lastSyncAt = DateTime.UtcNow;
        var createdAt = DateTime.UtcNow.AddDays(-1);
        var location = new LocationDto("Room", 52.52, 13.405);

        // Act
        var dto = new SyncedNodeDto(
            id,
            cloudNodeId,
            "node-001",
            "Synced Node",
            SyncedNodeSourceDto.OtherHub,
            "Hub B",
            location,
            true,
            lastSyncAt,
            createdAt
        );

        // Assert
        dto.Id.Should().Be(id);
        dto.CloudNodeId.Should().Be(cloudNodeId);
        dto.NodeId.Should().Be("node-001");
        dto.Name.Should().Be("Synced Node");
        dto.Source.Should().Be(SyncedNodeSourceDto.OtherHub);
        dto.SourceDetails.Should().Be("Hub B");
        dto.Location.Should().Be(location);
        dto.IsOnline.Should().BeTrue();
        dto.LastSyncAt.Should().Be(lastSyncAt);
        dto.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void SyncedNodeDto_WithNullOptionalProperties_IsValid()
    {
        // Act
        var dto = new SyncedNodeDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "node-002",
            "Minimal Node",
            SyncedNodeSourceDto.Virtual,
            null,
            null,
            false,
            DateTime.UtcNow,
            DateTime.UtcNow
        );

        // Assert
        dto.SourceDetails.Should().BeNull();
        dto.Location.Should().BeNull();
    }

    [Fact]
    public void CreateSyncedNodeDto_Constructor_SetsAllProperties()
    {
        // Arrange
        var cloudNodeId = Guid.NewGuid();
        var location = new LocationDto("Test", null, null);

        // Act
        var dto = new CreateSyncedNodeDto(
            cloudNodeId,
            "node-001",
            "Test Node",
            SyncedNodeSourceDto.Direct,
            "DWD Köln",
            location,
            true
        );

        // Assert
        dto.CloudNodeId.Should().Be(cloudNodeId);
        dto.NodeId.Should().Be("node-001");
        dto.Name.Should().Be("Test Node");
        dto.Source.Should().Be(SyncedNodeSourceDto.Direct);
        dto.SourceDetails.Should().Be("DWD Köln");
        dto.Location.Should().Be(location);
        dto.IsOnline.Should().BeTrue();
    }

    [Fact]
    public void CreateSyncedNodeDto_WithDefaultValues_IsValid()
    {
        // Act
        var dto = new CreateSyncedNodeDto(
            Guid.NewGuid(),
            "node-002",
            "Default Node",
            SyncedNodeSourceDto.Virtual
        );

        // Assert
        dto.SourceDetails.Should().BeNull();
        dto.Location.Should().BeNull();
        dto.IsOnline.Should().BeFalse();
    }

    #endregion

    #region UnifiedNodeDto Tests

    [Fact]
    public void UnifiedNodeDto_Constructor_SetsAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var location = new LocationDto("Room", null, null);
        var lastSeen = DateTime.UtcNow;
        var sensors = new List<SensorDto>();
        var readings = new List<UnifiedReadingDto>
        {
            new("temperature", "Temperature", 21.5, "°C", DateTime.UtcNow, UnifiedNodeSourceDto.Local)
        };

        // Act
        var dto = new UnifiedNodeDto(
            id,
            "node-001",
            "Test Node",
            UnifiedNodeSourceDto.Local,
            "Hub A",
            sensors,
            location,
            true,
            lastSeen,
            readings
        );

        // Assert
        dto.Id.Should().Be(id);
        dto.NodeId.Should().Be("node-001");
        dto.Name.Should().Be("Test Node");
        dto.Source.Should().Be(UnifiedNodeSourceDto.Local);
        dto.SourceDetails.Should().Be("Hub A");
        dto.Sensors.Should().NotBeNull();
        dto.Location.Should().Be(location);
        dto.IsOnline.Should().BeTrue();
        dto.LastSeen.Should().Be(lastSeen);
        dto.LatestReadings.Should().HaveCount(1);
    }

    [Fact]
    public void UnifiedNodeDto_WithNullOptionalProperties_IsValid()
    {
        // Act
        var dto = new UnifiedNodeDto(
            Guid.NewGuid(),
            "node-002",
            "Minimal Node",
            UnifiedNodeSourceDto.Virtual,
            null,
            null,
            null,
            false,
            null,
            null
        );

        // Assert
        dto.SourceDetails.Should().BeNull();
        dto.Sensors.Should().BeNull();
        dto.Location.Should().BeNull();
        dto.LastSeen.Should().BeNull();
        dto.LatestReadings.Should().BeNull();
    }

    [Fact]
    public void UnifiedReadingDto_Constructor_SetsAllProperties()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act
        var dto = new UnifiedReadingDto("humidity", "Humidity", 65.0, "%", timestamp, UnifiedNodeSourceDto.OtherHub);

        // Assert
        dto.SensorTypeId.Should().Be("humidity");
        dto.SensorTypeName.Should().Be("Humidity");
        dto.Value.Should().Be(65.0);
        dto.Unit.Should().Be("%");
        dto.Timestamp.Should().Be(timestamp);
        dto.Source.Should().Be(UnifiedNodeSourceDto.OtherHub);
    }

    #endregion

    #region EffectiveConfigDto Tests

    [Fact]
    public void EffectiveConfigDto_Constructor_SetsAllProperties()
    {
        // Act
        var dto = new EffectiveConfigDto(
            60,
            "0x76",
            21,
            22,
            4,
            34,
            35,
            12,
            13,
            9600,
            0.5,
            1.05
        );

        // Assert
        dto.IntervalSeconds.Should().Be(60);
        dto.I2CAddress.Should().Be("0x76");
        dto.SdaPin.Should().Be(21);
        dto.SclPin.Should().Be(22);
        dto.OneWirePin.Should().Be(4);
        dto.AnalogPin.Should().Be(34);
        dto.DigitalPin.Should().Be(35);
        dto.TriggerPin.Should().Be(12);
        dto.EchoPin.Should().Be(13);
        dto.BaudRate.Should().Be(9600);
        dto.OffsetCorrection.Should().Be(0.5);
        dto.GainCorrection.Should().Be(1.05);
    }

    [Fact]
    public void EffectiveConfigDto_WithNullOptionalProperties_IsValid()
    {
        // Act
        var dto = new EffectiveConfigDto(
            30,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            0.0,
            1.0
        );

        // Assert
        dto.I2CAddress.Should().BeNull();
        dto.SdaPin.Should().BeNull();
        dto.SclPin.Should().BeNull();
        dto.OneWirePin.Should().BeNull();
        dto.AnalogPin.Should().BeNull();
        dto.DigitalPin.Should().BeNull();
        dto.TriggerPin.Should().BeNull();
        dto.EchoPin.Should().BeNull();
        dto.BaudRate.Should().BeNull();
    }

    #endregion

    #region SensorAssignmentConfigDto Tests

    [Fact]
    public void SensorAssignmentConfigDto_Constructor_SetsAllProperties()
    {
        // Arrange
        var capabilities = new List<SensorCapabilityConfigDto>
        {
            new("temperature", "Temperature", "°C"),
            new("humidity", "Humidity", "%")
        };

        // Act
        var dto = new SensorAssignmentConfigDto(
            1,
            "DHT22",
            "Temperature/Humidity Sensor",
            "thermometer",
            "#FF5722",
            true,
            60,
            "0x76",
            21,
            22,
            null,
            null,
            null,
            null,
            null,
            0.5,
            1.0,
            capabilities
        );

        // Assert
        dto.EndpointId.Should().Be(1);
        dto.SensorCode.Should().Be("DHT22");
        dto.SensorName.Should().Be("Temperature/Humidity Sensor");
        dto.Icon.Should().Be("thermometer");
        dto.Color.Should().Be("#FF5722");
        dto.IsActive.Should().BeTrue();
        dto.IntervalSeconds.Should().Be(60);
        dto.I2CAddress.Should().Be("0x76");
        dto.SdaPin.Should().Be(21);
        dto.SclPin.Should().Be(22);
        dto.OffsetCorrection.Should().Be(0.5);
        dto.GainCorrection.Should().Be(1.0);
        dto.Capabilities.Should().HaveCount(2);
    }

    [Fact]
    public void SensorAssignmentConfigDto_WithNullOptionalProperties_IsValid()
    {
        // Act
        var dto = new SensorAssignmentConfigDto(
            0,
            "BME280",
            "Sensor",
            null,
            null,
            true,
            30,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            0.0,
            1.0,
            new List<SensorCapabilityConfigDto>()
        );

        // Assert
        dto.Icon.Should().BeNull();
        dto.Color.Should().BeNull();
        dto.I2CAddress.Should().BeNull();
        dto.SdaPin.Should().BeNull();
        dto.SclPin.Should().BeNull();
        dto.OneWirePin.Should().BeNull();
        dto.AnalogPin.Should().BeNull();
        dto.DigitalPin.Should().BeNull();
        dto.TriggerPin.Should().BeNull();
        dto.EchoPin.Should().BeNull();
    }

    #endregion

    #region ChartDataDto Tests

    [Fact]
    public void ChartDataDto_Constructor_SetsAllProperties()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
        var sensorId = Guid.NewGuid();
        var lastUpdate = DateTime.UtcNow;
        var minTimestamp = DateTime.UtcNow.AddHours(-2);
        var maxTimestamp = DateTime.UtcNow.AddHours(-1);
        var dataPoints = new List<ChartPointDto>
        {
            new(DateTime.UtcNow, 21.5, 20.0, 23.0)
        };
        var stats = new ChartStatsDto(18.0, minTimestamp, 25.0, maxTimestamp, 21.5);
        var trend = new TrendDto(0.5, 2.3, "up");

        // Act
        var dto = new ChartDataDto(
            nodeId,
            "Node 1",
            assignmentId,
            sensorId,
            "DHT22",
            "temperature",
            "Living Room",
            "°C",
            "#FF5722",
            21.5,
            lastUpdate,
            stats,
            trend,
            dataPoints
        );

        // Assert
        dto.NodeId.Should().Be(nodeId);
        dto.NodeName.Should().Be("Node 1");
        dto.AssignmentId.Should().Be(assignmentId);
        dto.SensorId.Should().Be(sensorId);
        dto.SensorName.Should().Be("DHT22");
        dto.MeasurementType.Should().Be("temperature");
        dto.LocationName.Should().Be("Living Room");
        dto.Unit.Should().Be("°C");
        dto.Color.Should().Be("#FF5722");
        dto.CurrentValue.Should().Be(21.5);
        dto.LastUpdate.Should().Be(lastUpdate);
        dto.Stats.Should().Be(stats);
        dto.Trend.Should().Be(trend);
        dto.DataPoints.Should().HaveCount(1);
    }

    [Fact]
    public void ChartDataDto_WithNullOptionalIds_IsValid()
    {
        // Arrange
        var minTimestamp = DateTime.UtcNow.AddHours(-2);
        var maxTimestamp = DateTime.UtcNow.AddHours(-1);
        var stats = new ChartStatsDto(18.0, minTimestamp, 25.0, maxTimestamp, 21.5);
        var trend = new TrendDto(0.0, 0.0, "stable");

        // Act
        var dto = new ChartDataDto(
            Guid.NewGuid(),
            "Node 2",
            null,
            null,
            "BME280",
            "humidity",
            "Bedroom",
            "%",
            "#2196F3",
            65.0,
            DateTime.UtcNow,
            stats,
            trend,
            new List<ChartPointDto>()
        );

        // Assert
        dto.AssignmentId.Should().BeNull();
        dto.SensorId.Should().BeNull();
    }

    #endregion

    #region NodeHardwareStatusDto Tests

    [Fact]
    public void NodeHardwareStatusDto_Constructor_SetsAllProperties()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var reportedAt = DateTime.UtcNow;
        var summary = new HardwareSummaryDto(5, 3, 2, 1, true, true, "OK");
        var detectedDevices = new List<DetectedDeviceDto>
        {
            new("BME280", "I2C", "0x76", "OK", "temperature", 1, null)
        };
        var storage = new StorageStatusDto(true, "LOCAL_AND_REMOTE", 16000000000, 1000000000, 15000000000, 5, DateTime.UtcNow.AddMinutes(-10), null);
        var busStatus = new BusStatusDto(true, 2, new List<string> { "0x76", "0x77" }, true, 1, true, true);

        // Act
        var dto = new NodeHardwareStatusDto(
            nodeId,
            "ESP32-001",
            "1.0.0",
            "ESP32-WROOM-32",
            reportedAt,
            summary,
            detectedDevices,
            storage,
            busStatus
        );

        // Assert
        dto.NodeId.Should().Be(nodeId);
        dto.SerialNumber.Should().Be("ESP32-001");
        dto.FirmwareVersion.Should().Be("1.0.0");
        dto.HardwareType.Should().Be("ESP32-WROOM-32");
        dto.ReportedAt.Should().Be(reportedAt);
        dto.Summary.Should().Be(summary);
        dto.DetectedDevices.Should().HaveCount(1);
        dto.Storage.Should().Be(storage);
        dto.BusStatus.Should().Be(busStatus);
    }

    [Fact]
    public void HardwareSummaryDto_Constructor_SetsAllProperties()
    {
        // Act
        var dto = new HardwareSummaryDto(5, 3, 2, 1, true, true, "Warning");

        // Assert
        dto.TotalDevicesDetected.Should().Be(5);
        dto.SensorsConfigured.Should().Be(3);
        dto.SensorsOk.Should().Be(2);
        dto.SensorsError.Should().Be(1);
        dto.HasSdCard.Should().BeTrue();
        dto.HasGps.Should().BeTrue();
        dto.OverallStatus.Should().Be("Warning");
    }

    [Fact]
    public void DetectedDeviceDto_Constructor_SetsAllProperties()
    {
        // Act
        var dto = new DetectedDeviceDto("BME280", "I2C", "0x76", "OK", "temperature", 1, null);

        // Assert
        dto.DeviceType.Should().Be("BME280");
        dto.Bus.Should().Be("I2C");
        dto.Address.Should().Be("0x76");
        dto.Status.Should().Be("OK");
        dto.SensorCode.Should().Be("temperature");
        dto.EndpointId.Should().Be(1);
        dto.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void DetectedDeviceDto_WithError_SetsErrorMessage()
    {
        // Act
        var dto = new DetectedDeviceDto("DS18B20", "OneWire", "28:FF:...", "Error", null, null, "Sensor not responding");

        // Assert
        dto.Status.Should().Be("Error");
        dto.SensorCode.Should().BeNull();
        dto.EndpointId.Should().BeNull();
        dto.ErrorMessage.Should().Be("Sensor not responding");
    }

    [Fact]
    public void StorageStatusDto_Constructor_SetsAllProperties()
    {
        // Arrange
        var lastSyncAt = DateTime.UtcNow.AddMinutes(-10);

        // Act
        var dto = new StorageStatusDto(true, "LOCAL_AND_REMOTE", 16000000000, 1000000000, 15000000000, 5, lastSyncAt, null);

        // Assert
        dto.Available.Should().BeTrue();
        dto.Mode.Should().Be("LOCAL_AND_REMOTE");
        dto.TotalBytes.Should().Be(16000000000);
        dto.UsedBytes.Should().Be(1000000000);
        dto.FreeBytes.Should().Be(15000000000);
        dto.PendingSyncCount.Should().Be(5);
        dto.LastSyncAt.Should().Be(lastSyncAt);
        dto.LastSyncError.Should().BeNull();
    }

    [Fact]
    public void StorageStatusDto_WithError_SetsLastSyncError()
    {
        // Act
        var dto = new StorageStatusDto(true, "LOCAL_ONLY", 16000000000, 1000000000, 15000000000, 10, null, "Network unavailable");

        // Assert
        dto.LastSyncAt.Should().BeNull();
        dto.LastSyncError.Should().Be("Network unavailable");
    }

    [Fact]
    public void BusStatusDto_Constructor_SetsAllProperties()
    {
        // Act
        var dto = new BusStatusDto(true, 2, new List<string> { "0x76", "0x77" }, true, 3, true, true);

        // Assert
        dto.I2cAvailable.Should().BeTrue();
        dto.I2cDeviceCount.Should().Be(2);
        dto.I2cAddresses.Should().HaveCount(2);
        dto.I2cAddresses.Should().Contain("0x76");
        dto.I2cAddresses.Should().Contain("0x77");
        dto.OneWireAvailable.Should().BeTrue();
        dto.OneWireDeviceCount.Should().Be(3);
        dto.UartAvailable.Should().BeTrue();
        dto.GpsDetected.Should().BeTrue();
    }

    [Fact]
    public void BusStatusDto_NoBusesAvailable_SetsAllFalse()
    {
        // Act
        var dto = new BusStatusDto(false, 0, new List<string>(), false, 0, false, false);

        // Assert
        dto.I2cAvailable.Should().BeFalse();
        dto.I2cDeviceCount.Should().Be(0);
        dto.I2cAddresses.Should().BeEmpty();
        dto.OneWireAvailable.Should().BeFalse();
        dto.OneWireDeviceCount.Should().Be(0);
        dto.UartAvailable.Should().BeFalse();
        dto.GpsDetected.Should().BeFalse();
    }

    #endregion

    #region ReportHardwareStatusDto Tests

    [Fact]
    public void ReportHardwareStatusDto_Constructor_SetsAllProperties()
    {
        // Arrange
        var detectedDevices = new List<DetectedDeviceDto>
        {
            new("BME280", "I2C", "0x76", "OK", "temperature", 1, null),
            new("SDS011", "UART", "Serial2", "OK", "pm25", 2, null)
        };
        var storage = new StorageStatusDto(true, "REMOTE_ONLY", 16000000000, 500000000, 15500000000, 0, DateTime.UtcNow, null);
        var busStatus = new BusStatusDto(true, 1, new List<string> { "0x76" }, false, 0, true, false);

        // Act
        var dto = new ReportHardwareStatusDto(
            "ESP32-001",
            "1.2.3",
            "ESP32-S3",
            detectedDevices,
            storage,
            busStatus
        );

        // Assert
        dto.SerialNumber.Should().Be("ESP32-001");
        dto.FirmwareVersion.Should().Be("1.2.3");
        dto.HardwareType.Should().Be("ESP32-S3");
        dto.DetectedDevices.Should().HaveCount(2);
        dto.Storage.Should().Be(storage);
        dto.BusStatus.Should().Be(busStatus);
    }

    [Fact]
    public void ReportHardwareStatusDto_WithEmptyDevices_IsValid()
    {
        // Arrange
        var storage = new StorageStatusDto(false, "REMOTE_ONLY", 0, 0, 0, 0, null, null);
        var busStatus = new BusStatusDto(false, 0, new List<string>(), false, 0, false, false);

        // Act
        var dto = new ReportHardwareStatusDto(
            "ESP32-002",
            "1.0.0",
            "ESP32-WROOM-32",
            new List<DetectedDeviceDto>(),
            storage,
            busStatus
        );

        // Assert
        dto.DetectedDevices.Should().BeEmpty();
        dto.Storage.Available.Should().BeFalse();
    }

    #endregion

    #region SensorWidgetDto Tests

    [Fact]
    public void SensorWidgetDto_Constructor_SetsAllProperties()
    {
        // Arrange
        var assignmentId = Guid.NewGuid();
        var sensorId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();
        var lastUpdate = DateTime.UtcNow;
        var minTimestamp = DateTime.UtcNow.AddHours(-2);
        var maxTimestamp = DateTime.UtcNow.AddHours(-1);
        var minMax = new MinMaxDto(18.0, minTimestamp, 25.0, maxTimestamp);
        var dataPoints = new List<SparklinePointDto>
        {
            new(DateTime.UtcNow, 21.5)
        };

        // Act
        var dto = new SensorWidgetDto(
            "widget-001",
            nodeId,
            "Node 1",
            assignmentId,
            sensorId,
            "temperature",
            "DHT22",
            "Living Room",
            "DHT22 Temperature",
            "°C",
            "#FF5722",
            21.5,
            lastUpdate,
            minMax,
            dataPoints
        );

        // Assert
        dto.WidgetId.Should().Be("widget-001");
        dto.NodeId.Should().Be(nodeId);
        dto.NodeName.Should().Be("Node 1");
        dto.AssignmentId.Should().Be(assignmentId);
        dto.SensorId.Should().Be(sensorId);
        dto.MeasurementType.Should().Be("temperature");
        dto.SensorName.Should().Be("DHT22");
        dto.LocationName.Should().Be("Living Room");
        dto.Label.Should().Be("DHT22 Temperature");
        dto.Unit.Should().Be("°C");
        dto.Color.Should().Be("#FF5722");
        dto.CurrentValue.Should().Be(21.5);
        dto.LastUpdate.Should().Be(lastUpdate);
        dto.MinMax.Should().Be(minMax);
        dto.DataPoints.Should().HaveCount(1);
    }

    [Fact]
    public void SensorWidgetDto_WithNullOptionalProperties_IsValid()
    {
        // Arrange
        var minMax = new MinMaxDto(15.0, DateTime.UtcNow.AddHours(-1), 25.0, DateTime.UtcNow);

        // Act
        var dto = new SensorWidgetDto(
            "widget-002",
            Guid.NewGuid(),
            "Node",
            null,
            null,
            "temp",
            "Sensor",
            "Room",
            "Label",
            "°C",
            "#000",
            20.0,
            DateTime.UtcNow,
            minMax,
            new List<SparklinePointDto>()
        );

        // Assert
        dto.AssignmentId.Should().BeNull();
        dto.SensorId.Should().BeNull();
    }

    [Fact]
    public void LocationGroupDto_Constructor_SetsAllProperties()
    {
        // Arrange
        var widgets = new List<SensorWidgetDto>();

        // Act
        var dto = new LocationGroupDto(
            "Living Room",
            "🏠",
            true,
            widgets
        );

        // Assert
        dto.LocationName.Should().Be("Living Room");
        dto.LocationIcon.Should().Be("🏠");
        dto.IsHero.Should().BeTrue();
        dto.Widgets.Should().BeEmpty();
    }

    [Fact]
    public void LocationDashboardDto_Constructor_SetsAllProperties()
    {
        // Arrange
        var locations = new List<LocationGroupDto>();

        // Act
        var dto = new LocationDashboardDto(locations);

        // Assert
        dto.Locations.Should().BeEmpty();
    }

    [Fact]
    public void DashboardFilterOptionsDto_Constructor_SetsAllProperties()
    {
        // Arrange
        var locations = new List<string> { "Living Room", "Kitchen" };
        var types = new List<string> { "temperature", "humidity" };

        // Act
        var dto = new DashboardFilterOptionsDto(locations, types);

        // Assert
        dto.Locations.Should().HaveCount(2);
        dto.MeasurementTypes.Should().HaveCount(2);
    }

    [Fact]
    public void DashboardFilterDto_WithDefaultValues_UsesDefaults()
    {
        // Act
        var dto = new DashboardFilterDto();

        // Assert
        dto.Locations.Should().BeNull();
        dto.MeasurementTypes.Should().BeNull();
        dto.Period.Should().Be(SparklinePeriod.Day);
    }

    #endregion

    #region Additional Coverage Tests

    [Fact]
    public void ConnectionConfigDto_Constructor_SetsAllProperties()
    {
        // Act
        var dto = new ConnectionConfigDto("REST", "https://hub.local:5000/api");

        // Assert
        dto.Mode.Should().Be("REST");
        dto.Endpoint.Should().Be("https://hub.local:5000/api");
    }

    [Fact]
    public void SensorConfigDto_Constructor_SetsAllProperties()
    {
        // Act
        var dto = new SensorConfigDto("temperature", true, 4);

        // Assert
        dto.Type.Should().Be("temperature");
        dto.Enabled.Should().BeTrue();
        dto.Pin.Should().Be(4);
    }

    [Fact]
    public void SensorConfigDto_WithDefaultPin_UsesDefaultValue()
    {
        // Act
        var dto = new SensorConfigDto("humidity", false);

        // Assert
        dto.Type.Should().Be("humidity");
        dto.Enabled.Should().BeFalse();
        dto.Pin.Should().Be(-1);
    }

    [Fact]
    public void NodeRegistrationResponseDto_Constructor_SetsAllProperties()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var sensors = new List<SensorConfigDto>
        {
            new("temperature", true, 21)
        };
        var connection = new ConnectionConfigDto("REST", "https://hub.local/api");

        // Act
        var dto = new NodeRegistrationResponseDto(
            nodeId,
            "ESP32-001",
            "Living Room Sensor",
            "Living Room",
            60,
            sensors,
            connection,
            true,
            "Node registered successfully"
        );

        // Assert
        dto.NodeId.Should().Be(nodeId);
        dto.SerialNumber.Should().Be("ESP32-001");
        dto.Name.Should().Be("Living Room Sensor");
        dto.Location.Should().Be("Living Room");
        dto.IntervalSeconds.Should().Be(60);
        dto.Sensors.Should().HaveCount(1);
        dto.Connection.Should().Be(connection);
        dto.IsNewNode.Should().BeTrue();
        dto.Message.Should().Be("Node registered successfully");
    }

    [Fact]
    public void NodeSensorConfigurationDto_Constructor_SetsAllProperties()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var configTimestamp = DateTime.UtcNow;
        var sensors = new List<SensorAssignmentConfigDto>
        {
            new(1, "DHT22", "DHT22 Sensor", "thermometer", "#FF5722", true, 60, null, null, null, null, null, null, null, null, 0.0, 1.0, new List<SensorCapabilityConfigDto>())
        };

        // Act
        var dto = new NodeSensorConfigurationDto(
            nodeId,
            "ESP32-001",
            "Sensor Node",
            false,
            30,
            sensors,
            configTimestamp
        );

        // Assert
        dto.NodeId.Should().Be(nodeId);
        dto.SerialNumber.Should().Be("ESP32-001");
        dto.Name.Should().Be("Sensor Node");
        dto.IsSimulation.Should().BeFalse();
        dto.DefaultIntervalSeconds.Should().Be(30);
        dto.Sensors.Should().HaveCount(1);
        dto.ConfigurationTimestamp.Should().Be(configTimestamp);
    }

    [Fact]
    public void CreateSensorReadingDto_Constructor_SetsAllProperties()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Act
        var dto = new CreateSensorReadingDto("ESP32-001", "temperature", 21.5, "°C", timestamp, 1);

        // Assert
        dto.DeviceId.Should().Be("ESP32-001");
        dto.Type.Should().Be("temperature");
        dto.Value.Should().Be(21.5);
        dto.Unit.Should().Be("°C");
        dto.Timestamp.Should().Be(timestamp);
        dto.EndpointId.Should().Be(1);
    }

    [Fact]
    public void CreateSensorReadingDto_WithNullOptionalProperties_IsValid()
    {
        // Act
        var dto = new CreateSensorReadingDto("ESP32-002", "humidity", 65.0);

        // Assert
        dto.Unit.Should().BeNull();
        dto.Timestamp.Should().BeNull();
        dto.EndpointId.Should().BeNull();
    }

    #endregion
}
