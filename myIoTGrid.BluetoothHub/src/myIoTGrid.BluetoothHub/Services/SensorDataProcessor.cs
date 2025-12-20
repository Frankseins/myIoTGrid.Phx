using System.Text;
using System.Text.Json;
using myIoTGrid.BluetoothHub.Models;

namespace myIoTGrid.BluetoothHub.Services;

public class SensorDataProcessor
{
    private readonly ILogger<SensorDataProcessor> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public SensorDataProcessor(ILogger<SensorDataProcessor> logger)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
    }

    public SensorData? ParseSensorData(byte[] data)
    {
        try
        {
            var json = Encoding.UTF8.GetString(data);
            _logger.LogDebug("Received JSON ({bytes} bytes): {json}", data.Length, json);

            var sensorData = JsonSerializer.Deserialize<SensorData>(json, _jsonOptions);

            if (sensorData == null)
            {
                _logger.LogWarning("Failed to deserialize sensor data - result was null");
                return null;
            }

            _logger.LogDebug("Successfully parsed sensor data from {nodeId}", sensorData.NodeId);
            return sensorData;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error: {message}", ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error parsing sensor data");
            return null;
        }
    }

    public SensorData? ParseSensorData(string json)
    {
        return ParseSensorData(Encoding.UTF8.GetBytes(json));
    }

    public bool ValidateSensorData(SensorData data)
    {
        if (string.IsNullOrWhiteSpace(data.NodeId))
        {
            _logger.LogWarning("Invalid sensor data: NodeId is missing or empty");
            return false;
        }

        if (string.IsNullOrWhiteSpace(data.Timestamp))
        {
            _logger.LogWarning("Invalid sensor data: Timestamp is missing or empty");
            return false;
        }

        // Validate timestamp format
        if (!DateTime.TryParse(data.Timestamp, out var timestamp))
        {
            _logger.LogWarning("Invalid timestamp format: {timestamp}", data.Timestamp);
            return false;
        }

        // Check if timestamp is not too old (> 5 minutes)
        var age = DateTime.UtcNow - timestamp.ToUniversalTime();
        if (age.TotalMinutes > 5)
        {
            _logger.LogWarning("Sensor data too old: {age:F1} minutes (from {nodeId})",
                age.TotalMinutes, data.NodeId);
            return false;
        }

        // Check if timestamp is not in the future (> 1 minute tolerance)
        if (age.TotalMinutes < -1)
        {
            _logger.LogWarning("Sensor data timestamp is in the future: {timestamp} (from {nodeId})",
                data.Timestamp, data.NodeId);
            return false;
        }

        // Check if at least one sensor reading exists
        var hasData = data.Sensors.Temperature.HasValue ||
                     data.Sensors.Humidity.HasValue ||
                     data.Sensors.Pressure.HasValue ||
                     data.Sensors.Uv.HasValue ||
                     data.Sensors.WaterLevel.HasValue ||
                     data.Sensors.Gps != null;

        if (!hasData)
        {
            _logger.LogWarning("No sensor readings in data from {nodeId}", data.NodeId);
            return false;
        }

        // Validate sensor value ranges
        if (data.Sensors.Temperature.HasValue &&
            (data.Sensors.Temperature < -100 || data.Sensors.Temperature > 100))
        {
            _logger.LogWarning("Temperature out of range: {temp}°C", data.Sensors.Temperature);
            return false;
        }

        if (data.Sensors.Humidity.HasValue &&
            (data.Sensors.Humidity < 0 || data.Sensors.Humidity > 100))
        {
            _logger.LogWarning("Humidity out of range: {humidity}%", data.Sensors.Humidity);
            return false;
        }

        if (data.Sensors.Pressure.HasValue &&
            (data.Sensors.Pressure < 300 || data.Sensors.Pressure > 1100))
        {
            _logger.LogWarning("Pressure out of range: {pressure} hPa", data.Sensors.Pressure);
            return false;
        }

        _logger.LogDebug("Sensor data validation passed for {nodeId}", data.NodeId);
        return true;
    }

    public SensorData EnrichSensorData(SensorData data, BleDevice device)
    {
        // Add RSSI if not provided
        if (!data.Rssi.HasValue && device.Rssi != 0)
        {
            data.Rssi = device.Rssi;
            _logger.LogDebug("Added device RSSI {rssi} to sensor data", device.Rssi);
        }

        // Ensure NodeId matches device
        if (data.NodeId != device.NodeId)
        {
            _logger.LogWarning(
                "NodeId mismatch: data={dataNodeId}, device={deviceNodeId}. Using device NodeId.",
                data.NodeId, device.NodeId);
            // We don't change the NodeId here - just log the mismatch
        }

        return data;
    }

    public string SerializeSensorData(SensorData data)
    {
        return JsonSerializer.Serialize(data, _jsonOptions);
    }
}
