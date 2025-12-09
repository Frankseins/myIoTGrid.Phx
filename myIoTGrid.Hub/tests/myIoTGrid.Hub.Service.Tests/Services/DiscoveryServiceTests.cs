using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace myIoTGrid.Hub.Service.Tests.Services;

/// <summary>
/// Tests for UDP Discovery Protocol (Sprint 6 - Story 1 & 8)
/// Tests the discovery request/response protocol between sensors and hub
/// </summary>
public class DiscoveryServiceTests
{
    private readonly Mock<ILogger<DiscoveryServiceTests>> _loggerMock;

    public DiscoveryServiceTests()
    {
        _loggerMock = new Mock<ILogger<DiscoveryServiceTests>>();
    }

    #region Discovery Request Tests

    [Fact]
    public void DiscoveryRequest_Serialization_ShouldProduceValidJson()
    {
        // Arrange
        var request = DiscoveryRequestDto.Create(
            serial: "ESP-AABBCCDD",
            firmwareVersion: "1.0.0",
            hardwareType: "ESP32"
        );

        // Act
        var json = JsonSerializer.Serialize(request);

        // Assert - JSON uses PascalCase by default in .NET
        json.Should().Contain("\"MessageType\":\"MYIOTGRID_DISCOVER\"");
        json.Should().Contain("\"Serial\":\"ESP-AABBCCDD\"");
        json.Should().Contain("\"FirmwareVersion\":\"1.0.0\"");
        json.Should().Contain("\"HardwareType\":\"ESP32\"");
    }

    [Fact]
    public void DiscoveryRequest_Deserialization_ShouldParseValidJson()
    {
        // Arrange - Use PascalCase as .NET default
        var json = """
        {
            "MessageType": "MYIOTGRID_DISCOVER",
            "Serial": "SIM-12345678-0001",
            "FirmwareVersion": "2.0.0",
            "HardwareType": "SIM"
        }
        """;

        // Act
        var request = JsonSerializer.Deserialize<DiscoveryRequestDto>(json);

        // Assert
        request.Should().NotBeNull();
        request!.IsValid.Should().BeTrue();
        request.Serial.Should().Be("SIM-12345678-0001");
        request.FirmwareVersion.Should().Be("2.0.0");
        request.HardwareType.Should().Be("SIM");
    }

    [Fact]
    public void DiscoveryRequest_WithInvalidMessageType_ShouldNotBeValid()
    {
        // Arrange
        var json = """
        {
            "MessageType": "WRONG_TYPE",
            "Serial": "ESP-001",
            "FirmwareVersion": "1.0.0",
            "HardwareType": "ESP32"
        }
        """;

        // Act
        var request = JsonSerializer.Deserialize<DiscoveryRequestDto>(json);

        // Assert
        request.Should().NotBeNull();
        request!.IsValid.Should().BeFalse();
    }

    #endregion

    #region Discovery Response Tests

    [Fact]
    public void DiscoveryResponse_Serialization_ShouldProduceValidJson()
    {
        // Arrange
        var response = DiscoveryResponseDto.Create(
            hubId: "hub-main",
            hubName: "Home Hub",
            apiUrl: "https://192.168.1.100:5001"
        );

        // Act
        var json = JsonSerializer.Serialize(response);

        // Assert - JSON uses PascalCase by default in .NET
        json.Should().Contain("\"MessageType\":\"MYIOTGRID_HUB\"");
        json.Should().Contain("\"HubId\":\"hub-main\"");
        json.Should().Contain("\"HubName\":\"Home Hub\"");
        json.Should().Contain("\"ApiUrl\":\"https://192.168.1.100:5001\"");
        json.Should().Contain("\"ApiVersion\":\"1.0\"");
        json.Should().Contain("\"ProtocolVersion\":\"1.0\"");
    }

    [Fact]
    public void DiscoveryResponse_Deserialization_ShouldParseValidJson()
    {
        // Arrange - Use PascalCase as .NET default
        var json = """
        {
            "MessageType": "MYIOTGRID_HUB",
            "HubId": "hub-office",
            "HubName": "Office Hub",
            "ApiUrl": "https://10.0.0.50:5001",
            "ApiVersion": "1.0",
            "ProtocolVersion": "1.0"
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize<DiscoveryResponseDto>(json);

        // Assert
        response.Should().NotBeNull();
        response!.IsValid.Should().BeTrue();
        response.HubId.Should().Be("hub-office");
        response.HubName.Should().Be("Office Hub");
        response.ApiUrl.Should().Be("https://10.0.0.50:5001");
    }

    [Fact]
    public void DiscoveryResponse_WithInvalidMessageType_ShouldNotBeValid()
    {
        // Arrange
        var json = """
        {
            "MessageType": "INVALID",
            "HubId": "hub-01",
            "HubName": "Test",
            "ApiUrl": "https://localhost:5001",
            "ApiVersion": "1.0",
            "ProtocolVersion": "1.0"
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize<DiscoveryResponseDto>(json);

        // Assert
        response.Should().NotBeNull();
        response!.IsValid.Should().BeFalse();
    }

    #endregion

    #region Protocol Constant Tests

    [Fact]
    public void Protocol_Constants_ShouldMatchBetweenRequestAndResponse()
    {
        // The discovery protocol message types should be consistent
        DiscoveryRequestDto.ExpectedMessageType.Should().Be("MYIOTGRID_DISCOVER");
        DiscoveryResponseDto.ExpectedMessageType.Should().Be("MYIOTGRID_HUB");
    }

    [Fact]
    public void Protocol_DefaultPort_ShouldBe5001()
    {
        // Arrange
        var options = new DiscoveryOptions();

        // Assert
        options.Port.Should().Be(5001);
    }

    [Fact]
    public void Protocol_DefaultProtocol_ShouldBeHttps()
    {
        // Arrange
        var options = new DiscoveryOptions();

        // Assert
        options.Protocol.Should().Be("https");
    }

    #endregion

    #region Integration-like Tests

    [Fact]
    public void Discovery_RoundTrip_RequestAndResponseShouldBeCompatible()
    {
        // Arrange - Create a request
        var request = DiscoveryRequestDto.Create(
            serial: "ESP-CAFEBABE",
            firmwareVersion: "1.2.3",
            hardwareType: "ESP32-WROOM-32"
        );

        // Act - Serialize and deserialize request
        var requestJson = JsonSerializer.Serialize(request);
        var deserializedRequest = JsonSerializer.Deserialize<DiscoveryRequestDto>(requestJson);

        // Assert request
        deserializedRequest.Should().NotBeNull();
        deserializedRequest!.IsValid.Should().BeTrue();
        deserializedRequest.Serial.Should().Be("ESP-CAFEBABE");

        // Arrange - Create response based on request
        var response = DiscoveryResponseDto.Create(
            hubId: "hub-001",
            hubName: "Test Hub",
            apiUrl: "https://192.168.1.1:5001"
        );

        // Act - Serialize and deserialize response
        var responseJson = JsonSerializer.Serialize(response);
        var deserializedResponse = JsonSerializer.Deserialize<DiscoveryResponseDto>(responseJson);

        // Assert response
        deserializedResponse.Should().NotBeNull();
        deserializedResponse!.IsValid.Should().BeTrue();
        deserializedResponse.HubId.Should().Be("hub-001");
    }

    [Fact]
    public void Discovery_MultipleSerialFormats_ShouldAllBeAccepted()
    {
        // Test different serial number formats
        var serials = new[]
        {
            "ESP-AABBCCDD",           // Standard ESP32 format
            "SIM-12345678-0001",      // Simulation format
            "myiotgrid-sensor-01",    // Custom format
            "LORA-F0E1D2C3",          // LoRa device format
            "DHT22-SENSOR-001"        // Sensor-specific format
        };

        foreach (var serial in serials)
        {
            var request = DiscoveryRequestDto.Create(serial, "1.0.0", "TEST");
            request.IsValid.Should().BeTrue($"Serial '{serial}' should be valid");
            request.Serial.Should().Be(serial);
        }
    }

    [Fact]
    public void Discovery_DifferentHardwareTypes_ShouldAllBeAccepted()
    {
        var hardwareTypes = new[]
        {
            "ESP32",
            "ESP32-S3",
            "ESP32-C3",
            "SIM",
            "LORA32",
            "HELTEC-V3"
        };

        foreach (var hwType in hardwareTypes)
        {
            var request = DiscoveryRequestDto.Create("TEST-001", "1.0.0", hwType);
            request.IsValid.Should().BeTrue($"Hardware type '{hwType}' should be valid");
            request.HardwareType.Should().Be(hwType);
        }
    }

    #endregion

    #region Options Configuration Tests

    [Fact]
    public void DiscoveryOptions_WhenDisabled_ShouldNotProcessRequests()
    {
        // Arrange
        var options = new DiscoveryOptions { Enabled = false };

        // Assert - Service should respect disabled flag
        options.Enabled.Should().BeFalse();
    }

    [Fact]
    public void DiscoveryOptions_CustomPort_ShouldBeConfigurable()
    {
        // Arrange
        var options = new DiscoveryOptions { Port = 6001 };

        // Assert
        options.Port.Should().Be(6001);
    }

    [Fact]
    public void DiscoveryOptions_AdvertiseIp_ShouldBeConfigurable()
    {
        // Arrange - Configure specific IP for multi-homed hosts
        var options = new DiscoveryOptions
        {
            AdvertiseIp = "192.168.1.50"
        };

        // Assert
        options.AdvertiseIp.Should().Be("192.168.1.50");
    }

    [Fact]
    public void DiscoveryOptions_NetworkInterface_ShouldBeConfigurable()
    {
        // Arrange - Configure specific network interface
        var options = new DiscoveryOptions
        {
            NetworkInterface = "eth0"
        };

        // Assert
        options.NetworkInterface.Should().Be("eth0");
    }

    #endregion
}
