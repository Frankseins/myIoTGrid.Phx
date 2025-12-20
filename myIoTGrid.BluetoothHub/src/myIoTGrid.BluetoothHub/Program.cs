using myIoTGrid.BluetoothHub;
using myIoTGrid.BluetoothHub.Services;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/bluetooth-hub-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting myIoTGrid Bluetooth Hub Service");

    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSystemd();
    builder.Services.AddSerilog();
    builder.Services.AddHostedService<Worker>();

    // Services registrieren
    builder.Services.AddSingleton<BluetoothScannerService>();
    builder.Services.AddSingleton<DeviceConnectionManager>();
    builder.Services.AddSingleton<SensorDataProcessor>();
    builder.Services.AddSingleton<ApiForwardingService>();

    builder.Services.AddHttpClient("ApiClient", client =>
    {
        var baseUrl = builder.Configuration["BluetoothHub:ApiBaseUrl"] ?? "http://localhost:5000";
        client.BaseAddress = new Uri(baseUrl);
        client.DefaultRequestHeaders.Add("X-Via-Bluetooth-Hub",
            builder.Configuration["BluetoothHub:HubId"]);
    });

    var host = builder.Build();
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
