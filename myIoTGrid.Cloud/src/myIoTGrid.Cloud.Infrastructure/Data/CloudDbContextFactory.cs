using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace myIoTGrid.Cloud.Infrastructure.Data;

/// <summary>
/// Factory for creating CloudDbContext at design time (for EF Core migrations)
/// </summary>
public class CloudDbContextFactory : IDesignTimeDbContextFactory<CloudDbContext>
{
    public CloudDbContext CreateDbContext(string[] args)
    {
        // Build configuration from appsettings.json in Api project
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../myIoTGrid.Cloud.Api"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("CloudDb")
            ?? "Host=localhost;Database=myiotgrid_cloud;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<CloudDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
            npgsqlOptions.MigrationsAssembly("myIoTGrid.Cloud.Infrastructure"));

        return new CloudDbContext(optionsBuilder.Options);
    }
}
