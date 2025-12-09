using FluentAssertions;
using myIoTGrid.Hub.Infrastructure.Data;
using myIoTGrid.Hub.Infrastructure.Repositories;

namespace myIoTGrid.Hub.Service.Tests.Repositories;

/// <summary>
/// Tests for UnitOfWork using AlertType entity
/// (SensorType uses TypeId as primary key, AlertType uses standard Guid Id)
/// </summary>
public class UnitOfWorkTests : IDisposable
{
    private readonly HubDbContext _context;
    private readonly UnitOfWork _sut;

    public UnitOfWorkTests()
    {
        _context = TestDbContextFactory.Create();
        _sut = new UnitOfWork(_context);
    }

    public void Dispose()
    {
        _sut.Dispose();
    }

    #region SaveChangesAsync Tests

    [Fact]
    public async Task SaveChangesAsync_WithChanges_ReturnsNumberOfChanges()
    {
        // Arrange
        _context.AlertTypes.Add(new AlertType
        {
            Id = Guid.NewGuid(),
            Code = "test",
            Name = "Test",
            DefaultLevel = AlertLevel.Info,
            CreatedAt = DateTime.UtcNow
        });

        // Act
        var result = await _sut.SaveChangesAsync();

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public async Task SaveChangesAsync_WithNoChanges_ReturnsZero()
    {
        // Act
        var result = await _sut.SaveChangesAsync();

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task SaveChangesAsync_WithMultipleChanges_ReturnsCorrectCount()
    {
        // Arrange
        _context.AlertTypes.Add(new AlertType { Id = Guid.NewGuid(), Code = "a", Name = "A", DefaultLevel = AlertLevel.Info, CreatedAt = DateTime.UtcNow });
        _context.AlertTypes.Add(new AlertType { Id = Guid.NewGuid(), Code = "b", Name = "B", DefaultLevel = AlertLevel.Warning, CreatedAt = DateTime.UtcNow });
        _context.AlertTypes.Add(new AlertType { Id = Guid.NewGuid(), Code = "c", Name = "C", DefaultLevel = AlertLevel.Critical, CreatedAt = DateTime.UtcNow });

        // Act
        var result = await _sut.SaveChangesAsync();

        // Assert
        result.Should().Be(3);
    }

    [Fact]
    public async Task SaveChangesAsync_WithCancellationToken_Works()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        _context.AlertTypes.Add(new AlertType { Id = Guid.NewGuid(), Code = "ct", Name = "CancellationToken", DefaultLevel = AlertLevel.Info, CreatedAt = DateTime.UtcNow });

        // Act
        var result = await _sut.SaveChangesAsync(cts.Token);

        // Assert
        result.Should().Be(1);
    }

    #endregion

    #region Transaction Tests

    [Fact]
    public async Task CommitTransactionAsync_WithoutTransaction_ThrowsException()
    {
        // Act & Assert
        var act = () => _sut.CommitTransactionAsync();
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("No transaction started.");
    }

    [Fact]
    public async Task RollbackTransactionAsync_WithoutTransaction_ThrowsException()
    {
        // Act & Assert
        var act = () => _sut.RollbackTransactionAsync();
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("No transaction started.");
    }

    // Note: BeginTransactionAsync, CommitTransactionAsync, and RollbackTransactionAsync
    // require a real database connection, not InMemory provider
    // These tests are skipped as InMemory doesn't support transactions

    [Fact(Skip = "InMemory provider does not support transactions")]
    public async Task BeginTransactionAsync_StartsTransaction()
    {
        // Act
        await _sut.BeginTransactionAsync();

        // Assert - Transaction should be started (no exception)
    }

    [Fact(Skip = "InMemory provider does not support transactions")]
    public async Task CommitTransactionAsync_CommitsAndDisposesTransaction()
    {
        // Arrange
        await _sut.BeginTransactionAsync();
        _context.AlertTypes.Add(new AlertType { Id = Guid.NewGuid(), Code = "trans", Name = "Transaction", DefaultLevel = AlertLevel.Info, CreatedAt = DateTime.UtcNow });
        await _sut.SaveChangesAsync();

        // Act
        await _sut.CommitTransactionAsync();

        // Assert - Should complete without exception
    }

    [Fact(Skip = "InMemory provider does not support transactions")]
    public async Task RollbackTransactionAsync_RollbacksAndDisposesTransaction()
    {
        // Arrange
        await _sut.BeginTransactionAsync();
        _context.AlertTypes.Add(new AlertType { Id = Guid.NewGuid(), Code = "rollback", Name = "Rollback", DefaultLevel = AlertLevel.Info, CreatedAt = DateTime.UtcNow });
        await _sut.SaveChangesAsync();

        // Act
        await _sut.RollbackTransactionAsync();

        // Assert - Should complete without exception
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var unitOfWork = new UnitOfWork(context);

        // Act & Assert - Should not throw
        unitOfWork.Dispose();
        var act = () => unitOfWork.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_DisposesContext()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var unitOfWork = new UnitOfWork(context);

        // Act
        unitOfWork.Dispose();

        // Assert - Context should be disposed
        var act = () => context.AlertTypes.ToList();
        act.Should().Throw<ObjectDisposedException>();
    }

    #endregion
}
