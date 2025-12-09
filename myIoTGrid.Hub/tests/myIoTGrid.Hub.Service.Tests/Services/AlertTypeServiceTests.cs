using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using myIoTGrid.Hub.Infrastructure.Repositories;
using myIoTGrid.Hub.Service.Services;

namespace myIoTGrid.Hub.Service.Tests.Services;

public class AlertTypeServiceTests : IDisposable
{
    private readonly Infrastructure.Data.HubDbContext _context;
    private readonly AlertTypeService _sut;
    private readonly Mock<ILogger<AlertTypeService>> _loggerMock;

    public AlertTypeServiceTests()
    {
        _context = TestDbContextFactory.Create();
        _loggerMock = new Mock<ILogger<AlertTypeService>>();
        var unitOfWork = new UnitOfWork(_context);

        _sut = new AlertTypeService(_context, unitOfWork, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task GetAllAsync_WhenNoAlertTypes_ReturnsEmptyList()
    {
        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WithAlertTypes_ReturnsAllOrderedByName()
    {
        // Arrange
        _context.AlertTypes.Add(new AlertType
        {
            Id = Guid.NewGuid(),
            Code = "z_test",
            Name = "Z Test",
            DefaultLevel = AlertLevel.Warning,
            CreatedAt = DateTime.UtcNow
        });
        _context.AlertTypes.Add(new AlertType
        {
            Id = Guid.NewGuid(),
            Code = "a_test",
            Name = "A Test",
            DefaultLevel = AlertLevel.Info,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = (await _sut.GetAllAsync()).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.First().Name.Should().Be("A Test");
        result.Last().Name.Should().Be("Z Test");
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingAlertType_ReturnsAlertType()
    {
        // Arrange
        var id = Guid.NewGuid();
        _context.AlertTypes.Add(new AlertType
        {
            Id = id,
            Code = "test_type",
            Name = "Test Type",
            DefaultLevel = AlertLevel.Warning,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByIdAsync(id);

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be("test_type");
        result.Name.Should().Be("Test Type");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingAlertType_ReturnsNull()
    {
        // Act
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByCodeAsync_WithExistingCode_ReturnsAlertType()
    {
        // Arrange
        _context.AlertTypes.Add(new AlertType
        {
            Id = Guid.NewGuid(),
            Code = "unique_code",
            Name = "Unique Type",
            DefaultLevel = AlertLevel.Critical,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByCodeAsync("unique_code");

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be("unique_code");
    }

    [Fact]
    public async Task GetByCodeAsync_WithNonExistingCode_ReturnsNull()
    {
        // Act
        var result = await _sut.GetByCodeAsync("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByCodeAsync_NormalizesCodeToLowerCase()
    {
        // Arrange
        _context.AlertTypes.Add(new AlertType
        {
            Id = Guid.NewGuid(),
            Code = "lower_case",
            Name = "Lower Case Type",
            DefaultLevel = AlertLevel.Warning,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByCodeAsync("LOWER_CASE");

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be("lower_case");
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_CreatesAlertType()
    {
        // Arrange
        var dto = new CreateAlertTypeDto(
            Code: "new_type",
            Name: "New Type",
            Description: "A new alert type",
            DefaultLevel: AlertLevelDto.Warning,
            IconName: "warning"
        );

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Code.Should().Be("new_type");
        result.Name.Should().Be("New Type");
        result.Description.Should().Be("A new alert type");
        result.DefaultLevel.Should().Be(AlertLevelDto.Warning);
    }

    [Fact]
    public async Task CreateAsync_NormalizesCodeToLowerCase()
    {
        // Arrange
        var dto = new CreateAlertTypeDto(
            Code: "UPPER_CASE",
            Name: "Upper Case",
            DefaultLevel: AlertLevelDto.Info
        );

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.Code.Should().Be("upper_case");
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateCode_ThrowsException()
    {
        // Arrange
        _context.AlertTypes.Add(new AlertType
        {
            Id = Guid.NewGuid(),
            Code = "duplicate",
            Name = "Duplicate",
            DefaultLevel = AlertLevel.Warning,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var dto = new CreateAlertTypeDto(
            Code: "duplicate",
            Name: "Another Duplicate",
            DefaultLevel: AlertLevelDto.Warning
        );

        // Act & Assert
        var act = () => _sut.CreateAsync(dto);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*existiert bereits*");
    }

    [Fact]
    public async Task SyncFromCloudAsync_DoesNotThrow()
    {
        // Act & Assert
        var act = () => _sut.SyncFromCloudAsync();
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SeedDefaultTypesAsync_WithEmptyDatabase_CreatesAllDefaultTypes()
    {
        // Act
        await _sut.SeedDefaultTypesAsync();

        // Assert
        var allTypes = await _sut.GetAllAsync();
        var defaultTypes = DefaultAlertTypes.GetAll();

        allTypes.Count().Should().Be(defaultTypes.Count());
    }

    [Fact]
    public async Task SeedDefaultTypesAsync_WithExistingTypes_DoesNotDuplicate()
    {
        // Arrange - add one default type manually
        _context.AlertTypes.Add(new AlertType
        {
            Id = Guid.NewGuid(),
            Code = "mold_risk",
            Name = "Existing Mold Risk",
            DefaultLevel = AlertLevel.Warning,
            IsGlobal = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        await _sut.SeedDefaultTypesAsync();

        // Assert
        var allTypes = await _sut.GetAllAsync();
        allTypes.Count(at => at.Code == "mold_risk").Should().Be(1);
    }

    [Fact]
    public async Task SeedDefaultTypesAsync_CalledTwice_DoesNotDuplicate()
    {
        // Act
        await _sut.SeedDefaultTypesAsync();
        await _sut.SeedDefaultTypesAsync();

        // Assert
        var allTypes = await _sut.GetAllAsync();
        var defaultTypes = DefaultAlertTypes.GetAll();

        allTypes.Count().Should().Be(defaultTypes.Count());
    }

    [Fact]
    public async Task SeedDefaultTypesAsync_SetsIsGlobalToTrue()
    {
        // Act
        await _sut.SeedDefaultTypesAsync();

        // Assert
        var allTypes = _context.AlertTypes.ToList();
        allTypes.Should().AllSatisfy(at => at.IsGlobal.Should().BeTrue());
    }

    [Theory]
    [InlineData("mold_risk", "Schimmelrisiko")]
    [InlineData("frost_warning", "Frostwarnung")]
    [InlineData("hub_offline", "Hub offline")]
    [InlineData("battery_low", "Batterie niedrig")]
    public async Task SeedDefaultTypesAsync_CreatesExpectedDefaultTypes(string expectedCode, string expectedName)
    {
        // Act
        await _sut.SeedDefaultTypesAsync();

        // Assert
        var alertType = await _sut.GetByCodeAsync(expectedCode);
        alertType.Should().NotBeNull();
        alertType!.Name.Should().Be(expectedName);
    }
}
