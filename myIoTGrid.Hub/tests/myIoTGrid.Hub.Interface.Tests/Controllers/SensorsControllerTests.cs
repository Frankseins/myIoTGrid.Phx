using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using myIoTGrid.Hub.Interface.Controllers;

namespace myIoTGrid.Hub.Interface.Tests.Controllers;

/// <summary>
/// Unit tests for SensorsController
/// </summary>
public class SensorsControllerTests
{
    private readonly Mock<ISensorService> _sensorServiceMock;
    private readonly SensorsController _sut;

    private readonly Guid _tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly Guid _sensorId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    public SensorsControllerTests()
    {
        _sensorServiceMock = new Mock<ISensorService>();
        _sut = new SensorsController(_sensorServiceMock.Object);
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_ReturnsOkWithSensors()
    {
        // Arrange
        var sensors = new List<SensorDto>
        {
            CreateSensorDto("dht22", "DHT22 Sensor"),
            CreateSensorDto("bme280", "BME280 Sensor")
        };

        _sensorServiceMock.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(sensors);

        // Act
        var result = await _sut.GetAll(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(sensors);
    }

    [Fact]
    public async Task GetAll_WithEmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        _sensorServiceMock.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SensorDto>());

        // Act
        var result = await _sut.GetAll(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var sensors = okResult.Value as IEnumerable<SensorDto>;
        sensors.Should().BeEmpty();
    }

    #endregion

    #region GetPaged Tests

    [Fact]
    public async Task GetPaged_ReturnsOkWithPagedResult()
    {
        // Arrange
        var queryParams = new QueryParamsDto { Page = 1, Size = 10 };
        var sensors = new List<SensorDto> { CreateSensorDto("dht22", "DHT22") };
        var pagedResult = new PagedResultDto<SensorDto>
        {
            Items = sensors,
            TotalRecords = 1,
            Page = 1,
            Size = 10
        };

        _sensorServiceMock.Setup(s => s.GetPagedAsync(queryParams, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _sut.GetPaged(queryParams, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(pagedResult);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WithExistingSensor_ReturnsOk()
    {
        // Arrange
        var sensor = CreateSensorDto("dht22", "DHT22 Sensor");

        _sensorServiceMock.Setup(s => s.GetByIdAsync(_sensorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sensor);

        // Act
        var result = await _sut.GetById(_sensorId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(sensor);
    }

    [Fact]
    public async Task GetById_WithNonExistingSensor_ReturnsNotFound()
    {
        // Arrange
        _sensorServiceMock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SensorDto?)null);

        // Act
        var result = await _sut.GetById(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region GetByCode Tests

    [Fact]
    public async Task GetByCode_WithExistingCode_ReturnsOk()
    {
        // Arrange
        var sensor = CreateSensorDto("dht22", "DHT22 Sensor");

        _sensorServiceMock.Setup(s => s.GetByCodeAsync("dht22", It.IsAny<CancellationToken>()))
            .ReturnsAsync(sensor);

        // Act
        var result = await _sut.GetByCode("dht22", CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(sensor);
    }

    [Fact]
    public async Task GetByCode_WithNonExistingCode_ReturnsNotFound()
    {
        // Arrange
        _sensorServiceMock.Setup(s => s.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SensorDto?)null);

        // Act
        var result = await _sut.GetByCode("nonexistent", CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region GetByCategory Tests

    [Fact]
    public async Task GetByCategory_ReturnsOkWithSensors()
    {
        // Arrange
        var sensors = new List<SensorDto>
        {
            CreateSensorDto("dht22", "DHT22"),
            CreateSensorDto("bme280", "BME280")
        };

        _sensorServiceMock.Setup(s => s.GetByCategoryAsync("climate", It.IsAny<CancellationToken>()))
            .ReturnsAsync(sensors);

        // Act
        var result = await _sut.GetByCategory("climate", CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(sensors);
    }

    #endregion

    #region GetCapabilities Tests

    [Fact]
    public async Task GetCapabilities_ReturnsOkWithCapabilities()
    {
        // Arrange
        var capabilities = new List<SensorCapabilityDto>
        {
            new(
                Id: Guid.NewGuid(),
                SensorId: _sensorId,
                MeasurementType: "temperature",
                DisplayName: "Temperatur",
                Unit: "°C",
                MinValue: -40,
                MaxValue: 80,
                Resolution: 0.1,
                Accuracy: 0.5,
                MatterClusterId: 1026,
                MatterClusterName: "TemperatureMeasurement",
                SortOrder: 0,
                IsActive: true
            )
        };

        _sensorServiceMock.Setup(s => s.GetCapabilitiesAsync(_sensorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(capabilities);

        // Act
        var result = await _sut.GetCapabilities(_sensorId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(capabilities);
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WithValidData_ReturnsCreatedAtAction()
    {
        // Arrange
        var createDto = new CreateSensorDto(
            Code: "dht22",
            Name: "DHT22 Sensor",
            Protocol: CommunicationProtocolDto.Digital,
            Category: "climate",
            Description: "Temperature & Humidity"
        );

        var sensor = CreateSensorDto("dht22", "DHT22 Sensor");

        _sensorServiceMock.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sensor);

        // Act
        var result = await _sut.Create(createDto, CancellationToken.None);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(SensorsController.GetById));
        createdResult.Value.Should().BeEquivalentTo(sensor);
    }

    [Fact]
    public async Task Create_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateSensorDto(
            Code: "test",
            Name: "Test",
            Protocol: CommunicationProtocolDto.I2C,
            Category: "climate"
        );

        _sensorServiceMock.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Invalid sensor data"));

        // Act
        var result = await _sut.Create(createDto, CancellationToken.None);

        // Assert
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequest.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Detail.Should().Contain("Invalid sensor data");
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidData_ReturnsOk()
    {
        // Arrange
        var updateDto = new UpdateSensorDto(
            Name: "Updated Sensor",
            Description: "Updated description",
            IntervalSeconds: 120
        );

        var sensor = CreateSensorDto("dht22", "Updated Sensor");

        _sensorServiceMock.Setup(s => s.UpdateAsync(_sensorId, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sensor);

        // Act
        var result = await _sut.Update(_sensorId, updateDto, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(sensor);
    }

    [Fact]
    public async Task Update_WithNonExistingSensor_ReturnsNotFound()
    {
        // Arrange
        var updateDto = new UpdateSensorDto(Name: "Updated Sensor");

        _sensorServiceMock.Setup(s => s.UpdateAsync(It.IsAny<Guid>(), updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Sensor not found"));

        // Act
        var result = await _sut.Update(Guid.NewGuid(), updateDto, CancellationToken.None);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problemDetails = notFoundResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Detail.Should().Contain("Sensor not found");
    }

    #endregion

    #region Calibrate Tests

    [Fact]
    public async Task Calibrate_WithValidData_ReturnsOk()
    {
        // Arrange
        var calibrateDto = new CalibrateSensorDto(
            OffsetCorrection: 0.5,
            GainCorrection: 1.02,
            CalibrationNotes: "Calibrated with reference sensor"
        );

        var sensor = CreateSensorDto("dht22", "Calibrated Sensor");

        _sensorServiceMock.Setup(s => s.CalibrateAsync(_sensorId, calibrateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sensor);

        // Act
        var result = await _sut.Calibrate(_sensorId, calibrateDto, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(sensor);
    }

    [Fact]
    public async Task Calibrate_WithNonExistingSensor_ReturnsNotFound()
    {
        // Arrange
        var calibrateDto = new CalibrateSensorDto(OffsetCorrection: 0.5);

        _sensorServiceMock.Setup(s => s.CalibrateAsync(It.IsAny<Guid>(), calibrateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Sensor not found"));

        // Act
        var result = await _sut.Calibrate(Guid.NewGuid(), calibrateDto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WithExistingSensor_ReturnsNoContent()
    {
        // Arrange
        _sensorServiceMock.Setup(s => s.DeleteAsync(_sensorId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Delete(_sensorId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_WithNonExistingSensor_ReturnsNotFound()
    {
        // Arrange
        _sensorServiceMock.Setup(s => s.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Sensor not found"));

        // Act
        var result = await _sut.Delete(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Delete_WithSensorHavingAssignments_ReturnsBadRequest()
    {
        // Arrange
        _sensorServiceMock.Setup(s => s.DeleteAsync(_sensorId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cannot delete sensor with active assignment"));

        // Act
        var result = await _sut.Delete(_sensorId, CancellationToken.None);

        // Assert
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequest.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Detail.Should().Contain("assignment");
    }

    #endregion

    #region SeedDefaultSensors Tests

    [Fact]
    public async Task SeedDefaultSensors_ReturnsNoContent()
    {
        // Arrange
        _sensorServiceMock.Setup(s => s.SeedDefaultSensorsAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.SeedDefaultSensors(CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _sensorServiceMock.Verify(s => s.SeedDefaultSensorsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Helper Methods

    private SensorDto CreateSensorDto(string code, string name)
    {
        return new SensorDto(
            Id: _sensorId,
            TenantId: _tenantId,
            Code: code,
            Name: name,
            Description: $"Test sensor {name}",
            SerialNumber: null,
            Manufacturer: "Test",
            Model: "Model-1",
            DatasheetUrl: null,
            Protocol: CommunicationProtocolDto.Digital,
            I2CAddress: null,
            SdaPin: null,
            SclPin: null,
            OneWirePin: null,
            AnalogPin: null,
            DigitalPin: 4,
            TriggerPin: null,
            EchoPin: null,
            BaudRate: null,
            IntervalSeconds: 60,
            MinIntervalSeconds: 1,
            WarmupTimeMs: 0,
            OffsetCorrection: 0,
            GainCorrection: 1,
            LastCalibratedAt: null,
            CalibrationNotes: null,
            CalibrationDueAt: null,
            Category: "climate",
            Icon: "thermostat",
            Color: "#FF5722",
            Capabilities: new List<SensorCapabilityDto>(),
            IsActive: true,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow
        );
    }

    #endregion
}
