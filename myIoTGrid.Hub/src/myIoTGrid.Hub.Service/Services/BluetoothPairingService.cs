using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using myIoTGrid.Shared.Common.DTOs;
using myIoTGrid.Shared.Contracts.Services;

namespace myIoTGrid.Hub.Service.Services;

/// <summary>
/// Service for Bluetooth device pairing using bluetoothctl on Linux.
/// Sprint BT-02: Backend Bluetooth Pairing
/// </summary>
public class BluetoothPairingService : IBluetoothPairingService
{
    private readonly ILogger<BluetoothPairingService> _logger;
    private static readonly Regex DevicePattern = new(@"Device\s+([0-9A-Fa-f:]{17})\s+(.+)", RegexOptions.Compiled);

    public BluetoothPairingService(ILogger<BluetoothPairingService> logger)
    {
        _logger = logger;
    }

    public async Task<List<ScannedBleDeviceDto>> ScanForDevicesAsync(int timeoutSeconds = 10, CancellationToken ct = default)
    {
        var devices = new List<ScannedBleDeviceDto>();

        try
        {
            _logger.LogInformation("Starting BLE scan for {Timeout} seconds...", timeoutSeconds);

            // Start scanning in background
            var scanProcess = StartBluetoothctlProcess();
            await scanProcess.StandardInput.WriteLineAsync("scan on");

            // Wait for scan duration
            await Task.Delay(timeoutSeconds * 1000, ct);

            // Stop scanning
            await scanProcess.StandardInput.WriteLineAsync("scan off");
            await Task.Delay(500, ct); // Brief delay to collect results

            // Get devices
            await scanProcess.StandardInput.WriteLineAsync("devices");
            await Task.Delay(500, ct);

            // Read output
            var output = await ReadProcessOutputAsync(scanProcess, 2000);

            // Parse devices
            var matches = DevicePattern.Matches(output);
            foreach (Match match in matches)
            {
                var mac = match.Groups[1].Value;
                var name = match.Groups[2].Value.Trim();

                // Filter for myIoTGrid devices
                if (name.StartsWith("myIoTGrid-", StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith("ESP32-", StringComparison.OrdinalIgnoreCase))
                {
                    devices.Add(new ScannedBleDeviceDto(mac, name));
                    _logger.LogInformation("Found myIoTGrid device: {Name} ({Mac})", name, mac);
                }
            }

            // Cleanup
            await scanProcess.StandardInput.WriteLineAsync("exit");
            scanProcess.Kill();

            _logger.LogInformation("BLE scan complete. Found {Count} myIoTGrid devices", devices.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during BLE scan");
        }

        return devices;
    }

    public async Task<BlePairingResultDto> PairDeviceAsync(string macAddress, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Pairing with device {MacAddress}...", macAddress);

            var process = StartBluetoothctlProcess();

            // First, try to remove any existing pairing
            await process.StandardInput.WriteLineAsync($"remove {macAddress}");
            await Task.Delay(1000, ct);

            // Start agent for pairing
            await process.StandardInput.WriteLineAsync("agent on");
            await process.StandardInput.WriteLineAsync("default-agent");
            await Task.Delay(500, ct);

            // Scan briefly to ensure device is discoverable
            await process.StandardInput.WriteLineAsync("scan on");
            await Task.Delay(3000, ct);
            await process.StandardInput.WriteLineAsync("scan off");
            await Task.Delay(500, ct);

            // Pair
            await process.StandardInput.WriteLineAsync($"pair {macAddress}");
            await Task.Delay(5000, ct); // Wait for pairing

            var pairOutput = await ReadProcessOutputAsync(process, 2000);
            var pairingSuccess = pairOutput.Contains("Pairing successful") ||
                                  pairOutput.Contains("already paired");

            if (!pairingSuccess && pairOutput.Contains("Failed"))
            {
                _logger.LogWarning("Pairing failed: {Output}", pairOutput);
                process.Kill();
                return new BlePairingResultDto(false, macAddress, null, "Pairing failed: " + pairOutput);
            }

            // Trust the device for auto-reconnect
            await process.StandardInput.WriteLineAsync($"trust {macAddress}");
            await Task.Delay(1000, ct);

            // Get device info
            await process.StandardInput.WriteLineAsync($"info {macAddress}");
            await Task.Delay(500, ct);
            var infoOutput = await ReadProcessOutputAsync(process, 1000);

            // Extract device name
            var nameMatch = Regex.Match(infoOutput, @"Name:\s+(.+)");
            var deviceName = nameMatch.Success ? nameMatch.Groups[1].Value.Trim() : null;

            await process.StandardInput.WriteLineAsync("exit");
            process.Kill();

            _logger.LogInformation("Successfully paired with {MacAddress} ({Name})", macAddress, deviceName);

            return new BlePairingResultDto(true, macAddress, deviceName, "Pairing successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pairing with device {MacAddress}", macAddress);
            return new BlePairingResultDto(false, macAddress, null, ex.Message);
        }
    }

    public async Task<bool> TrustDeviceAsync(string macAddress, CancellationToken ct = default)
    {
        try
        {
            var result = await RunBluetoothctlCommandAsync($"trust {macAddress}", ct);
            return result.Contains("trust succeeded") || result.Contains("Changing trust succeeded");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error trusting device {MacAddress}", macAddress);
            return false;
        }
    }

    public async Task<bool> UnpairDeviceAsync(string macAddress, CancellationToken ct = default)
    {
        try
        {
            var result = await RunBluetoothctlCommandAsync($"remove {macAddress}", ct);
            return result.Contains("Device has been removed") || result.Contains("not available");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unpairing device {MacAddress}", macAddress);
            return false;
        }
    }

    public async Task<bool> IsDevicePairedAsync(string macAddress, CancellationToken ct = default)
    {
        try
        {
            var result = await RunBluetoothctlCommandAsync($"info {macAddress}", ct);
            return result.Contains("Paired: yes");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking paired status for {MacAddress}", macAddress);
            return false;
        }
    }

    public async Task<List<ScannedBleDeviceDto>> GetPairedDevicesAsync(CancellationToken ct = default)
    {
        var devices = new List<ScannedBleDeviceDto>();

        try
        {
            var result = await RunBluetoothctlCommandAsync("paired-devices", ct);

            var matches = DevicePattern.Matches(result);
            foreach (Match match in matches)
            {
                var mac = match.Groups[1].Value;
                var name = match.Groups[2].Value.Trim();
                devices.Add(new ScannedBleDeviceDto(mac, name));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paired devices");
        }

        return devices;
    }

    private async Task<string> RunBluetoothctlCommandAsync(string command, CancellationToken ct)
    {
        var process = StartBluetoothctlProcess();

        await process.StandardInput.WriteLineAsync(command);
        await Task.Delay(1000, ct);

        var output = await ReadProcessOutputAsync(process, 1000);

        await process.StandardInput.WriteLineAsync("exit");
        process.Kill();

        return output;
    }

    private Process StartBluetoothctlProcess()
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "bluetoothctl",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        return process;
    }

    private async Task<string> ReadProcessOutputAsync(Process process, int timeoutMs)
    {
        var output = new System.Text.StringBuilder();

        var readTask = Task.Run(async () =>
        {
            var buffer = new char[4096];
            while (process.StandardOutput.Peek() >= 0)
            {
                var read = await process.StandardOutput.ReadAsync(buffer, 0, buffer.Length);
                if (read > 0)
                    output.Append(buffer, 0, read);
            }
        });

        await Task.WhenAny(readTask, Task.Delay(timeoutMs));

        return output.ToString();
    }
}

