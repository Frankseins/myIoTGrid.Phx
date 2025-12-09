using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using myIoTGrid.Hub.Infrastructure.Repositories;
using myIoTGrid.Hub.Service.Services;

namespace myIoTGrid.Hub.Service.Tests.Services;

public class TenantServiceTests : IDisposable
{
    private readonly Infrastructure.Data.HubDbContext _context;
    private readonly TenantService _sut;
    private readonly Mock<ILogger<TenantService>> _loggerMock;
    private readonly IConfiguration _configuration;

    public TenantServiceTests()
    {
        _context = TestDbContextFactory.Create();
        _loggerMock = new Mock<ILogger<TenantService>>();
        var unitOfWork = new UnitOfWork(_context);

        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Hub:DefaultTenantId", "00000000-0000-0000-0000-000000000001" },
            { "Hub:DefaultTenantName", "Default" }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _sut = new TenantService(_context, unitOfWork, _loggerMock.Object, _configuration);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public void GetCurrentTenantId_ReturnsDefaultTenantId()
    {
        // Act
        var result = _sut.GetCurrentTenantId();

        // Assert
        result.Should().Be(Guid.Parse("00000000-0000-0000-0000-000000000001"));
    }

    [Fact]
    public void SetCurrentTenantId_SetsTenantId()
    {
        // Arrange
        var newTenantId = Guid.NewGuid();

        // Act
        _sut.SetCurrentTenantId(newTenantId);
        var result = _sut.GetCurrentTenantId();

        // Assert
        result.Should().Be(newTenantId);
    }

    [Fact]
    public async Task EnsureDefaultTenantAsync_CreatesDefaultTenant_WhenNotExists()
    {
        // Act
        await _sut.EnsureDefaultTenantAsync();

        // Assert
        var tenants = await _sut.GetAllAsync();
        tenants.Should().ContainSingle(t => t.Name == "Default");
    }

    [Fact]
    public async Task EnsureDefaultTenantAsync_DoesNotDuplicate_WhenCalledTwice()
    {
        // Act
        await _sut.EnsureDefaultTenantAsync();
        await _sut.EnsureDefaultTenantAsync();

        // Assert
        var tenants = await _sut.GetAllAsync();
        tenants.Count(t => t.Name == "Default").Should().Be(1);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingTenant_ReturnsTenant()
    {
        // Arrange
        await _sut.EnsureDefaultTenantAsync();
        var tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        // Act
        var result = await _sut.GetByIdAsync(tenantId);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Default");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingTenant_ReturnsNull()
    {
        // Act
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOnlyActiveTenants()
    {
        // Arrange
        await _sut.EnsureDefaultTenantAsync();

        // Add an inactive tenant directly
        _context.Tenants.Add(new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Inactive",
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().ContainSingle();
        result.Should().NotContain(t => t.Name == "Inactive");
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_CreatesTenant()
    {
        // Arrange
        var dto = new CreateTenantDto(
            Name: "Test Tenant",
            CloudApiKey: "api-key-123"
        );

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Test Tenant");
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_WithExistingTenant_UpdatesTenant()
    {
        // Arrange
        var createDto = new CreateTenantDto(Name: "Original");
        var created = await _sut.CreateAsync(createDto);

        var updateDto = new UpdateTenantDto(
            Name: "Updated",
            CloudApiKey: "new-key"
        );

        // Act
        var result = await _sut.UpdateAsync(created.Id, updateDto);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated");
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistingTenant_ReturnsNull()
    {
        // Arrange
        var updateDto = new UpdateTenantDto(Name: "Test");

        // Act
        var result = await _sut.UpdateAsync(Guid.NewGuid(), updateDto);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithInvalidDefaultTenantId_UsesFallback()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Hub:DefaultTenantId", "invalid-guid" }
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Act
        var service = new TenantService(_context, new UnitOfWork(_context), _loggerMock.Object, config);

        // Assert
        service.GetCurrentTenantId().Should().Be(Guid.Parse("00000000-0000-0000-0000-000000000001"));
    }

    [Fact]
    public void Constructor_WithMissingConfiguration_UsesFallback()
    {
        // Arrange
        var config = new ConfigurationBuilder().Build();

        // Act
        var service = new TenantService(_context, new UnitOfWork(_context), _loggerMock.Object, config);

        // Assert
        service.GetCurrentTenantId().Should().Be(Guid.Parse("00000000-0000-0000-0000-000000000001"));
    }
}
