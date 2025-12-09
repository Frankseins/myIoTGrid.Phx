using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using myIoTGrid.Shared.Common.Interfaces;
using myIoTGrid.Hub.Interface.BackgroundServices;
using MatterBridgeService = myIoTGrid.Hub.Interface.BackgroundServices.MatterBridgeService;
using myIoTGrid.Hub.Infrastructure.Data;
using myIoTGrid.Hub.Infrastructure.Matter;
using myIoTGrid.Hub.Infrastructure.Mqtt;
using myIoTGrid.Hub.Infrastructure.Repositories;
using myIoTGrid.Hub.Interface.Hubs;
using myIoTGrid.Hub.Interface.Middleware;
using myIoTGrid.Hub.Interface.Services;
using myIoTGrid.Shared.Contracts.Services;
using myIoTGrid.Hub.Service.Services;
using myIoTGrid.Hub.Service.Validators;
using myIoTGrid.Shared.Utilities.Converters;
using myIoTGrid.Shared.Common.Options;
using Serilog;
using Serilog.Events;

// ===========================================
// Serilog Bootstrap Logger
// ===========================================
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("myIoTGrid Hub API wird gestartet...");

    var builder = WebApplication.CreateBuilder(args);

    // ===========================================
    // Kestrel TLS Configuration
    // TLS 1.2 for ESP32 mbedTLS + TLS 1.3 for modern browsers
    // ===========================================
    builder.WebHost.ConfigureKestrel(serverOptions =>
    {
        serverOptions.ConfigureHttpsDefaults(httpsOptions =>
        {
            // Support both TLS 1.2 (ESP32) and TLS 1.3 (Chrome/Firefox)
            httpsOptions.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;

            // Log TLS handshakes for debugging
            httpsOptions.OnAuthenticate = (connectionContext, sslOptions) =>
            {
                Log.Debug("TLS Handshake from {RemoteEndPoint}", connectionContext.RemoteEndPoint);
                // Let the system negotiate the best cipher suite
                // This allows Chrome to use TLS 1.3 and ESP32 to use TLS 1.2
            };
        });

        // Enable connection logging
        serverOptions.ConfigureEndpointDefaults(listenOptions =>
        {
            listenOptions.Use(next => async context =>
            {
                Log.Debug("TCP Connection from {RemoteEndPoint}", context.RemoteEndPoint);
                try
                {
                    await next(context);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Connection from {RemoteEndPoint} failed: {Message}", context.RemoteEndPoint, ex.Message);
                    throw;
                }
            });
        });
    });

    // ===========================================
    // Serilog Configuration from appsettings.json
    // ===========================================
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithEnvironmentName()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .Enrich.WithProperty("Application", "myIoTGrid.Hub.Api")
    );

    // ===========================================
    // Service Registration
    // ===========================================

    // Configuration Options
    builder.Services.Configure<MonitoringOptions>(
        builder.Configuration.GetSection(MonitoringOptions.SectionName));
    builder.Services.Configure<DiscoveryOptions>(
        builder.Configuration.GetSection(DiscoveryOptions.SectionName));

    // DbContext mit SQLite
    builder.Services.AddDbContext<HubDbContext>(options =>
        options.UseSqlite(
            builder.Configuration.GetConnectionString("HubDb") ?? "Data Source=./data/hub.db",
            sqliteOptions => sqliteOptions.MigrationsAssembly("myIoTGrid.Hub.Infrastructure")
        ));

    // Unit of Work
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

    // Services (Scoped)
    builder.Services.AddScoped<ITenantService, TenantService>();
    builder.Services.AddScoped<IAlertTypeService, AlertTypeService>();
    builder.Services.AddScoped<IHubService, HubService>();
    builder.Services.AddScoped<INodeService, NodeService>();
    builder.Services.AddScoped<ISensorService, SensorService>();
    builder.Services.AddScoped<INodeSensorAssignmentService, NodeSensorAssignmentService>();
    builder.Services.AddScoped<IEffectiveConfigService, EffectiveConfigService>();
    builder.Services.AddScoped<IReadingService, ReadingService>();
    builder.Services.AddScoped<IAlertService, AlertService>();
    builder.Services.AddScoped<ISignalRNotificationService, SignalRNotificationService>();
    builder.Services.AddScoped<ISeedDataService, SeedDataService>();
    builder.Services.AddScoped<IDashboardService, DashboardService>();
    builder.Services.AddScoped<IChartService, ChartService>();
    builder.Services.AddScoped<INodeDebugLogService, NodeDebugLogService>();
    builder.Services.AddScoped<INodeHardwareStatusService, NodeHardwareStatusService>();

    // Memory Cache for Sensors
    builder.Services.AddMemoryCache();

    // FluentValidation
    builder.Services.AddValidatorsFromAssemblyContaining<CreateReadingValidator>();

    // SignalR
    builder.Services.AddSignalR();

    // ===========================================
    // MQTT Client (DEAKTIVIERT - wird später implementiert)
    // ===========================================
    // TODO: MQTT-Support aktivieren wenn Mosquitto Broker verfügbar ist
    // Für lokale Entwicklung nutzen wir erstmal nur REST API.
    // Um MQTT zu aktivieren:
    // 1. Mosquitto starten: docker run -d -p 1883:1883 eclipse-mosquitto:2
    // 2. Die folgenden Zeilen einkommentieren:
    //
    // builder.Services.Configure<MqttOptions>(builder.Configuration.GetSection(MqttOptions.SectionName));
    // builder.Services.AddSingleton<IMqttMessageHandler, myIoTGrid.Hub.Interface.Mqtt.ReadingMqttHandler>();
    // builder.Services.AddSingleton<IMqttMessageHandler, myIoTGrid.Hub.Interface.Mqtt.HubStatusMqttHandler>();
    // builder.Services.AddHostedService<MqttClientService>();

    // Matter Bridge Client
    builder.Services.Configure<MatterBridgeOptions>(
        builder.Configuration.GetSection(MatterBridgeOptions.SectionName));
    builder.Services.AddHttpClient<IMatterBridgeClient, MatterBridgeClient>();

    // Background Services
    builder.Services.AddHostedService<SeedDataHostedService>();
    builder.Services.AddHostedService<NodeMonitorService>();
    builder.Services.AddHostedService<HubMonitorService>();
    builder.Services.AddHostedService<DataRetentionService>();
    builder.Services.AddHostedService<MatterBridgeService>();
    builder.Services.AddHostedService<DiscoveryService>();
    builder.Services.AddHostedService<DebugLogCleanupService>();

    // Controllers (aus Interface-Projekt)
    builder.Services.AddControllers()
        .AddApplicationPart(typeof(myIoTGrid.Hub.Interface.Controllers.ReadingsController).Assembly)
        .AddJsonOptions(options =>
        {
            // Serialize enums as strings
            options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            // Serialize DateTimes as UTC with 'Z' suffix for proper browser timezone handling
            options.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter());
            options.JsonSerializerOptions.Converters.Add(new UtcNullableDateTimeConverter());
        });

    // Swagger/OpenAPI
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Health Checks
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<HubDbContext>("database");

    // CORS für lokales Frontend
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins(
                    builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                    ?? ["http://localhost:4200", "https://localhost:4200"]
                )
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    var app = builder.Build();

    // ===========================================
    // Middleware Pipeline
    // ===========================================

    // Datenbank-Verzeichnis erstellen falls nicht vorhanden
    var dataDir = Path.Combine(app.Environment.ContentRootPath, "data");
    if (!Directory.Exists(dataDir))
    {
        Directory.CreateDirectory(dataDir);
    }

    // Serilog Request Logging (ganz am Anfang!)
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.GetLevel = (httpContext, elapsed, ex) => ex != null
            ? LogEventLevel.Error
            : httpContext.Response.StatusCode > 499
                ? LogEventLevel.Error
                : elapsed > 1000
                    ? LogEventLevel.Warning
                    : LogEventLevel.Information;
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? "unknown");
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString() ?? "unknown");
        };
    });

    // Exception Middleware
    app.UseExceptionMiddleware();

    // Swagger (in Development und Production für lokale Tests)
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "myIoTGrid Hub API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "myIoTGrid Hub API";
    });

    app.UseCors();

    // Tenant Middleware (vor Authorization)
    app.UseTenantMiddleware();

    app.UseAuthorization();

    // Health Checks
    app.MapHealthChecks("/health");
    app.MapHealthChecks("/health/ready");

    app.MapControllers();

    // SignalR Hub
    app.MapHub<SensorHub>("/hubs/sensors");

    // Willkommensnachricht im Root
    app.MapGet("/", () => Results.Ok(new
    {
        Name = "myIoTGrid Hub API",
        Version = "1.0.0",
        Status = "Running",
        Documentation = "/swagger",
        Health = "/health"
    }));

    Log.Information("myIoTGrid Hub API wurde erfolgreich gestartet");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "myIoTGrid Hub API ist mit einem Fehler abgestürzt");
    throw;
}
finally
{
    Log.Information("myIoTGrid Hub API wird beendet");
    Log.CloseAndFlush();
}
