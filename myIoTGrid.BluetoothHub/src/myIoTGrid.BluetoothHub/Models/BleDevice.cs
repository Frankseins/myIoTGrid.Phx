namespace myIoTGrid.BluetoothHub.Models;

public class BleDevice
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string MacAddress { get; set; }
    public required string NodeId { get; set; }
    public int Rssi { get; set; }
    public bool IsConnected { get; set; }
    public DateTime LastSeen { get; set; }
    public DateTime? LastConnected { get; set; }
}

public class RegisteredDevice
{
    public required string Name { get; set; }
    public required string NodeId { get; set; }
    public string? MacAddress { get; set; }
    public List<string> ExpectedSensors { get; set; } = new();
}
