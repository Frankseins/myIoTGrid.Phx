using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using myIoTGrid.Hub.Infrastructure.Data;
using myIoTGrid.Hub.Service.Services;

namespace myIoTGrid.Hub.Service.Tests.Services;

/// <summary>
/// Unit tests for NodeHardwareStatusService (Sprint 8).
/// </summary>
public class NodeHardwareStatusServiceTests : IDisposable
{
    private readonly HubDbContext _context;
    private readonly Mock<ILogger<NodeHardwareStatusService>> _loggerMock;
    private readonly NodeHardwareStatusService _service;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _hubId = Guid.NewGuid();

    public NodeHardwareStatusServiceTests()
    {
        var options = new DbContextOptionsBuilder<HubDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new HubDbContext(options);
        _loggerMock = new Mock<ILogger<NodeHardwareStatusService>>();

        _service = new NodeHardwareStatusService(_context, _loggerMock.Object);

        SeedDatabase();
    }

    private void SeedDatabase()
    {
        var tenant = new Tenant
        {
            Id = _tenantId,
            Name = "Test Tenant",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Tenants.Add(tenant);

        var hub = new HubEntity
        {
            Id = _hubId,
            TenantId = _tenantId,
            HubId = "test-hub",
            Name = "Test Hub",
            IsOnline = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Hubs.Add(hub);

        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region ReportHardwareStatusAsync Tests

    [Fact]
    public async Task ReportHardwareStatusAsync_WithValidMacAddress_ReturnsHardwareStatus()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var macAddress = "AA:BB:CC:DD:EE:FF";
        var node = new Node
        {
            Id = nodeId,
            HubId = _hubId,
            NodeId = "test-node-001",
            Name = "Test Node",
            MacAddress = macAddress,
            IsOnline = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Nodes.Add(node);
        await _context.SaveChangesAsync();

        var dto = CreateReportHardwareStatusDto(macAddress);

        // Act
        var result = await _service.ReportHardwareStatusAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result!.NodeId.Should().Be(nodeId);
        result.SerialNumber.Should().Be(macAddress);
        result.FirmwareVersion.Should().Be(dto.FirmwareVersion);
    }

    [Fact]
    public async Task ReportHardwareStatusAsync_WithValidNodeId_ReturnsHardwareStatus()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var nodeIdString = "ESP32-NODE-001";
        var node = new Node
        {
            Id = nodeId,
            HubId = _hubId,
            NodeId = nodeIdString,
            Name = "Test Node",
            MacAddress = "11:22:33:44:55:66",
            IsOnline = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Nodes.Add(node);
        await _context.SaveChangesAsync();

        var dto = CreateReportHardwareStatusDto(nodeIdString);

        // Act
        var result = await _service.ReportHardwareStatusAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result!.NodeId.Should().Be(nodeId);
    }

    [Fact]
    public async Task ReportHardwareStatusAsync_WithUnknownSerialNumber_ReturnsNull()
    {
        // Arrange
        var dto = CreateReportHardwareStatusDto("UNKNOWN-SERIAL-NUMBER");

        // Act
        var result = await _service.ReportHardwareStatusAsync(dto);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ReportHardwareStatusAsync_UpdatesFirmwareVersion()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var macAddress = "FF:EE:DD:CC:BB:AA";
        var node = new Node
        {
            Id = nodeId,
            HubId = _hubId,
            NodeId = "test-node-002",
            Name = "Test Node 2",
            MacAddress = macAddress,
            FirmwareVersion = "1.0.0",
            IsOnline = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Nodes.Add(node);
        await _context.SaveChangesAsync();

        var dto = CreateReportHardwareStatusDto(macAddress, firmwareVersion: "2.0.0");

        // Act
        await _service.ReportHardwareStatusAsync(dto);

        // Assert
        var updatedNode = await _context.Nodes.FindAsync(nodeId);
        updatedNode!.FirmwareVersion.Should().Be("2.0.0");
    }

    [Fact]
    public async Task ReportHardwareStatusAsync_UpdatesHardwareStatusReportedAt()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var macAddress = "12:34:56:78:9A:BC";
        var beforeTime = DateTime.UtcNow;
        var node = new Node
        {
            Id = nodeId,
            HubId = _hubId,
            NodeId = "test-node-003",
            Name = "Test Node 3",
            MacAddress = macAddress,
            IsOnline = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Nodes.Add(node);
        await _context.SaveChangesAsync();

        var dto = CreateReportHardwareStatusDto(macAddress);

        // Act
        await _service.ReportHardwareStatusAsync(dto);

        // Assert
        var updatedNode = await _context.Nodes.FindAsync(nodeId);
        updatedNode!.HardwareStatusReportedAt.Should().NotBeNull();
        updatedNode.HardwareStatusReportedAt.Should().BeAfter(beforeTime);
    }

    [Fact]
    public async Task ReportHardwareStatusAsync_StoresHardwareStatusJson()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var macAddress = "AB:CD:EF:01:23:45";
        var node = new Node
        {
            Id = nodeId,
            HubId = _hubId,
            NodeId = "test-node-004",
            Name = "Test Node 4",
            MacAddress = macAddress,
            IsOnline = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Nodes.Add(node);
        await _context.SaveChangesAsync();

        var dto = CreateReportHardwareStatusDto(macAddress);

        // Act
        await _service.ReportHardwareStatusAsync(dto);

        // Assert
        var updatedNode = await _context.Nodes.FindAsync(nodeId);
        updatedNode!.HardwareStatusJson.Should().NotBeNullOrEmpty();
        updatedNode.HardwareStatusJson.Should().Contain("detectedDevices");
    }

    [Fact]
    public async Task ReportHardwareStatusAsync_UpdatesSyncInfo()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var macAddress = "AA:11:BB:22:CC:33";
        var lastSyncAt = DateTime.UtcNow.AddHours(-1);
        var node = new Node
        {
            Id = nodeId,
            HubId = _hubId,
            NodeId = "test-node-005",
            Name = "Test Node 5",
            MacAddress = macAddress,
            IsOnline = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Nodes.Add(node);
        await _context.SaveChangesAsync();

        var storage = new StorageStatusDto(
            Available: true,
            Mode: "LOCAL_AND_REMOTE",
            TotalBytes: 4000000000,
            UsedBytes: 1000000000,
            FreeBytes: 3000000000,
            PendingSyncCount: 15,
            LastSyncAt: lastSyncAt,
            LastSyncError: null
        );
        var dto = CreateReportHardwareStatusDto(macAddress, storage: storage);

        // Act
        await _service.ReportHardwareStatusAsync(dto);

        // Assert
        var updatedNode = await _context.Nodes.FindAsync(nodeId);
        updatedNode!.PendingSyncCount.Should().Be(15);
        updatedNode.LastSyncAt.Should().Be(lastSyncAt);
        updatedNode.LastSyncError.Should().BeNull();
    }

    [Fact]
    public async Task ReportHardwareStatusAsync_UpdatesStorageMode()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var macAddress = "DD:44:EE:55:FF:66";
        var node = new Node
        {
            Id = nodeId,
            HubId = _hubId,
            NodeId = "test-node-006",
            Name = "Test Node 6",
            MacAddress = macAddress,
            StorageMode = StorageMode.RemoteOnly,
            IsOnline = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Nodes.Add(node);
        await _context.SaveChangesAsync();

        var storage = new StorageStatusDto(
            Available: true,
            Mode: "LocalAndRemote",
            TotalBytes: 4000000000,
            UsedBytes: 1000000000,
            FreeBytes: 3000000000,
            PendingSyncCount: 0,
            LastSyncAt: null,
            LastSyncError: null
        );
        var dto = CreateReportHardwareStatusDto(macAddress, storage: storage);

        // Act
        await _service.ReportHardwareStatusAsync(dto);

        // Assert
        var updatedNode = await _context.Nodes.FindAsync(nodeId);
        updatedNode!.StorageMode.Should().Be(StorageMode.LocalAndRemote);
    }

    [Fact]
    public async Task ReportHardwareStatusAsync_WithLastSyncError_StoresError()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var macAddress = "77:88:99:AA:BB:CC";
        var node = new Node
        {
            Id = nodeId,
            HubId = _hubId,
            NodeId = "test-node-007",
            Name = "Test Node 7",
            MacAddress = macAddress,
            IsOnline = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Nodes.Add(node);
        await _context.SaveChangesAsync();

        var storage = new StorageStatusDto(
            Available: true,
            Mode: "REMOTE_ONLY",
            TotalBytes: 4000000000,
            UsedBytes: 1000000000,
            FreeBytes: 3000000000,
            PendingSyncCount: 50,
            LastSyncAt: null,
            LastSyncError: "Connection timeout"
        );
        var dto = CreateReportHardwareStatusDto(macAddress, storage: storage);

        // Act
        await _service.ReportHardwareStatusAsync(dto);

        // Assert
        var updatedNode = await _context.Nodes.FindAsync(nodeId);
        updatedNode!.LastSyncError.Should().Be("Connection timeout");
    }

    #endregion

    #region GetHardwareStatusAsync Tests

    [Fact]
    public async Task GetHardwareStatusAsync_WithValidNodeId_ReturnsHardwareStatus()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var macAddress = "11:22:33:44:55:66";
        var node = new Node
        {
            Id = nodeId,
            HubId = _hubId,
            NodeId = "get-test-001",
            Name = "Get Test Node",
            MacAddress = macAddress,
            FirmwareVersion = "1.5.0",
            HardwareStatusReportedAt = DateTime.UtcNow.AddMinutes(-5),
            HardwareStatusJson = """{"detectedDevices":[{"deviceType":"BME280","bus":"I2C","address":"0x76","status":"OK","sensorCode":"temperature","endpointId":1,"errorMessage":null}],"storage":{"available":true,"mode":"REMOTE_ONLY","totalBytes":0,"usedBytes":0,"freeBytes":0,"pendingSyncCount":0,"lastSyncAt":null,"lastSyncError":null},"busStatus":{"i2cAvailable":true,"i2cDeviceCount":1,"i2cAddresses":["0x76"],"oneWireAvailable":false,"oneWireDeviceCount":0,"uartAvailable":true,"gpsDetected":false}}""",
            IsOnline = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _context.Nodes.Add(node);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetHardwareStatusAsync(nodeId);

        // Assert
        result.Should().NotBeNull();
        result!.NodeId.Should().Be(nodeId);
        result.SerialNumber.Should().Be(macAddress);
        result.FirmwareVersion.Should().Be("1.5.0");
        result.DetectedDevices.Should().HaveCount(1);
        result.DetectedDevices.First().DeviceType.Should().Be("BME280");
    }

    [Fact]
    public async Task GetHardwareStatusAsync_WithInvalidNodeId_ReturnsNull()
    {
        // Arrange
        var nonExistentNodeId = Guid.NewGuid();

        // Act
        var result = await _service.GetHardwareStatusAsync(nonExistentNodeId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetHardwareStatusAsync_WithNoHardwareStatusJson_ReturnsDefaults()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var node = new Node
        {
            Id = nodeId,
            HubId = _hubId,
            NodeId = "get-test-002",
            Name = "Node Without Status",
            MacAddress = "00:11:22:33:44:55",
            HardwareStatusJson = null,
            IsOnline = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Nodes.Add(node);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetHardwareStatusAsync(nodeId);

        // Assert
        result.Should().NotBeNull();
        result!.DetectedDevices.Should().BeEmpty();
        result.Storage.Available.Should().BeFalse();
        result.BusStatus.I2cAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task GetHardwareStatusAsync_CalculatesSummaryCorrectly()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var sensorId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();

        var sensor = new Sensor
        {
            Id = sensorId,
            Name = "BME280",
            Code = "bme280_001",
            CreatedAt = DateTime.UtcNow
        };
        _context.Sensors.Add(sensor);

        var node = new Node
        {
            Id = nodeId,
            HubId = _hubId,
            NodeId = "get-test-003",
            Name = "Node With Assignment",
            MacAddress = "66:77:88:99:AA:BB",
            HardwareStatusJson = """{"detectedDevices":[{"deviceType":"BME280","bus":"I2C","address":"0x76","status":"OK","sensorCode":"temperature","endpointId":1,"errorMessage":null}],"storage":{"available":true,"mode":"LOCAL_AND_REMOTE","totalBytes":4000000000,"usedBytes":1000000000,"freeBytes":3000000000,"pendingSyncCount":0,"lastSyncAt":null,"lastSyncError":null},"busStatus":{"i2cAvailable":true,"i2cDeviceCount":1,"i2cAddresses":["0x76"],"oneWireAvailable":false,"oneWireDeviceCount":0,"uartAvailable":true,"gpsDetected":false}}""",
            IsOnline = true,
            CreatedAt = DateTime.UtcNow,
            SensorAssignments = new List<NodeSensorAssignment>
            {
                new()
                {
                    Id = assignmentId,
                    NodeId = nodeId,
                    SensorId = sensorId,
                    EndpointId = 1,
                    IsActive = true,
                    AssignedAt = DateTime.UtcNow
                }
            }
        };
        _context.Nodes.Add(node);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetHardwareStatusAsync(nodeId);

        // Assert
        result.Should().NotBeNull();
        result!.Summary.TotalDevicesDetected.Should().Be(1);
        result.Summary.SensorsConfigured.Should().Be(1);
        result.Summary.SensorsOk.Should().Be(1);
        result.Summary.SensorsError.Should().Be(0);
        result.Summary.HasSdCard.Should().BeTrue();
        result.Summary.HasGps.Should().BeFalse();
        result.Summary.OverallStatus.Should().Be("OK");
    }

    [Fact]
    public async Task GetHardwareStatusAsync_WithErrorDevices_CalculatesErrorStatus()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var node = new Node
        {
            Id = nodeId,
            HubId = _hubId,
            NodeId = "get-test-004",
            Name = "Node With Errors",
            MacAddress = "CC:DD:EE:FF:00:11",
            HardwareStatusJson = """{"detectedDevices":[{"deviceType":"BME280","bus":"I2C","address":"0x76","status":"Error","sensorCode":"temperature","endpointId":1,"errorMessage":"I2C read failed"}],"storage":{"available":false,"mode":"REMOTE_ONLY","totalBytes":0,"usedBytes":0,"freeBytes":0,"pendingSyncCount":0,"lastSyncAt":null,"lastSyncError":null},"busStatus":{"i2cAvailable":true,"i2cDeviceCount":1,"i2cAddresses":["0x76"],"oneWireAvailable":false,"oneWireDeviceCount":0,"uartAvailable":false,"gpsDetected":false}}""",
            IsOnline = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Nodes.Add(node);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetHardwareStatusAsync(nodeId);

        // Assert
        result.Should().NotBeNull();
        result!.Summary.SensorsError.Should().Be(1);
        result.Summary.OverallStatus.Should().Be("Error");
    }

    [Fact]
    public async Task GetHardwareStatusAsync_WithGps_DetectsGps()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var node = new Node
        {
            Id = nodeId,
            HubId = _hubId,
            NodeId = "get-test-005",
            Name = "Node With GPS",
            MacAddress = "22:33:44:55:66:77",
            HardwareStatusJson = """{"detectedDevices":[{"deviceType":"GPS Module","bus":"UART","address":"Serial1","status":"OK","sensorCode":null,"endpointId":null,"errorMessage":null}],"storage":{"available":false,"mode":"REMOTE_ONLY","totalBytes":0,"usedBytes":0,"freeBytes":0,"pendingSyncCount":0,"lastSyncAt":null,"lastSyncError":null},"busStatus":{"i2cAvailable":false,"i2cDeviceCount":0,"i2cAddresses":[],"oneWireAvailable":false,"oneWireDeviceCount":0,"uartAvailable":true,"gpsDetected":true}}""",
            IsOnline = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Nodes.Add(node);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetHardwareStatusAsync(nodeId);

        // Assert
        result.Should().NotBeNull();
        result!.Summary.HasGps.Should().BeTrue();
    }

    [Fact]
    public async Task GetHardwareStatusAsync_WithInvalidJson_ReturnsDefaults()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var node = new Node
        {
            Id = nodeId,
            HubId = _hubId,
            NodeId = "get-test-006",
            Name = "Node With Invalid JSON",
            MacAddress = "88:99:AA:BB:CC:DD",
            HardwareStatusJson = "invalid json {{{",
            IsOnline = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Nodes.Add(node);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetHardwareStatusAsync(nodeId);

        // Assert
        result.Should().NotBeNull();
        result!.DetectedDevices.Should().BeEmpty();
        result.Storage.Available.Should().BeFalse();
    }

    #endregion

    #region GetHardwareStatusBySerialAsync Tests

    [Fact]
    public async Task GetHardwareStatusBySerialAsync_WithMacAddress_ReturnsHardwareStatus()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var macAddress = "AA:BB:CC:DD:EE:11";
        var node = new Node
        {
            Id = nodeId,
            HubId = _hubId,
            NodeId = "serial-test-001",
            Name = "Serial Test Node",
            MacAddress = macAddress,
            FirmwareVersion = "2.0.0",
            IsOnline = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Nodes.Add(node);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetHardwareStatusBySerialAsync(macAddress);

        // Assert
        result.Should().NotBeNull();
        result!.NodeId.Should().Be(nodeId);
        result.SerialNumber.Should().Be(macAddress);
    }

    [Fact]
    public async Task GetHardwareStatusBySerialAsync_WithNodeId_ReturnsHardwareStatus()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var nodeIdString = "ESP32-SERIAL-002";
        var node = new Node
        {
            Id = nodeId,
            HubId = _hubId,
            NodeId = nodeIdString,
            Name = "Serial Test Node 2",
            MacAddress = "11:22:33:44:55:22",
            FirmwareVersion = "2.1.0",
            IsOnline = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Nodes.Add(node);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetHardwareStatusBySerialAsync(nodeIdString);

        // Assert
        result.Should().NotBeNull();
        result!.NodeId.Should().Be(nodeId);
    }

    [Fact]
    public async Task GetHardwareStatusBySerialAsync_WithUnknownSerial_ReturnsNull()
    {
        // Act
        var result = await _service.GetHardwareStatusBySerialAsync("UNKNOWN-SERIAL");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetHardwareStatusBySerialAsync_IncludesSensorAssignments()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var sensorId = Guid.NewGuid();
        var macAddress = "FF:FF:00:00:11:11";

        var sensor = new Sensor
        {
            Id = sensorId,
            Name = "DHT22",
            Code = "dht22_001",
            CreatedAt = DateTime.UtcNow
        };
        _context.Sensors.Add(sensor);

        var node = new Node
        {
            Id = nodeId,
            HubId = _hubId,
            NodeId = "serial-test-003",
            Name = "Serial Test Node 3",
            MacAddress = macAddress,
            HardwareStatusJson = """{"detectedDevices":[{"deviceType":"DHT22","bus":"GPIO","address":"GPIO4","status":"OK","sensorCode":"temperature","endpointId":1,"errorMessage":null}],"storage":{"available":false,"mode":"REMOTE_ONLY","totalBytes":0,"usedBytes":0,"freeBytes":0,"pendingSyncCount":0,"lastSyncAt":null,"lastSyncError":null},"busStatus":{"i2cAvailable":false,"i2cDeviceCount":0,"i2cAddresses":[],"oneWireAvailable":false,"oneWireDeviceCount":0,"uartAvailable":false,"gpsDetected":false}}""",
            IsOnline = true,
            CreatedAt = DateTime.UtcNow,
            SensorAssignments = new List<NodeSensorAssignment>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    NodeId = nodeId,
                    SensorId = sensorId,
                    EndpointId = 1,
                    IsActive = true,
                    AssignedAt = DateTime.UtcNow
                }
            }
        };
        _context.Nodes.Add(node);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetHardwareStatusBySerialAsync(macAddress);

        // Assert
        result.Should().NotBeNull();
        result!.Summary.SensorsConfigured.Should().Be(1);
    }

    #endregion

    #region Helper Methods

    private static ReportHardwareStatusDto CreateReportHardwareStatusDto(
        string serialNumber,
        string firmwareVersion = "1.0.0",
        StorageStatusDto? storage = null,
        List<DetectedDeviceDto>? devices = null,
        BusStatusDto? busStatus = null)
    {
        var defaultStorage = new StorageStatusDto(
            Available: false,
            Mode: "REMOTE_ONLY",
            TotalBytes: 0,
            UsedBytes: 0,
            FreeBytes: 0,
            PendingSyncCount: 0,
            LastSyncAt: null,
            LastSyncError: null
        );

        var defaultDevices = new List<DetectedDeviceDto>
        {
            new(
                DeviceType: "BME280",
                Bus: "I2C",
                Address: "0x76",
                Status: "OK",
                SensorCode: "temperature",
                EndpointId: 1,
                ErrorMessage: null
            )
        };

        var defaultBusStatus = new BusStatusDto(
            I2cAvailable: true,
            I2cDeviceCount: 1,
            I2cAddresses: new List<string> { "0x76" },
            OneWireAvailable: false,
            OneWireDeviceCount: 0,
            UartAvailable: true,
            GpsDetected: false
        );

        return new ReportHardwareStatusDto(
            SerialNumber: serialNumber,
            FirmwareVersion: firmwareVersion,
            HardwareType: "ESP32",
            DetectedDevices: devices ?? defaultDevices,
            Storage: storage ?? defaultStorage,
            BusStatus: busStatus ?? defaultBusStatus
        );
    }

    #endregion
}
