using FluentValidation;
using FluentValidation.AspNetCore;
using HealthChecks.NpgSql;
using Microsoft.EntityFrameworkCore;
using Serilog;
using myIoTGrid.Cloud.Infrastructure.Data;
using myIoTGrid.Cloud.Infrastructure.Repositories;
using myIoTGrid.Cloud.Interface.BackgroundServices;
using myIoTGrid.Cloud.Interface.Hubs;
using myIoTGrid.Cloud.Interface.Middleware;
using myIoTGrid.Cloud.Interface.Services;
using myIoTGrid.Cloud.Service.Services;
using myIoTGrid.Cloud.Service.Validators;
using myIoTGrid.Shared.Common.Interfaces;
using myIoTGrid.Shared.Contracts.Services;

// =============================================================================
// SERILOG CONFIGURATION
// =============================================================================
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "./logs/cloud-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting myIoTGrid Cloud API...");

    var builder = WebApplication.CreateBuilder(args);

    // Use Serilog
    builder.Host.UseSerilog();

    // =============================================================================
    // DATABASE (PostgreSQL)
    // =============================================================================
    var connectionString = builder.Configuration.GetConnectionString("CloudDb")
        ?? "Host=localhost;Database=myiotgrid_cloud;Username=postgres;Password=postgres";

    builder.Services.AddDbContext<CloudDbContext>(options =>
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(3);
            npgsqlOptions.CommandTimeout(30);
        }));

    // =============================================================================
    // REPOSITORIES
    // =============================================================================
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

    // =============================================================================
    // SERVICES
    // =============================================================================
    builder.Services.AddScoped<ITenantService, TenantService>();
    builder.Services.AddScoped<IHubService, HubService>();
    builder.Services.AddScoped<INodeService, NodeService>();
    builder.Services.AddScoped<ISensorService, SensorService>();
    builder.Services.AddScoped<INodeSensorAssignmentService, NodeSensorAssignmentService>();
    builder.Services.AddScoped<IReadingService, ReadingService>();
    builder.Services.AddScoped<IAlertService, AlertService>();
    builder.Services.AddScoped<IAlertTypeService, AlertTypeService>();
    builder.Services.AddScoped<IDashboardService, DashboardService>();
    builder.Services.AddScoped<IChartService, ChartService>();
    builder.Services.AddScoped<ISeedDataService, SeedDataService>();
    builder.Services.AddScoped<ISignalRNotificationService, SignalRNotificationService>();

    // =============================================================================
    // VALIDATION (FluentValidation)
    // =============================================================================
    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddValidatorsFromAssemblyContaining<CreateReadingValidator>();

    // =============================================================================
    // CONTROLLERS & SIGNALR
    // =============================================================================
    builder.Services.AddControllers();
    builder.Services.AddSignalR();

    // =============================================================================
    // SWAGGER / OPENAPI
    // =============================================================================
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // =============================================================================
    // CORS
    // =============================================================================
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? ["http://localhost:4200", "https://localhost:4200"];

    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });
    });

    // =============================================================================
    // BACKGROUND SERVICES
    // =============================================================================
    builder.Services.AddHostedService<SeedDataHostedService>();
    builder.Services.AddHostedService<DataRetentionService>();

    // =============================================================================
    // HEALTH CHECKS
    // =============================================================================
    builder.Services.AddHealthChecks()
        .AddNpgSql(connectionString, name: "postgresql");

    var app = builder.Build();

    // =============================================================================
    // MIDDLEWARE PIPELINE
    // =============================================================================
    
    // Serilog Request Logging
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "{RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    });

    // Exception Handling
    app.UseMiddleware<ExceptionMiddleware>();

    // Swagger (Development + Production)
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "myIoTGrid Cloud API v1");
        options.RoutePrefix = "swagger";
    });

    // CORS
    app.UseCors();

    // Tenant Middleware
    app.UseMiddleware<TenantMiddleware>();

    // Authorization (prepared for future use)
    app.UseAuthorization();

    // Health Checks
    app.MapHealthChecks("/health");
    app.MapHealthChecks("/health/ready");

    // Controllers
    app.MapControllers();

    // SignalR Hub
    app.MapHub<SensorHub>("/hubs/sensors");

    // Root endpoint
    app.MapGet("/", () => new
    {
        name = "myIoTGrid Cloud API",
        version = "1.0.0",
        status = "Running",
        timestamp = DateTime.UtcNow,
        endpoints = new
        {
            swagger = "/swagger",
            health = "/health",
            api = "/api",
            signalr = "/hubs/sensors"
        }
    });

    // =============================================================================
    // DATABASE MIGRATION (Auto-migrate in development)
    // =============================================================================
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<CloudDbContext>();
        
        if (app.Environment.IsDevelopment())
        {
            Log.Information("Applying database migrations...");
            await context.Database.MigrateAsync();
        }
        else
        {
            // In production, just ensure database exists
            await context.Database.EnsureCreatedAsync();
        }
    }

    Log.Information("myIoTGrid Cloud API started successfully");
    Log.Information("Swagger UI available at: /swagger");
    Log.Information("SignalR Hub available at: /hubs/sensors");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
