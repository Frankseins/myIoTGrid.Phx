using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using myIoTGrid.Hub.Infrastructure.Matter;

namespace myIoTGrid.Hub.Service.Tests.Matter;

public class MatterBridgeClientTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _handlerMock;
    private readonly HttpClient _httpClient;
    private readonly Mock<ILogger<MatterBridgeClient>> _loggerMock;
    private readonly MatterBridgeOptions _options;
    private MatterBridgeClient _sut = null!;

    public MatterBridgeClientTests()
    {
        _handlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_handlerMock.Object);
        _loggerMock = new Mock<ILogger<MatterBridgeClient>>();

        _options = new MatterBridgeOptions
        {
            Enabled = true,
            BaseUrl = "http://localhost:3000",
            TimeoutSeconds = 10,
            RetryCount = 3,
            RetryDelayMilliseconds = 100,
            EnabledSensorTypes = ["temperature", "humidity"],
            EnableAlertSensors = true
        };
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private void CreateSut()
    {
        _sut = new MatterBridgeClient(
            _httpClient,
            Options.Create(_options),
            _loggerMock.Object);
    }

    private void SetupHttpResponse(HttpStatusCode statusCode, object? content = null)
    {
        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = content != null
                    ? JsonContent.Create(content)
                    : new StringContent("")
            });
    }

    private void SetupHttpException()
    {
        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));
    }

    #region IsAvailableAsync Tests

    [Fact]
    public async Task IsAvailableAsync_WhenDisabled_ReturnsFalse()
    {
        // Arrange
        _options.Enabled = false;
        CreateSut();

        // Act
        var result = await _sut.IsAvailableAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsAvailableAsync_WhenHealthEndpointSucceeds_ReturnsTrue()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK);
        CreateSut();

        // Act
        var result = await _sut.IsAvailableAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsAvailableAsync_WhenHealthEndpointFails_ReturnsFalse()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.ServiceUnavailable);
        CreateSut();

        // Act
        var result = await _sut.IsAvailableAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsAvailableAsync_WhenExceptionOccurs_ReturnsFalse()
    {
        // Arrange
        SetupHttpException();
        CreateSut();

        // Act
        var result = await _sut.IsAvailableAsync();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetStatusAsync Tests

    [Fact]
    public async Task GetStatusAsync_WhenDisabled_ReturnsNull()
    {
        // Arrange
        _options.Enabled = false;
        CreateSut();

        // Act
        var result = await _sut.GetStatusAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetStatusAsync_WhenSuccessful_ReturnsMatterBridgeStatus()
    {
        // Arrange
        var response = new
        {
            isStarted = true,
            deviceCount = 2,
            devices = new[]
            {
                new { sensorId = "s1", name = "Sensor 1", type = "temperature", location = (string?)"Living Room" },
                new { sensorId = "s2", name = "Sensor 2", type = "humidity", location = (string?)null }
            },
            pairingCode = 12345678,
            discriminator = 3840
        };

        SetupHttpResponse(HttpStatusCode.OK, response);
        CreateSut();

        // Act
        var result = await _sut.GetStatusAsync();

        // Assert
        result.Should().NotBeNull();
        result!.IsStarted.Should().BeTrue();
        result.DeviceCount.Should().Be(2);
        result.Devices.Should().HaveCount(2);
        result.PairingCode.Should().Be(12345678);
        result.Discriminator.Should().Be(3840);
    }

    [Fact]
    public async Task GetStatusAsync_WhenExceptionOccurs_ReturnsNull()
    {
        // Arrange
        SetupHttpException();
        CreateSut();

        // Act
        var result = await _sut.GetStatusAsync();

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region RegisterDeviceAsync Tests

    [Fact]
    public async Task RegisterDeviceAsync_WhenDisabled_ReturnsFalse()
    {
        // Arrange
        _options.Enabled = false;
        CreateSut();

        // Act
        var result = await _sut.RegisterDeviceAsync("sensor-1", "Sensor 1", "temperature");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RegisterDeviceAsync_WhenSuccessful_ReturnsTrue()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.Created);
        CreateSut();

        // Act
        var result = await _sut.RegisterDeviceAsync("sensor-1", "Sensor 1", "temperature", "Kitchen");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterDeviceAsync_WhenServerError_ReturnsFalse()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.InternalServerError);
        CreateSut();

        // Act
        var result = await _sut.RegisterDeviceAsync("sensor-1", "Sensor 1", "temperature");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region UpdateDeviceValueAsync Tests

    [Fact]
    public async Task UpdateDeviceValueAsync_WhenDisabled_ReturnsFalse()
    {
        // Arrange
        _options.Enabled = false;
        CreateSut();

        // Act
        var result = await _sut.UpdateDeviceValueAsync("sensor-1", "temperature", 21.5);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateDeviceValueAsync_WhenSensorTypeNotEnabled_ReturnsFalse()
    {
        // Arrange
        CreateSut();

        // Act - "pressure" is not in EnabledSensorTypes
        var result = await _sut.UpdateDeviceValueAsync("sensor-1", "pressure", 1013.25);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateDeviceValueAsync_WhenSuccessful_ReturnsTrue()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK);
        CreateSut();

        // Act
        var result = await _sut.UpdateDeviceValueAsync("sensor-1", "temperature", 22.5);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateDeviceValueAsync_WhenServerError_ReturnsFalse()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.NotFound);
        CreateSut();

        // Act
        var result = await _sut.UpdateDeviceValueAsync("sensor-1", "temperature", 23.0);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region RemoveDeviceAsync Tests

    [Fact]
    public async Task RemoveDeviceAsync_WhenDisabled_ReturnsFalse()
    {
        // Arrange
        _options.Enabled = false;
        CreateSut();

        // Act
        var result = await _sut.RemoveDeviceAsync("sensor-1");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveDeviceAsync_WhenSuccessful_ReturnsTrue()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.NoContent);
        CreateSut();

        // Act
        var result = await _sut.RemoveDeviceAsync("sensor-1");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveDeviceAsync_WhenNotFound_ReturnsFalse()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.NotFound);
        CreateSut();

        // Act
        var result = await _sut.RemoveDeviceAsync("nonexistent-sensor");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region SetContactSensorStateAsync Tests

    [Fact]
    public async Task SetContactSensorStateAsync_WhenDisabled_ReturnsFalse()
    {
        // Arrange
        _options.Enabled = false;
        CreateSut();

        // Act
        var result = await _sut.SetContactSensorStateAsync("alert-1", true);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SetContactSensorStateAsync_WhenAlertSensorsDisabled_ReturnsFalse()
    {
        // Arrange
        _options.EnableAlertSensors = false;
        CreateSut();

        // Act
        var result = await _sut.SetContactSensorStateAsync("alert-1", true);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SetContactSensorStateAsync_WhenSuccessful_ReturnsTrue()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK);
        CreateSut();

        // Act
        var result = await _sut.SetContactSensorStateAsync("alert-1", true);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SetContactSensorStateAsync_WhenServerError_ReturnsFalse()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.InternalServerError);
        CreateSut();

        // Act
        var result = await _sut.SetContactSensorStateAsync("alert-1", false);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetCommissionInfoAsync Tests

    [Fact]
    public async Task GetCommissionInfoAsync_WhenDisabled_ReturnsNull()
    {
        // Arrange
        _options.Enabled = false;
        CreateSut();

        // Act
        var result = await _sut.GetCommissionInfoAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCommissionInfoAsync_WhenSuccessful_ReturnsCommissionInfo()
    {
        // Arrange
        var response = new
        {
            pairingCode = 12345678,
            discriminator = 3840,
            manualPairingCode = "34970112332",
            qrCodeData = "MT:Y.K9042C00KA0648G00"
        };

        SetupHttpResponse(HttpStatusCode.OK, response);
        CreateSut();

        // Act
        var result = await _sut.GetCommissionInfoAsync();

        // Assert
        result.Should().NotBeNull();
        result!.PairingCode.Should().Be(12345678);
        result.Discriminator.Should().Be(3840);
        result.ManualPairingCode.Should().Be("34970112332");
        result.QrCodeData.Should().Be("MT:Y.K9042C00KA0648G00");
    }

    [Fact]
    public async Task GetCommissionInfoAsync_WhenExceptionOccurs_ReturnsNull()
    {
        // Arrange
        SetupHttpException();
        CreateSut();

        // Act
        var result = await _sut.GetCommissionInfoAsync();

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GenerateQrCodeAsync Tests

    [Fact]
    public async Task GenerateQrCodeAsync_WhenDisabled_ReturnsNull()
    {
        // Arrange
        _options.Enabled = false;
        CreateSut();

        // Act
        var result = await _sut.GenerateQrCodeAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GenerateQrCodeAsync_WhenSuccessful_ReturnsQrCodeInfo()
    {
        // Arrange
        var response = new
        {
            qrCodeData = "MT:Y.K9042C00KA0648G00",
            qrCodeImage = "data:image/png;base64,iVBORw0KGgoAAAA...",
            manualPairingCode = "34970112332"
        };

        SetupHttpResponse(HttpStatusCode.OK, response);
        CreateSut();

        // Act
        var result = await _sut.GenerateQrCodeAsync();

        // Assert
        result.Should().NotBeNull();
        result!.QrCodeData.Should().Be("MT:Y.K9042C00KA0648G00");
        result.QrCodeImage.Should().StartWith("data:image");
        result.ManualPairingCode.Should().Be("34970112332");
    }

    [Fact]
    public async Task GenerateQrCodeAsync_WhenExceptionOccurs_ReturnsNull()
    {
        // Arrange
        SetupHttpException();
        CreateSut();

        // Act
        var result = await _sut.GenerateQrCodeAsync();

        // Assert
        result.Should().BeNull();
    }

    #endregion
}
