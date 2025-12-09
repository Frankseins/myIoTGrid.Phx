using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using myIoTGrid.Hub.Infrastructure.Matter;
using myIoTGrid.Hub.Interface.BackgroundServices;

namespace myIoTGrid.Hub.Interface.Tests.BackgroundServices;

/// <summary>
/// Unit tests for Background Services
/// Tests focus on configuration-based behavior (enabled/disabled states)
/// </summary>
public class NodeMonitorServiceConfigurationTests
{
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<ILogger<NodeMonitorService>> _loggerMock;

    public NodeMonitorServiceConfigurationTests()
    {
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _loggerMock = new Mock<ILogger<NodeMonitorService>>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenDisabled_ReturnsImmediately()
    {
        // Arrange
        var options = new MonitoringOptions { EnableNodeMonitoring = false };
        var sut = new NodeMonitorService(
            _scopeFactoryMock.Object,
            Options.Create(options),
            _loggerMock.Object);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Act
        await sut.StartAsync(cts.Token);
        await Task.Delay(50);
        await sut.StopAsync(CancellationToken.None);

        // Assert - Service should not have created any scopes since it's disabled
        _scopeFactoryMock.Verify(f => f.CreateScope(), Times.Never);
    }
}

public class HubMonitorServiceConfigurationTests
{
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<ILogger<HubMonitorService>> _loggerMock;

    public HubMonitorServiceConfigurationTests()
    {
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _loggerMock = new Mock<ILogger<HubMonitorService>>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenDisabled_ReturnsImmediately()
    {
        // Arrange
        var options = new MonitoringOptions { EnableHubMonitoring = false };
        var sut = new HubMonitorService(
            _scopeFactoryMock.Object,
            Options.Create(options),
            _loggerMock.Object);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Act
        await sut.StartAsync(cts.Token);
        await Task.Delay(50);
        await sut.StopAsync(CancellationToken.None);

        // Assert - Service should not have created any scopes since it's disabled
        _scopeFactoryMock.Verify(f => f.CreateScope(), Times.Never);
    }
}

public class DataRetentionServiceConfigurationTests
{
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<ILogger<DataRetentionService>> _loggerMock;

    public DataRetentionServiceConfigurationTests()
    {
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _loggerMock = new Mock<ILogger<DataRetentionService>>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenDisabled_ReturnsImmediately()
    {
        // Arrange
        var options = new MonitoringOptions { EnableDataRetention = false };
        var sut = new DataRetentionService(
            _scopeFactoryMock.Object,
            Options.Create(options),
            _loggerMock.Object);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Act
        await sut.StartAsync(cts.Token);
        await Task.Delay(50);
        await sut.StopAsync(CancellationToken.None);

        // Assert - Service should not have created any scopes since it's disabled
        _scopeFactoryMock.Verify(f => f.CreateScope(), Times.Never);
    }
}

public class MatterBridgeServiceConfigurationTests
{
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<IMatterBridgeClient> _matterBridgeClientMock;
    private readonly Mock<ILogger<MatterBridgeService>> _loggerMock;

    public MatterBridgeServiceConfigurationTests()
    {
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _matterBridgeClientMock = new Mock<IMatterBridgeClient>();
        _loggerMock = new Mock<ILogger<MatterBridgeService>>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenDisabled_DoesNotCheckAvailability()
    {
        // Arrange
        var options = new MatterBridgeOptions { Enabled = false };
        var sut = new MatterBridgeService(
            _scopeFactoryMock.Object,
            _matterBridgeClientMock.Object,
            Options.Create(options),
            _loggerMock.Object);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Act
        await sut.StartAsync(cts.Token);
        await Task.Delay(50);
        await sut.StopAsync(CancellationToken.None);

        // Assert
        _matterBridgeClientMock.Verify(c => c.IsAvailableAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenMatterBridgeNotAvailable_RetriesWithTimeout()
    {
        // Arrange
        var options = new MatterBridgeOptions
        {
            Enabled = true,
            EnabledSensorTypes = new[] { "temperature", "humidity" }
        };

        _matterBridgeClientMock.Setup(c => c.IsAvailableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = new MatterBridgeService(
            _scopeFactoryMock.Object,
            _matterBridgeClientMock.Object,
            Options.Create(options),
            _loggerMock.Object);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

        // Act
        await sut.StartAsync(cts.Token);
        await Task.Delay(2000);
        await sut.StopAsync(CancellationToken.None);

        // Assert - Should have retried availability check
        _matterBridgeClientMock.Verify(c => c.IsAvailableAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}

public class MonitoringOptionsTests
{
    [Fact]
    public void MonitoringOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new MonitoringOptions();

        // Assert
        options.EnableNodeMonitoring.Should().BeTrue();
        options.EnableHubMonitoring.Should().BeTrue();
        options.EnableDataRetention.Should().BeTrue();
        options.NodeCheckIntervalSeconds.Should().Be(60);
        options.HubCheckIntervalSeconds.Should().Be(60);
        options.NodeOfflineTimeoutMinutes.Should().Be(5);
        options.HubOfflineTimeoutMinutes.Should().Be(5);
        options.DataRetentionDays.Should().Be(30);
        options.DataRetentionIntervalHours.Should().Be(24);
    }

    [Fact]
    public void MonitoringOptions_CanBeConfigured()
    {
        // Arrange & Act
        var options = new MonitoringOptions
        {
            EnableNodeMonitoring = false,
            EnableHubMonitoring = false,
            EnableDataRetention = false,
            NodeCheckIntervalSeconds = 120,
            HubCheckIntervalSeconds = 180,
            NodeOfflineTimeoutMinutes = 10,
            HubOfflineTimeoutMinutes = 15,
            DataRetentionDays = 60,
            DataRetentionIntervalHours = 12
        };

        // Assert
        options.EnableNodeMonitoring.Should().BeFalse();
        options.EnableHubMonitoring.Should().BeFalse();
        options.EnableDataRetention.Should().BeFalse();
        options.NodeCheckIntervalSeconds.Should().Be(120);
        options.HubCheckIntervalSeconds.Should().Be(180);
        options.NodeOfflineTimeoutMinutes.Should().Be(10);
        options.HubOfflineTimeoutMinutes.Should().Be(15);
        options.DataRetentionDays.Should().Be(60);
        options.DataRetentionIntervalHours.Should().Be(12);
    }
}

public class MatterBridgeOptionsTests
{
    [Fact]
    public void MatterBridgeOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new MatterBridgeOptions();

        // Assert
        options.Enabled.Should().BeFalse();
        options.BaseUrl.Should().Be("http://localhost:3000");
        options.TimeoutSeconds.Should().Be(10);
        options.RetryCount.Should().Be(3);
        options.RetryDelayMilliseconds.Should().Be(1000);
    }

    [Fact]
    public void MatterBridgeOptions_CanBeConfigured()
    {
        // Arrange & Act
        var options = new MatterBridgeOptions
        {
            Enabled = true,
            BaseUrl = "http://matter-bridge:8080",
            TimeoutSeconds = 30,
            RetryCount = 5,
            RetryDelayMilliseconds = 2000,
            EnabledSensorTypes = new[] { "temperature", "humidity", "pressure" },
            EnableAlertSensors = true
        };

        // Assert
        options.Enabled.Should().BeTrue();
        options.BaseUrl.Should().Be("http://matter-bridge:8080");
        options.TimeoutSeconds.Should().Be(30);
        options.RetryCount.Should().Be(5);
        options.RetryDelayMilliseconds.Should().Be(2000);
        options.EnabledSensorTypes.Should().HaveCount(3);
        options.EnableAlertSensors.Should().BeTrue();
    }
}

public class DiscoveryOptionsTests
{
    [Fact]
    public void DiscoveryOptions_DefaultValues_AreCorrect()
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
    }

    [Fact]
    public void DiscoveryOptions_CanBeConfigured()
    {
        // Arrange & Act
        var options = new DiscoveryOptions
        {
            Enabled = false,
            Port = 8080,
            HubId = "custom-hub-01",
            HubName = "Custom Hub",
            Protocol = "http",
            ApiPort = 8080,
            AdvertiseIp = "192.168.1.100",
            NetworkInterface = "eth0",
            ReceiveTimeoutMs = 2000,
            LogDiscoveryRequests = true
        };

        // Assert
        options.Enabled.Should().BeFalse();
        options.Port.Should().Be(8080);
        options.HubId.Should().Be("custom-hub-01");
        options.HubName.Should().Be("Custom Hub");
        options.Protocol.Should().Be("http");
        options.ApiPort.Should().Be(8080);
        options.AdvertiseIp.Should().Be("192.168.1.100");
        options.NetworkInterface.Should().Be("eth0");
        options.ReceiveTimeoutMs.Should().Be(2000);
        options.LogDiscoveryRequests.Should().BeTrue();
    }
}

public class MatterBridgeServiceEnabledTests
{
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<IMatterBridgeClient> _matterBridgeClientMock;
    private readonly Mock<ILogger<MatterBridgeService>> _loggerMock;

    public MatterBridgeServiceEnabledTests()
    {
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _matterBridgeClientMock = new Mock<IMatterBridgeClient>();
        _loggerMock = new Mock<ILogger<MatterBridgeService>>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenMatterBridgeBecomesAvailable_InitializesDevices()
    {
        // Arrange
        var options = new MatterBridgeOptions
        {
            Enabled = true,
            EnabledSensorTypes = new[] { "temperature" }
        };

        var callCount = 0;
        _matterBridgeClientMock.Setup(c => c.IsAvailableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount >= 2; // Becomes available after first check
            });

        _matterBridgeClientMock.Setup(c => c.GetStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MatterBridgeStatus(
                IsStarted: true,
                DeviceCount: 0,
                Devices: new List<MatterDeviceInfo>(),
                PairingCode: 12345678,
                Discriminator: 1234
            ));

        var sut = new MatterBridgeService(
            _scopeFactoryMock.Object,
            _matterBridgeClientMock.Object,
            Options.Create(options),
            _loggerMock.Object);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act
        await sut.StartAsync(cts.Token);
        await Task.Delay(2500); // Wait for retry
        await sut.StopAsync(CancellationToken.None);

        // Assert - Should have checked availability multiple times
        _matterBridgeClientMock.Verify(c => c.IsAvailableAsync(It.IsAny<CancellationToken>()), Times.AtLeast(2));
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancellationRequested_StopsGracefully()
    {
        // Arrange
        var options = new MatterBridgeOptions
        {
            Enabled = true,
            EnabledSensorTypes = new[] { "temperature" }
        };

        _matterBridgeClientMock.Setup(c => c.IsAvailableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = new MatterBridgeService(
            _scopeFactoryMock.Object,
            _matterBridgeClientMock.Object,
            Options.Create(options),
            _loggerMock.Object);

        using var cts = new CancellationTokenSource();

        // Act
        await sut.StartAsync(cts.Token);
        await Task.Delay(100);
        cts.Cancel();
        await sut.StopAsync(CancellationToken.None);

        // Assert - Should not throw
        _matterBridgeClientMock.Verify(c => c.IsAvailableAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}

public class NodeMonitorServiceEnabledTests
{
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<ILogger<NodeMonitorService>> _loggerMock;
    private readonly Mock<IServiceScope> _scopeMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;

    public NodeMonitorServiceEnabledTests()
    {
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _loggerMock = new Mock<ILogger<NodeMonitorService>>();
        _scopeMock = new Mock<IServiceScope>();
        _serviceProviderMock = new Mock<IServiceProvider>();

        _scopeMock.Setup(s => s.ServiceProvider).Returns(_serviceProviderMock.Object);
        _scopeFactoryMock.Setup(f => f.CreateScope()).Returns(_scopeMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancellationRequested_StopsGracefully()
    {
        // Arrange
        var options = new MonitoringOptions
        {
            EnableNodeMonitoring = true,
            NodeCheckIntervalSeconds = 1 // Short interval for testing
        };

        var sut = new NodeMonitorService(
            _scopeFactoryMock.Object,
            Options.Create(options),
            _loggerMock.Object);

        using var cts = new CancellationTokenSource();

        // Act
        await sut.StartAsync(cts.Token);
        await Task.Delay(50);
        cts.Cancel();

        // Give time for cancellation to propagate
        await Task.Delay(100);
        await sut.StopAsync(CancellationToken.None);

        // Assert - Should not throw, service stopped gracefully
        _loggerMock.Invocations.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Constructor_WithValidDependencies_DoesNotThrow()
    {
        // Arrange & Act
        var options = new MonitoringOptions();
        var sut = new NodeMonitorService(
            _scopeFactoryMock.Object,
            Options.Create(options),
            _loggerMock.Object);

        // Assert
        sut.Should().NotBeNull();
    }
}

public class HubMonitorServiceEnabledTests
{
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<ILogger<HubMonitorService>> _loggerMock;
    private readonly Mock<IServiceScope> _scopeMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;

    public HubMonitorServiceEnabledTests()
    {
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _loggerMock = new Mock<ILogger<HubMonitorService>>();
        _scopeMock = new Mock<IServiceScope>();
        _serviceProviderMock = new Mock<IServiceProvider>();

        _scopeMock.Setup(s => s.ServiceProvider).Returns(_serviceProviderMock.Object);
        _scopeFactoryMock.Setup(f => f.CreateScope()).Returns(_scopeMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancellationRequested_StopsGracefully()
    {
        // Arrange
        var options = new MonitoringOptions
        {
            EnableHubMonitoring = true,
            HubCheckIntervalSeconds = 1
        };

        var sut = new HubMonitorService(
            _scopeFactoryMock.Object,
            Options.Create(options),
            _loggerMock.Object);

        using var cts = new CancellationTokenSource();

        // Act
        await sut.StartAsync(cts.Token);
        await Task.Delay(50);
        cts.Cancel();
        await Task.Delay(100);
        await sut.StopAsync(CancellationToken.None);

        // Assert - Should not throw
        _loggerMock.Invocations.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Constructor_WithValidDependencies_DoesNotThrow()
    {
        // Arrange & Act
        var options = new MonitoringOptions();
        var sut = new HubMonitorService(
            _scopeFactoryMock.Object,
            Options.Create(options),
            _loggerMock.Object);

        // Assert
        sut.Should().NotBeNull();
    }
}

public class DataRetentionServiceEnabledTests
{
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<ILogger<DataRetentionService>> _loggerMock;
    private readonly Mock<IServiceScope> _scopeMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;

    public DataRetentionServiceEnabledTests()
    {
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _loggerMock = new Mock<ILogger<DataRetentionService>>();
        _scopeMock = new Mock<IServiceScope>();
        _serviceProviderMock = new Mock<IServiceProvider>();

        _scopeMock.Setup(s => s.ServiceProvider).Returns(_serviceProviderMock.Object);
        _scopeFactoryMock.Setup(f => f.CreateScope()).Returns(_scopeMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancellationRequested_StopsGracefully()
    {
        // Arrange
        var options = new MonitoringOptions
        {
            EnableDataRetention = true,
            DataRetentionIntervalHours = 1
        };

        var sut = new DataRetentionService(
            _scopeFactoryMock.Object,
            Options.Create(options),
            _loggerMock.Object);

        using var cts = new CancellationTokenSource();

        // Act
        await sut.StartAsync(cts.Token);
        await Task.Delay(50);
        cts.Cancel();
        await Task.Delay(100);
        await sut.StopAsync(CancellationToken.None);

        // Assert - Should not throw
        _loggerMock.Invocations.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Constructor_WithValidDependencies_DoesNotThrow()
    {
        // Arrange & Act
        var options = new MonitoringOptions();
        var sut = new DataRetentionService(
            _scopeFactoryMock.Object,
            Options.Create(options),
            _loggerMock.Object);

        // Assert
        sut.Should().NotBeNull();
    }
}

#region DebugLogCleanupService Tests

public class DebugLogCleanupServiceDisabledTests
{
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<ILogger<DebugLogCleanupService>> _loggerMock;

    public DebugLogCleanupServiceDisabledTests()
    {
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _loggerMock = new Mock<ILogger<DebugLogCleanupService>>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenDisabled_ReturnsImmediately()
    {
        // Arrange
        var options = new MonitoringOptions { EnableDebugLogCleanup = false };
        var sut = new DebugLogCleanupService(
            _scopeFactoryMock.Object,
            Options.Create(options),
            _loggerMock.Object);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Act
        await sut.StartAsync(cts.Token);
        await Task.Delay(50);
        await sut.StopAsync(CancellationToken.None);

        // Assert - Service should not have created any scopes since it's disabled
        _scopeFactoryMock.Verify(f => f.CreateScope(), Times.Never);
    }

    [Fact]
    public void Constructor_WithValidDependencies_DoesNotThrow()
    {
        // Arrange & Act
        var options = new MonitoringOptions();
        var sut = new DebugLogCleanupService(
            _scopeFactoryMock.Object,
            Options.Create(options),
            _loggerMock.Object);

        // Assert
        sut.Should().NotBeNull();
    }
}

public class DebugLogCleanupServiceEnabledTests
{
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<ILogger<DebugLogCleanupService>> _loggerMock;
    private readonly Mock<IServiceScope> _scopeMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;

    public DebugLogCleanupServiceEnabledTests()
    {
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _loggerMock = new Mock<ILogger<DebugLogCleanupService>>();
        _scopeMock = new Mock<IServiceScope>();
        _serviceProviderMock = new Mock<IServiceProvider>();

        _scopeMock.Setup(s => s.ServiceProvider).Returns(_serviceProviderMock.Object);
        _scopeFactoryMock.Setup(f => f.CreateScope()).Returns(_scopeMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancellationRequested_StopsGracefully()
    {
        // Arrange
        var options = new MonitoringOptions
        {
            EnableDebugLogCleanup = true,
            DebugLogCleanupIntervalHours = 1,
            DebugLogRetentionDays = 7,
            MaxDebugLogsPerNode = 1000
        };

        var sut = new DebugLogCleanupService(
            _scopeFactoryMock.Object,
            Options.Create(options),
            _loggerMock.Object);

        using var cts = new CancellationTokenSource();

        // Act
        await sut.StartAsync(cts.Token);
        await Task.Delay(50);
        cts.Cancel();
        await Task.Delay(100);
        await sut.StopAsync(CancellationToken.None);

        // Assert - Should not throw
        _loggerMock.Invocations.Should().NotBeEmpty();
    }

    [Fact]
    public void MonitoringOptions_DebugLogCleanup_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new MonitoringOptions();

        // Assert
        options.EnableDebugLogCleanup.Should().BeTrue(); // Default is true
        options.DebugLogCleanupIntervalHours.Should().Be(6);
        options.DebugLogRetentionDays.Should().Be(7);
        options.MaxDebugLogsPerNode.Should().Be(10000);
    }

    [Fact]
    public void MonitoringOptions_DebugLogCleanup_CanBeConfigured()
    {
        // Arrange & Act
        var options = new MonitoringOptions
        {
            EnableDebugLogCleanup = true,
            DebugLogCleanupIntervalHours = 12,
            DebugLogRetentionDays = 14,
            MaxDebugLogsPerNode = 5000
        };

        // Assert
        options.EnableDebugLogCleanup.Should().BeTrue();
        options.DebugLogCleanupIntervalHours.Should().Be(12);
        options.DebugLogRetentionDays.Should().Be(14);
        options.MaxDebugLogsPerNode.Should().Be(5000);
    }
}

#endregion

#region SeedDataHostedService Tests

public class SeedDataHostedServiceTests
{
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<ILogger<SeedDataHostedService>> _loggerMock;
    private readonly Mock<IServiceScope> _scopeMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;

    public SeedDataHostedServiceTests()
    {
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _loggerMock = new Mock<ILogger<SeedDataHostedService>>();
        _scopeMock = new Mock<IServiceScope>();
        _serviceProviderMock = new Mock<IServiceProvider>();

        _scopeMock.Setup(s => s.ServiceProvider).Returns(_serviceProviderMock.Object);
        _scopeFactoryMock.Setup(f => f.CreateScope()).Returns(_scopeMock.Object);
    }

    [Fact]
    public void Constructor_WithValidDependencies_DoesNotThrow()
    {
        // Arrange & Act
        var sut = new SeedDataHostedService(
            _scopeFactoryMock.Object,
            _loggerMock.Object);

        // Assert
        sut.Should().NotBeNull();
    }

    [Fact]
    public async Task StopAsync_CompletesSuccessfully()
    {
        // Arrange
        var sut = new SeedDataHostedService(
            _scopeFactoryMock.Object,
            _loggerMock.Object);

        // Act
        await sut.StopAsync(CancellationToken.None);

        // Assert - Should complete without throwing
        _loggerMock.Invocations.Should().NotBeEmpty();
    }
}

#endregion

#region DiscoveryService Tests

public class DiscoveryServiceDisabledTests
{
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<ILogger<DiscoveryService>> _loggerMock;

    public DiscoveryServiceDisabledTests()
    {
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _loggerMock = new Mock<ILogger<DiscoveryService>>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenDisabled_ReturnsImmediately()
    {
        // Arrange
        var options = new DiscoveryOptions { Enabled = false };
        var sut = new DiscoveryService(
            _scopeFactoryMock.Object,
            Options.Create(options),
            _loggerMock.Object);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Act
        await sut.StartAsync(cts.Token);
        await Task.Delay(50);
        await sut.StopAsync(CancellationToken.None);

        // Assert - Service should not have created any scopes since it's disabled
        _scopeFactoryMock.Verify(f => f.CreateScope(), Times.Never);
    }

    [Fact]
    public void Constructor_WithValidDependencies_DoesNotThrow()
    {
        // Arrange & Act
        var options = new DiscoveryOptions();
        var sut = new DiscoveryService(
            _scopeFactoryMock.Object,
            Options.Create(options),
            _loggerMock.Object);

        // Assert
        sut.Should().NotBeNull();
    }
}

public class DiscoveryServiceEnabledTests
{
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<ILogger<DiscoveryService>> _loggerMock;
    private readonly Mock<IServiceScope> _scopeMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;

    public DiscoveryServiceEnabledTests()
    {
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _loggerMock = new Mock<ILogger<DiscoveryService>>();
        _scopeMock = new Mock<IServiceScope>();
        _serviceProviderMock = new Mock<IServiceProvider>();

        _scopeMock.Setup(s => s.ServiceProvider).Returns(_serviceProviderMock.Object);
        _scopeFactoryMock.Setup(f => f.CreateScope()).Returns(_scopeMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancellationRequested_StopsGracefully()
    {
        // Arrange - Note: Using high port to avoid binding issues
        var options = new DiscoveryOptions
        {
            Enabled = true,
            Port = 59123, // High port for testing
            ReceiveTimeoutMs = 100
        };

        var sut = new DiscoveryService(
            _scopeFactoryMock.Object,
            Options.Create(options),
            _loggerMock.Object);

        using var cts = new CancellationTokenSource();

        // Act
        await sut.StartAsync(cts.Token);
        await Task.Delay(50);
        cts.Cancel();
        await Task.Delay(200);
        await sut.StopAsync(CancellationToken.None);

        // Assert - Should not throw
        _loggerMock.Invocations.Should().NotBeEmpty();
    }

    [Fact]
    public void DiscoveryOptions_FullConfiguration_CanBeSet()
    {
        // Arrange & Act
        var options = new DiscoveryOptions
        {
            Enabled = true,
            Port = 8080,
            HubId = "test-hub-01",
            HubName = "Test Hub",
            Protocol = "http",
            ApiPort = 8080,
            AdvertiseIp = "192.168.1.50",
            NetworkInterface = "eth1",
            ReceiveTimeoutMs = 5000,
            LogDiscoveryRequests = true
        };

        // Assert
        options.Enabled.Should().BeTrue();
        options.Port.Should().Be(8080);
        options.HubId.Should().Be("test-hub-01");
        options.HubName.Should().Be("Test Hub");
        options.Protocol.Should().Be("http");
        options.ApiPort.Should().Be(8080);
        options.AdvertiseIp.Should().Be("192.168.1.50");
        options.NetworkInterface.Should().Be("eth1");
        options.ReceiveTimeoutMs.Should().Be(5000);
        options.LogDiscoveryRequests.Should().BeTrue();
    }
}

#endregion
