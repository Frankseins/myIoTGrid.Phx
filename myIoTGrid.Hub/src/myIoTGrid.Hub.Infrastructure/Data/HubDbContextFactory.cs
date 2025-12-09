using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace myIoTGrid.Hub.Infrastructure.Data;

/// <summary>
/// Design-Time Factory für EF Core Migrations
/// </summary>
public class HubDbContextFactory : IDesignTimeDbContextFactory<HubDbContext>
{
    public HubDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<HubDbContext>();
        optionsBuilder.UseSqlite("Data Source=./data/hub.db");

        return new HubDbContext(optionsBuilder.Options);
    }
}
