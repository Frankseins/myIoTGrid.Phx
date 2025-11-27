using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using myIoTGrid.Hub.Infrastructure.Data;
using myIoTGrid.Hub.Service.Interfaces;

namespace myIoTGrid.Hub.Interface.BackgroundServices;

/// <summary>
/// Hosted service that seeds default data on application startup
/// </summary>
public class SeedDataHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<SeedDataHostedService> _logger;

    public SeedDataHostedService(
        IServiceScopeFactory scopeFactory,
        IHostEnvironment environment,
        ILogger<SeedDataHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _environment = environment;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("SeedDataHostedService starting...");

        using var scope = _scopeFactory.CreateScope();

        try
        {
            // Apply migrations in Development
            if (_environment.IsDevelopment())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<HubDbContext>();
                _logger.LogInformation("Applying database migrations...");
                await dbContext.Database.MigrateAsync(cancellationToken);
                _logger.LogInformation("Database migrations applied successfully");
            }

            // Seed default data
            var seedDataService = scope.ServiceProvider.GetRequiredService<ISeedDataService>();
            await seedDataService.SeedAllAsync(cancellationToken);

            _logger.LogInformation("SeedDataHostedService completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during seed data initialization");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("SeedDataHostedService stopping");
        return Task.CompletedTask;
    }
}
