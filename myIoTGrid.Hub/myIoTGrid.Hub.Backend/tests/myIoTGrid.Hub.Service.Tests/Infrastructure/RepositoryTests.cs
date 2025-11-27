using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using myIoTGrid.Hub.Domain.Entities;
using myIoTGrid.Hub.Infrastructure.Data;
using myIoTGrid.Hub.Infrastructure.Repositories;

namespace myIoTGrid.Hub.Service.Tests.Repositories;

public class RepositoryTests : IDisposable
{
    private readonly HubDbContext _context;
    private readonly Repository<SensorType> _sut;

    public RepositoryTests()
    {
        _context = TestDbContextFactory.Create();
        _sut = new Repository<SensorType>(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingEntity_ReturnsEntity()
    {
        // Arrange
        var id = Guid.NewGuid();
        _context.SensorTypes.Add(new SensorType
        {
            Id = id,
            Code = "test",
            Name = "Test",
            Unit = "t",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByIdAsync(id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingEntity_ReturnsNull()
    {
        // Act
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WhenEmpty_ReturnsEmptyCollection()
    {
        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WithEntities_ReturnsAllEntities()
    {
        // Arrange
        _context.SensorTypes.Add(new SensorType { Id = Guid.NewGuid(), Code = "a", Name = "A", Unit = "a", CreatedAt = DateTime.UtcNow });
        _context.SensorTypes.Add(new SensorType { Id = Guid.NewGuid(), Code = "b", Name = "B", Unit = "b", CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    #endregion

    #region FindAsync Tests

    [Fact]
    public async Task FindAsync_WithMatchingPredicate_ReturnsMatchingEntities()
    {
        // Arrange
        _context.SensorTypes.Add(new SensorType { Id = Guid.NewGuid(), Code = "match", Name = "Match", Unit = "m", CreatedAt = DateTime.UtcNow });
        _context.SensorTypes.Add(new SensorType { Id = Guid.NewGuid(), Code = "other", Name = "Other", Unit = "o", CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.FindAsync(st => st.Code == "match");

        // Assert
        result.Should().ContainSingle();
        result.First().Code.Should().Be("match");
    }

    [Fact]
    public async Task FindAsync_WithNoMatches_ReturnsEmptyCollection()
    {
        // Arrange
        _context.SensorTypes.Add(new SensorType { Id = Guid.NewGuid(), Code = "test", Name = "Test", Unit = "t", CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.FindAsync(st => st.Code == "nonexistent");

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region FirstOrDefaultAsync Tests

    [Fact]
    public async Task FirstOrDefaultAsync_WithMatchingPredicate_ReturnsFirstMatch()
    {
        // Arrange
        _context.SensorTypes.Add(new SensorType { Id = Guid.NewGuid(), Code = "first", Name = "First", Unit = "f", CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.FirstOrDefaultAsync(st => st.Code == "first");

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be("first");
    }

    [Fact]
    public async Task FirstOrDefaultAsync_WithNoMatches_ReturnsNull()
    {
        // Act
        var result = await _sut.FirstOrDefaultAsync(st => st.Code == "nonexistent");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region ExistsAsync Tests

    [Fact]
    public async Task ExistsAsync_WithMatchingEntity_ReturnsTrue()
    {
        // Arrange
        _context.SensorTypes.Add(new SensorType { Id = Guid.NewGuid(), Code = "exists", Name = "Exists", Unit = "e", CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.ExistsAsync(st => st.Code == "exists");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithNoMatchingEntity_ReturnsFalse()
    {
        // Act
        var result = await _sut.ExistsAsync(st => st.Code == "nonexistent");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region CountAsync Tests

    [Fact]
    public async Task CountAsync_WithoutPredicate_ReturnsAllCount()
    {
        // Arrange
        _context.SensorTypes.Add(new SensorType { Id = Guid.NewGuid(), Code = "a", Name = "A", Unit = "a", CreatedAt = DateTime.UtcNow });
        _context.SensorTypes.Add(new SensorType { Id = Guid.NewGuid(), Code = "b", Name = "B", Unit = "b", CreatedAt = DateTime.UtcNow });
        _context.SensorTypes.Add(new SensorType { Id = Guid.NewGuid(), Code = "c", Name = "C", Unit = "c", CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.CountAsync();

        // Assert
        result.Should().Be(3);
    }

    [Fact]
    public async Task CountAsync_WithPredicate_ReturnsMatchingCount()
    {
        // Arrange
        _context.SensorTypes.Add(new SensorType { Id = Guid.NewGuid(), Code = "a", Name = "Match", Unit = "a", CreatedAt = DateTime.UtcNow });
        _context.SensorTypes.Add(new SensorType { Id = Guid.NewGuid(), Code = "b", Name = "Match", Unit = "b", CreatedAt = DateTime.UtcNow });
        _context.SensorTypes.Add(new SensorType { Id = Guid.NewGuid(), Code = "c", Name = "Other", Unit = "c", CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.CountAsync(st => st.Name == "Match");

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public async Task CountAsync_WhenEmpty_ReturnsZero()
    {
        // Act
        var result = await _sut.CountAsync();

        // Assert
        result.Should().Be(0);
    }

    #endregion

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_AddsEntityToDbSet()
    {
        // Arrange
        var entity = new SensorType
        {
            Id = Guid.NewGuid(),
            Code = "new",
            Name = "New",
            Unit = "n",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var result = await _sut.AddAsync(entity);
        await _context.SaveChangesAsync();

        // Assert
        result.Should().Be(entity);
        var fromDb = await _context.SensorTypes.FindAsync(entity.Id);
        fromDb.Should().NotBeNull();
    }

    #endregion

    #region AddRangeAsync Tests

    [Fact]
    public async Task AddRangeAsync_AddsMultipleEntities()
    {
        // Arrange
        var entities = new List<SensorType>
        {
            new() { Id = Guid.NewGuid(), Code = "a", Name = "A", Unit = "a", CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Code = "b", Name = "B", Unit = "b", CreatedAt = DateTime.UtcNow }
        };

        // Act
        await _sut.AddRangeAsync(entities);
        await _context.SaveChangesAsync();

        // Assert
        var count = await _context.SensorTypes.CountAsync();
        count.Should().Be(2);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_UpdatesEntity()
    {
        // Arrange
        var entity = new SensorType
        {
            Id = Guid.NewGuid(),
            Code = "original",
            Name = "Original",
            Unit = "o",
            CreatedAt = DateTime.UtcNow
        };
        _context.SensorTypes.Add(entity);
        await _context.SaveChangesAsync();
        _context.Entry(entity).State = Microsoft.EntityFrameworkCore.EntityState.Detached;

        // Act
        entity.Name = "Updated";
        _sut.Update(entity);
        await _context.SaveChangesAsync();

        // Assert
        var fromDb = await _context.SensorTypes.FindAsync(entity.Id);
        fromDb!.Name.Should().Be("Updated");
    }

    #endregion

    #region UpdateRange Tests

    [Fact]
    public async Task UpdateRange_UpdatesMultipleEntities()
    {
        // Arrange
        var entity1 = new SensorType { Id = Guid.NewGuid(), Code = "a", Name = "A", Unit = "a", CreatedAt = DateTime.UtcNow };
        var entity2 = new SensorType { Id = Guid.NewGuid(), Code = "b", Name = "B", Unit = "b", CreatedAt = DateTime.UtcNow };
        _context.SensorTypes.AddRange(entity1, entity2);
        await _context.SaveChangesAsync();
        _context.Entry(entity1).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
        _context.Entry(entity2).State = Microsoft.EntityFrameworkCore.EntityState.Detached;

        // Act
        entity1.Name = "Updated A";
        entity2.Name = "Updated B";
        _sut.UpdateRange([entity1, entity2]);
        await _context.SaveChangesAsync();

        // Assert
        var fromDb1 = await _context.SensorTypes.FindAsync(entity1.Id);
        var fromDb2 = await _context.SensorTypes.FindAsync(entity2.Id);
        fromDb1!.Name.Should().Be("Updated A");
        fromDb2!.Name.Should().Be("Updated B");
    }

    #endregion

    #region Remove Tests

    [Fact]
    public async Task Remove_RemovesEntity()
    {
        // Arrange
        var entity = new SensorType
        {
            Id = Guid.NewGuid(),
            Code = "todelete",
            Name = "To Delete",
            Unit = "d",
            CreatedAt = DateTime.UtcNow
        };
        _context.SensorTypes.Add(entity);
        await _context.SaveChangesAsync();

        // Act
        _sut.Remove(entity);
        await _context.SaveChangesAsync();

        // Assert
        var fromDb = await _context.SensorTypes.FindAsync(entity.Id);
        fromDb.Should().BeNull();
    }

    #endregion

    #region RemoveRange Tests

    [Fact]
    public async Task RemoveRange_RemovesMultipleEntities()
    {
        // Arrange
        var entity1 = new SensorType { Id = Guid.NewGuid(), Code = "a", Name = "A", Unit = "a", CreatedAt = DateTime.UtcNow };
        var entity2 = new SensorType { Id = Guid.NewGuid(), Code = "b", Name = "B", Unit = "b", CreatedAt = DateTime.UtcNow };
        _context.SensorTypes.AddRange(entity1, entity2);
        await _context.SaveChangesAsync();

        // Act
        _sut.RemoveRange([entity1, entity2]);
        await _context.SaveChangesAsync();

        // Assert
        var count = await _context.SensorTypes.CountAsync();
        count.Should().Be(0);
    }

    #endregion

    #region CancellationToken Tests

    [Fact]
    public async Task GetByIdAsync_WithCancellationToken_Works()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        var result = await _sut.GetByIdAsync(Guid.NewGuid(), cts.Token);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_WithCancellationToken_Works()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        var result = await _sut.GetAllAsync(cts.Token);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task FindAsync_WithCancellationToken_Works()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        var result = await _sut.FindAsync(st => st.Code == "test", cts.Token);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ExistsAsync_WithCancellationToken_Works()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        var result = await _sut.ExistsAsync(st => st.Code == "test", cts.Token);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CountAsync_WithCancellationToken_Works()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        var result = await _sut.CountAsync(null, cts.Token);

        // Assert
        result.Should().Be(0);
    }

    #endregion
}

public class TenantRepositoryTests : IDisposable
{
    private readonly HubDbContext _context;
    private readonly TenantRepository<Alert> _sut;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _alertTypeId = Guid.NewGuid();

    public TenantRepositoryTests()
    {
        _context = TestDbContextFactory.Create();
        _sut = new TenantRepository<Alert>(_context);

        // Setup AlertType
        _context.AlertTypes.Add(new AlertType
        {
            Id = _alertTypeId,
            Code = "test",
            Name = "Test",
            CreatedAt = DateTime.UtcNow
        });
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task GetByTenantAsync_ReturnsOnlyTenantEntities()
    {
        // Arrange
        var otherTenantId = Guid.NewGuid();
        _context.Alerts.Add(new Alert { Id = Guid.NewGuid(), TenantId = _tenantId, AlertTypeId = _alertTypeId, Message = "Tenant 1", CreatedAt = DateTime.UtcNow });
        _context.Alerts.Add(new Alert { Id = Guid.NewGuid(), TenantId = _tenantId, AlertTypeId = _alertTypeId, Message = "Tenant 1 Again", CreatedAt = DateTime.UtcNow });
        _context.Alerts.Add(new Alert { Id = Guid.NewGuid(), TenantId = otherTenantId, AlertTypeId = _alertTypeId, Message = "Other Tenant", CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByTenantAsync(_tenantId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(a => a.TenantId.Should().Be(_tenantId));
    }

    [Fact]
    public async Task GetByTenantAsync_WhenNoEntitiesForTenant_ReturnsEmpty()
    {
        // Arrange
        var differentTenantId = Guid.NewGuid();
        _context.Alerts.Add(new Alert { Id = Guid.NewGuid(), TenantId = differentTenantId, AlertTypeId = _alertTypeId, Message = "Different", CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByTenantAsync(_tenantId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByTenantAsync_WithCancellationToken_Works()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        var result = await _sut.GetByTenantAsync(_tenantId, cts.Token);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TenantRepository_InheritsFromRepository()
    {
        // Arrange
        var alert = new Alert
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            AlertTypeId = _alertTypeId,
            Message = "Test",
            CreatedAt = DateTime.UtcNow
        };

        // Act - Test base class methods
        await _sut.AddAsync(alert);
        await _context.SaveChangesAsync();

        var found = await _sut.GetByIdAsync(alert.Id);

        // Assert
        found.Should().NotBeNull();
        found!.Message.Should().Be("Test");
    }
}
