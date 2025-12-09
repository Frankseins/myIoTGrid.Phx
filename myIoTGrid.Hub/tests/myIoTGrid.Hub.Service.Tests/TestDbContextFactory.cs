using Microsoft.EntityFrameworkCore;
using myIoTGrid.Hub.Infrastructure.Data;

namespace myIoTGrid.Hub.Service.Tests;

/// <summary>
/// Factory for creating in-memory test DbContext instances
/// </summary>
public static class TestDbContextFactory
{
    /// <summary>
    /// Creates a new HubDbContext with an in-memory database
    /// </summary>
    public static HubDbContext Create(string? dbName = null)
    {
        dbName ??= Guid.NewGuid().ToString();

        var options = new DbContextOptionsBuilder<HubDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var context = new HubDbContext(options);
        context.Database.EnsureCreated();

        return context;
    }

    /// <summary>
    /// Creates a shared in-memory database context
    /// </summary>
    public static HubDbContext CreateShared(string dbName)
    {
        var options = new DbContextOptionsBuilder<HubDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new HubDbContext(options);
    }
}
