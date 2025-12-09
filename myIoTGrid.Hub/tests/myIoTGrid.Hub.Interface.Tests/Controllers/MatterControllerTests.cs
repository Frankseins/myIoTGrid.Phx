using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using myIoTGrid.Hub.Infrastructure.Matter;
using myIoTGrid.Hub.Interface.Controllers;

namespace myIoTGrid.Hub.Interface.Tests.Controllers;

/// <summary>
/// Tests for MatterController.
/// </summary>
public class MatterControllerTests
{
    private readonly Mock<IMatterBridgeClient> _matterClientMock;
    private readonly Mock<ILogger<MatterController>> _loggerMock;
    private readonly MatterController _sut;

    public MatterControllerTests()
    {
        _matterClientMock = new Mock<IMatterBridgeClient>();
        _loggerMock = new Mock<ILogger<MatterController>>();
        _sut = new MatterController(_matterClientMock.Object, _loggerMock.Object);
    }

    #region GetStatus Tests

    [Fact]
    public async Task GetStatus_WhenBridgeAvailable_ReturnsOkWithStatus()
    {
        // Arrange
        var status = new MatterBridgeStatus(
            IsStarted: true,
            DeviceCount: 5,
            Devices: new List<MatterDeviceInfo>(),
            PairingCode: 12345678,
            Discriminator: 3840
        );

        _matterClientMock.Setup(c => c.IsAvailableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _matterClientMock.Setup(c => c.GetStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(status);

        // Act
        var result = await _sut.GetStatus(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedStatus = okResult.Value.Should().BeOfType<MatterBridgeStatus>().Subject;
        returnedStatus.IsStarted.Should().BeTrue();
        returnedStatus.DeviceCount.Should().Be(5);
    }

    [Fact]
    public async Task GetStatus_WhenBridgeNotAvailable_Returns503()
    {
        // Arrange
        _matterClientMock.Setup(c => c.IsAvailableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.GetStatus(CancellationToken.None);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

    [Fact]
    public async Task GetStatus_WhenStatusIsNull_Returns503()
    {
        // Arrange
        _matterClientMock.Setup(c => c.IsAvailableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _matterClientMock.Setup(c => c.GetStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((MatterBridgeStatus?)null);

        // Act
        var result = await _sut.GetStatus(CancellationToken.None);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

    #endregion

    #region GetCommissionInfo Tests

    [Fact]
    public async Task GetCommissionInfo_WhenBridgeAvailable_ReturnsOkWithInfo()
    {
        // Arrange
        var commissionInfo = new MatterCommissionInfo(
            PairingCode: 12345678,
            Discriminator: 3840,
            ManualPairingCode: "11111111111",
            QrCodeData: "MT:Y.K9042C00KA0648G00"
        );

        _matterClientMock.Setup(c => c.IsAvailableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _matterClientMock.Setup(c => c.GetCommissionInfoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(commissionInfo);

        // Act
        var result = await _sut.GetCommissionInfo(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedInfo = okResult.Value.Should().BeOfType<MatterCommissionInfo>().Subject;
        returnedInfo.PairingCode.Should().Be(12345678);
    }

    [Fact]
    public async Task GetCommissionInfo_WhenBridgeNotAvailable_Returns503()
    {
        // Arrange
        _matterClientMock.Setup(c => c.IsAvailableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.GetCommissionInfo(CancellationToken.None);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

    [Fact]
    public async Task GetCommissionInfo_WhenInfoIsNull_Returns503()
    {
        // Arrange
        _matterClientMock.Setup(c => c.IsAvailableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _matterClientMock.Setup(c => c.GetCommissionInfoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((MatterCommissionInfo?)null);

        // Act
        var result = await _sut.GetCommissionInfo(CancellationToken.None);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

    #endregion

    #region GenerateQrCode Tests

    [Fact]
    public async Task GenerateQrCode_WhenBridgeAvailable_ReturnsOkWithQrCode()
    {
        // Arrange
        var qrCode = new MatterQrCodeInfo(
            QrCodeData: "MT:Y.K9042C00KA0648G00",
            QrCodeImage: "iVBORw0KGgoAAAANSUhEUgAAA...",
            ManualPairingCode: "11111111111"
        );

        _matterClientMock.Setup(c => c.IsAvailableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _matterClientMock.Setup(c => c.GenerateQrCodeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(qrCode);

        // Act
        var result = await _sut.GenerateQrCode(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedQr = okResult.Value.Should().BeOfType<MatterQrCodeInfo>().Subject;
        returnedQr.QrCodeData.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GenerateQrCode_WhenBridgeNotAvailable_Returns503()
    {
        // Arrange
        _matterClientMock.Setup(c => c.IsAvailableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.GenerateQrCode(CancellationToken.None);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

    [Fact]
    public async Task GenerateQrCode_WhenQrCodeIsNull_Returns503()
    {
        // Arrange
        _matterClientMock.Setup(c => c.IsAvailableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _matterClientMock.Setup(c => c.GenerateQrCodeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((MatterQrCodeInfo?)null);

        // Act
        var result = await _sut.GenerateQrCode(CancellationToken.None);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

    #endregion

    #region GetDevices Tests

    [Fact]
    public async Task GetDevices_WhenBridgeAvailable_ReturnsOkWithDevices()
    {
        // Arrange
        var devices = new List<MatterDeviceInfo>
        {
            new MatterDeviceInfo("device-1", "Temperature Sensor", "TemperatureSensor", "Wohnzimmer"),
            new MatterDeviceInfo("device-2", "Humidity Sensor", "HumiditySensor", "Küche")
        };
        var status = new MatterBridgeStatus(
            IsStarted: true,
            DeviceCount: 2,
            Devices: devices,
            PairingCode: 12345678,
            Discriminator: 3840
        );

        _matterClientMock.Setup(c => c.IsAvailableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _matterClientMock.Setup(c => c.GetStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(status);

        // Act
        var result = await _sut.GetDevices(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedDevices = okResult.Value.Should().BeAssignableTo<IEnumerable<MatterDeviceInfo>>().Subject;
        returnedDevices.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetDevices_WhenBridgeNotAvailable_Returns503()
    {
        // Arrange
        _matterClientMock.Setup(c => c.IsAvailableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.GetDevices(CancellationToken.None);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

    [Fact]
    public async Task GetDevices_WhenStatusIsNull_Returns503()
    {
        // Arrange
        _matterClientMock.Setup(c => c.IsAvailableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _matterClientMock.Setup(c => c.GetStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((MatterBridgeStatus?)null);

        // Act
        var result = await _sut.GetDevices(CancellationToken.None);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

    #endregion

    #region HealthCheck Tests

    [Fact]
    public async Task HealthCheck_WhenBridgeAvailable_ReturnsOkWithHealthy()
    {
        // Arrange
        _matterClientMock.Setup(c => c.IsAvailableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.HealthCheck(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        // Anonymous type, so we check it's not null
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task HealthCheck_WhenBridgeNotAvailable_Returns503()
    {
        // Arrange
        _matterClientMock.Setup(c => c.IsAvailableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.HealthCheck(CancellationToken.None);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

    #endregion
}
