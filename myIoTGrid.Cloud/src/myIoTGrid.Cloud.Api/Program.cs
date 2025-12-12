using FluentValidation;
using FluentValidation.AspNetCore;
using HealthChecks.NpgSql;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Net;
using myIoTGrid.Cloud.Infrastructure.Data;
using myIoTGrid.Cloud.Infrastructure.Matter;
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
    // AZURE CONTAINER APPS: HTTP-only auf Port 8080
    // Azure Ingress übernimmt HTTPS-Terminierung extern
    // =============================================================================
    if (builder.Environment.IsProduction())
    {
        builder.WebHost.ConfigureKestrel(options =>
        {
            // Nur HTTP auf Port 8080 - Azure übernimmt HTTPS
            options.Listen(IPAddress.Any, 8080, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
            });
        });

        Log.Information("Production mode: Kestrel configured for HTTP on port 8080");
        Log.Information("Azure Ingress will handle HTTPS termination externally");
    }

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
    // CACHING
    // =============================================================================
    builder.Services.AddMemoryCache();

    // =============================================================================
    // REPOSITORIES
    // =============================================================================
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

    // =============================================================================
    // INFRASTRUCTURE SERVICES
    // =============================================================================
    builder.Services.AddSingleton<IMatterBridgeClient, NoOpMatterBridgeClient>();

    // =============================================================================
    // SERVICES
    // =============================================================================
    builder.Services.AddScoped<ITenantService, TenantService>();
    builder.Services.AddScoped<IEffectiveConfigService, EffectiveConfigService>();
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
    builder.Services.AddScoped<INodeDebugLogService, NodeDebugLogService>();

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
    builder.Services.AddSwaggerGen(options =>
    {
        // Custom schema IDs to avoid conflicts with nested types
        options.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
    });

    // =============================================================================
    // CORS - Konfiguriert für Azure Container Apps
    // =============================================================================
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? ["http://localhost:4200", "https://localhost:4200"];

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials(); // Wichtig für SignalR!
        });

        // Default Policy für Fallback
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

    // Swagger
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "myIoTGrid Cloud API v1");
        options.RoutePrefix = "swagger";
    });

    // Debug endpoint for Swagger errors (only in Development)
    if (app.Environment.IsDevelopment())
    {
        app.MapGet("/swagger-debug", (IServiceProvider sp) =>
        {
            try
            {
                var generator = sp.GetRequiredService<Swashbuckle.AspNetCore.Swagger.ISwaggerProvider>();
                var swagger = generator.GetSwagger("v1");
                return Results.Ok(new { success = true, message = "Swagger generated successfully" });
            }
            catch (Exception ex)
            {
                return Results.Ok(new { success = false, error = ex.Message, stackTrace = ex.StackTrace });
            }
        });
    }

    // CORS - WICHTIG: VOR Routing aktivieren!
    app.UseCors("AllowFrontend");

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
    // DATABASE MIGRATION (with retry for container startup)
    // =============================================================================
    var maxRetries = 5;
    var retryDelaySeconds = 5;

    for (var retry = 0; retry < maxRetries; retry++)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<CloudDbContext>();

            // Test database connection first
            Log.Information("Testing database connection (attempt {Attempt}/{MaxRetries})...", retry + 1, maxRetries);
            if (!await context.Database.CanConnectAsync())
            {
                throw new Exception("Database connection test failed");
            }

            if (app.Environment.IsDevelopment())
            {
                Log.Information("Applying database migrations...");
                await context.Database.MigrateAsync();
            }
            else
            {
                // In production, ensure database exists and apply migrations
                Log.Information("Applying database migrations...");
                await context.Database.MigrateAsync();
            }

            Log.Information("Database connection established successfully");
            break; // Success, exit retry loop
        }
        catch (Exception ex) when (retry < maxRetries - 1)
        {
            Log.Warning("Database connection failed (attempt {Attempt}/{MaxRetries}): {Error}. Retrying in {Delay}s...",
                retry + 1, maxRetries, ex.Message, retryDelaySeconds);
            await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Database connection failed after {MaxRetries} attempts. Application will continue without database.", maxRetries);
            // Don't throw - let the app start and fail on first DB request
            // This allows health checks and swagger to work
        }
    }

    // =============================================================================
    // STARTUP LOGGING
    // =============================================================================
    Log.Information("myIoTGrid Cloud API started successfully");

    if (app.Environment.IsProduction())
    {
        Log.Information("Mode: Production (Azure Container Apps)");
        Log.Information("Listening on: http://*:8080 (internal)");
        Log.Information("Public URL: https://api.myiotgrid.cloud");
    }
    else
    {
        Log.Information("Mode: Development");
    }

    Log.Information("Swagger UI: /swagger");
    Log.Information("SignalR Hub: /hubs/sensors");
    Log.Information("Health Check: /health");

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
