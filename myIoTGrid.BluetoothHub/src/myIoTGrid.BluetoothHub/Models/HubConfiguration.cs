namespace myIoTGrid.BluetoothHub.Models;

public class HubConfiguration
{
    public required string HubId { get; set; }
    public required string HubName { get; set; }
    public required string ApiBaseUrl { get; set; }
    public int ScanInterval { get; set; } = 30000;
    public int ReconnectDelay { get; set; } = 5000;
    public required string ServiceUUID { get; set; }
    public required string SensorDataUUID { get; set; }
    public List<RegisteredDevice> RegisteredDevices { get; set; } = new();
}
