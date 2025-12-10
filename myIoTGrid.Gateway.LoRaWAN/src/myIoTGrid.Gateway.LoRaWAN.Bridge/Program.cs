using myIoTGrid.Gateway.LoRaWAN.Bridge.Decoders;
using myIoTGrid.Gateway.LoRaWAN.Bridge.Services;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// Configure Services
// ============================================

// Add health checks
builder.Services.AddHealthChecks();

// Register Payload Decoder
builder.Services.AddSingleton<IPayloadDecoder, MyIoTGridDecoder>();

// Register MQTT Services as Singletons (they manage their own connections)
builder.Services.AddSingleton<ChirpStackSubscriber>();
builder.Services.AddSingleton<MyIoTGridPublisher>();

// Register Bridge Orchestrator
builder.Services.AddSingleton<BridgeOrchestrator>();

// Register Background Services
builder.Services.AddHostedService(sp => sp.GetRequiredService<ChirpStackSubscriber>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<MyIoTGridPublisher>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<BridgeOrchestrator>());

// ============================================
// Build Application
// ============================================

var app = builder.Build();

// ============================================
// Configure Pipeline
// ============================================

// Health check endpoint
app.MapHealthChecks("/health");

// Root endpoint
app.MapGet("/", () => new
{
    Service = "myIoTGrid.Gateway.LoRaWAN.Bridge",
    Version = "1.0.0",
    Status = "Running",
    Timestamp = DateTime.UtcNow
});

// Status endpoint
app.MapGet("/status", (BridgeOrchestrator orchestrator, ChirpStackSubscriber subscriber, MyIoTGridPublisher publisher) => new
{
    Service = "myIoTGrid.Gateway.LoRaWAN.Bridge",
    Version = "1.0.0",
    Status = "Running",
    Connections = new
    {
        ChirpStack = subscriber.IsConnected,
        MyIoTGrid = publisher.IsConnected
    },
    Statistics = new
    {
        UplinksReceived = subscriber.UplinksReceived,
        JoinsReceived = subscriber.JoinsReceived,
        ReadingsPublished = publisher.ReadingsPublished,
        Errors = subscriber.Errors + publisher.Errors,
        LastUplinkAt = subscriber.LastUplinkAt,
        StartedAt = orchestrator.StartedAt,
        Uptime = DateTime.UtcNow - orchestrator.StartedAt
    },
    Timestamp = DateTime.UtcNow
});

// ============================================
// Run Application
// ============================================

app.Logger.LogInformation("myIoTGrid.Gateway.LoRaWAN.Bridge starting...");
app.Logger.LogInformation("ChirpStack MQTT: {Server}", builder.Configuration["ChirpStack:MqttServer"]);
app.Logger.LogInformation("myIoTGrid MQTT: {Server}", builder.Configuration["MyIoTGrid:MqttServer"]);

app.Run();
