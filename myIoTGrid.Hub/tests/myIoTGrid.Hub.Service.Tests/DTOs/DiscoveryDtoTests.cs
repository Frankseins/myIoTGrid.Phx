using FluentAssertions;

namespace myIoTGrid.Hub.Service.Tests.DTOs;

#region DiscoveryRequestDto Tests

/// <summary>
/// Tests for DiscoveryRequestDto - UDP discovery request from sensors
/// </summary>
public class DiscoveryRequestDtoTests
{
    [Fact]
    public void DiscoveryRequestDto_ShouldBeCreatedWithAllProperties()
    {
        // Arrange & Act
        var dto = new DiscoveryRequestDto(
            MessageType: "MYIOTGRID_DISCOVER",
            Serial: "SIM-12345678-0001",
            FirmwareVersion: "1.0.0",
            HardwareType: "ESP32"
        );

        // Assert
        dto.MessageType.Should().Be("MYIOTGRID_DISCOVER");
        dto.Serial.Should().Be("SIM-12345678-0001");
        dto.FirmwareVersion.Should().Be("1.0.0");
        dto.HardwareType.Should().Be("ESP32");
    }

    [Fact]
    public void DiscoveryRequestDto_IsValid_ShouldReturnTrueForCorrectMessageType()
    {
        // Arrange
        var dto = DiscoveryRequestDto.Create("SIM-001", "1.0.0", "ESP32");

        // Assert
        dto.IsValid.Should().BeTrue();
        dto.MessageType.Should().Be(DiscoveryRequestDto.ExpectedMessageType);
    }

    [Fact]
    public void DiscoveryRequestDto_IsValid_ShouldReturnFalseForIncorrectMessageType()
    {
        // Arrange
        var dto = new DiscoveryRequestDto(
            MessageType: "INVALID_TYPE",
            Serial: "SIM-001",
            FirmwareVersion: "1.0.0",
            HardwareType: "ESP32"
        );

        // Assert
        dto.IsValid.Should().BeFalse();
    }

    [Fact]
    public void DiscoveryRequestDto_Create_ShouldSetExpectedMessageType()
    {
        // Act
        var dto = DiscoveryRequestDto.Create(
            serial: "ESP-AABBCCDD",
            firmwareVersion: "2.1.0",
            hardwareType: "SIM"
        );

        // Assert
        dto.MessageType.Should().Be("MYIOTGRID_DISCOVER");
        dto.Serial.Should().Be("ESP-AABBCCDD");
        dto.FirmwareVersion.Should().Be("2.1.0");
        dto.HardwareType.Should().Be("SIM");
    }

    [Fact]
    public void DiscoveryRequestDto_ExpectedMessageType_ShouldBeConstant()
    {
        // Assert
        DiscoveryRequestDto.ExpectedMessageType.Should().Be("MYIOTGRID_DISCOVER");
    }
}

#endregion

#region DiscoveryResponseDto Tests

/// <summary>
/// Tests for DiscoveryResponseDto - UDP discovery response from hub
/// </summary>
public class DiscoveryResponseDtoTests
{
    [Fact]
    public void DiscoveryResponseDto_ShouldBeCreatedWithAllProperties()
    {
        // Arrange & Act
        var dto = new DiscoveryResponseDto(
            MessageType: "MYIOTGRID_HUB",
            HubId: "hub-01",
            HubName: "Home Hub",
            ApiUrl: "https://192.168.1.100:5001",
            ApiVersion: "1.0",
            ProtocolVersion: "1.0"
        );

        // Assert
        dto.MessageType.Should().Be("MYIOTGRID_HUB");
        dto.HubId.Should().Be("hub-01");
        dto.HubName.Should().Be("Home Hub");
        dto.ApiUrl.Should().Be("https://192.168.1.100:5001");
        dto.ApiVersion.Should().Be("1.0");
        dto.ProtocolVersion.Should().Be("1.0");
    }

    [Fact]
    public void DiscoveryResponseDto_IsValid_ShouldReturnTrueForCorrectMessageType()
    {
        // Arrange
        var dto = DiscoveryResponseDto.Create("hub-01", "Home Hub", "https://localhost:5001");

        // Assert
        dto.IsValid.Should().BeTrue();
        dto.MessageType.Should().Be(DiscoveryResponseDto.ExpectedMessageType);
    }

    [Fact]
    public void DiscoveryResponseDto_IsValid_ShouldReturnFalseForIncorrectMessageType()
    {
        // Arrange
        var dto = new DiscoveryResponseDto(
            MessageType: "WRONG_TYPE",
            HubId: "hub-01",
            HubName: "Hub",
            ApiUrl: "https://localhost:5001",
            ApiVersion: "1.0",
            ProtocolVersion: "1.0"
        );

        // Assert
        dto.IsValid.Should().BeFalse();
    }

    [Fact]
    public void DiscoveryResponseDto_Create_ShouldUseCurrentVersions()
    {
        // Act
        var dto = DiscoveryResponseDto.Create(
            hubId: "hub-main",
            hubName: "Main Hub",
            apiUrl: "https://10.0.0.1:5001"
        );

        // Assert
        dto.MessageType.Should().Be("MYIOTGRID_HUB");
        dto.HubId.Should().Be("hub-main");
        dto.HubName.Should().Be("Main Hub");
        dto.ApiUrl.Should().Be("https://10.0.0.1:5001");
        dto.ApiVersion.Should().Be(DiscoveryResponseDto.CurrentApiVersion);
        dto.ProtocolVersion.Should().Be(DiscoveryResponseDto.CurrentProtocolVersion);
    }

    [Fact]
    public void DiscoveryResponseDto_CurrentVersions_ShouldBeSet()
    {
        // Assert
        DiscoveryResponseDto.CurrentApiVersion.Should().Be("1.0");
        DiscoveryResponseDto.CurrentProtocolVersion.Should().Be("1.0");
        DiscoveryResponseDto.ExpectedMessageType.Should().Be("MYIOTGRID_HUB");
    }
}

#endregion

#region DiscoveryOptions Tests

/// <summary>
/// Tests for DiscoveryOptions - Configuration for discovery service
/// </summary>
public class DiscoveryOptionsTests
{
    [Fact]
    public void DiscoveryOptions_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var options = new DiscoveryOptions();

        // Assert
        options.Enabled.Should().BeTrue();
        options.Port.Should().Be(5001);
        options.Protocol.Should().Be("https");
        options.ApiPort.Should().Be(5001);
        options.ReceiveTimeoutMs.Should().Be(1000);
        options.LogDiscoveryRequests.Should().BeFalse();
        options.HubId.Should().BeNull();
        options.HubName.Should().BeNull();
        options.AdvertiseIp.Should().BeNull();
        options.NetworkInterface.Should().BeNull();
    }

    [Fact]
    public void DiscoveryOptions_ShouldAllowCustomValues()
    {
        // Arrange & Act
        var options = new DiscoveryOptions
        {
            Enabled = false,
            Port = 6000,
            Protocol = "http",
            ApiPort = 8080,
            HubId = "custom-hub",
            HubName = "Custom Hub Name",
            AdvertiseIp = "192.168.1.50",
            NetworkInterface = "eth0",
            ReceiveTimeoutMs = 2000,
            LogDiscoveryRequests = true
        };

        // Assert
        options.Enabled.Should().BeFalse();
        options.Port.Should().Be(6000);
        options.Protocol.Should().Be("http");
        options.ApiPort.Should().Be(8080);
        options.HubId.Should().Be("custom-hub");
        options.HubName.Should().Be("Custom Hub Name");
        options.AdvertiseIp.Should().Be("192.168.1.50");
        options.NetworkInterface.Should().Be("eth0");
        options.ReceiveTimeoutMs.Should().Be(2000);
        options.LogDiscoveryRequests.Should().BeTrue();
    }

    [Fact]
    public void DiscoveryOptions_SectionName_ShouldBeCorrect()
    {
        // Assert
        DiscoveryOptions.SectionName.Should().Be("Discovery");
    }
}

#endregion
