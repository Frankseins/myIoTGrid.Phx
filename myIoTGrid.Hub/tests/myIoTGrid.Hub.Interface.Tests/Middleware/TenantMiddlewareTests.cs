using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using myIoTGrid.Hub.Interface.Middleware;

namespace myIoTGrid.Hub.Interface.Tests.Middleware;

/// <summary>
/// Tests for TenantMiddleware.
/// Extrahiert TenantId aus Header oder verwendet Default.
/// </summary>
public class TenantMiddlewareTests
{
    private readonly Mock<ILogger<TenantMiddleware>> _loggerMock;
    private readonly Mock<ITenantService> _tenantServiceMock;
    private readonly Mock<RequestDelegate> _nextMock;
    private readonly IConfiguration _configuration;

    private readonly Guid _defaultTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly Guid _customTenantId = Guid.Parse("00000000-0000-0000-0000-000000000099");

    public TenantMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger<TenantMiddleware>>();
        _tenantServiceMock = new Mock<ITenantService>();
        _nextMock = new Mock<RequestDelegate>();

        var configValues = new Dictionary<string, string?>
        {
            { "Hub:DefaultTenantId", _defaultTenantId.ToString() }
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();
    }

    [Fact]
    public async Task InvokeAsync_WithTenantIdHeader_UsesTenantFromHeader()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Tenant-Id"] = _customTenantId.ToString();

        var middleware = new TenantMiddleware(_nextMock.Object, _loggerMock.Object, _configuration);

        // Act
        await middleware.InvokeAsync(context, _tenantServiceMock.Object);

        // Assert
        _tenantServiceMock.Verify(s => s.SetCurrentTenantId(_customTenantId), Times.Once);
        _nextMock.Verify(n => n(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithoutTenantIdHeader_UsesDefaultTenant()
    {
        // Arrange
        var context = new DefaultHttpContext();
        // No X-Tenant-Id header

        var middleware = new TenantMiddleware(_nextMock.Object, _loggerMock.Object, _configuration);

        // Act
        await middleware.InvokeAsync(context, _tenantServiceMock.Object);

        // Assert
        _tenantServiceMock.Verify(s => s.SetCurrentTenantId(_defaultTenantId), Times.Once);
        _nextMock.Verify(n => n(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithInvalidTenantIdHeader_UsesDefaultTenant()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Tenant-Id"] = "not-a-valid-guid";

        var middleware = new TenantMiddleware(_nextMock.Object, _loggerMock.Object, _configuration);

        // Act
        await middleware.InvokeAsync(context, _tenantServiceMock.Object);

        // Assert
        _tenantServiceMock.Verify(s => s.SetCurrentTenantId(_defaultTenantId), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithEmptyTenantIdHeader_UsesDefaultTenant()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Tenant-Id"] = "";

        var middleware = new TenantMiddleware(_nextMock.Object, _loggerMock.Object, _configuration);

        // Act
        await middleware.InvokeAsync(context, _tenantServiceMock.Object);

        // Assert
        _tenantServiceMock.Verify(s => s.SetCurrentTenantId(_defaultTenantId), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithoutConfiguredDefaultTenant_UsesFallbackTenant()
    {
        // Arrange
        var emptyConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        var context = new DefaultHttpContext();

        var middleware = new TenantMiddleware(_nextMock.Object, _loggerMock.Object, emptyConfig);
        var fallbackTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        // Act
        await middleware.InvokeAsync(context, _tenantServiceMock.Object);

        // Assert
        _tenantServiceMock.Verify(s => s.SetCurrentTenantId(fallbackTenantId), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_CallsNextMiddleware()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var nextCalled = false;

        RequestDelegate next = ctx =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new TenantMiddleware(next, _loggerMock.Object, _configuration);

        // Act
        await middleware.InvokeAsync(context, _tenantServiceMock.Object);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WithMultipleTenantIdHeaders_UsesFirst()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers.Append("X-Tenant-Id", _customTenantId.ToString());
        context.Request.Headers.Append("X-Tenant-Id", Guid.NewGuid().ToString());

        var middleware = new TenantMiddleware(_nextMock.Object, _loggerMock.Object, _configuration);

        // Act
        await middleware.InvokeAsync(context, _tenantServiceMock.Object);

        // Assert
        _tenantServiceMock.Verify(s => s.SetCurrentTenantId(_customTenantId), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_SetsContextTenantBeforeCallingNext()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Tenant-Id"] = _customTenantId.ToString();

        var sequence = new List<string>();

        _tenantServiceMock.Setup(s => s.SetCurrentTenantId(_customTenantId))
            .Callback(() => sequence.Add("SetTenant"));

        RequestDelegate next = ctx =>
        {
            sequence.Add("Next");
            return Task.CompletedTask;
        };

        var middleware = new TenantMiddleware(next, _loggerMock.Object, _configuration);

        // Act
        await middleware.InvokeAsync(context, _tenantServiceMock.Object);

        // Assert
        sequence.Should().ContainInOrder("SetTenant", "Next");
    }
}
