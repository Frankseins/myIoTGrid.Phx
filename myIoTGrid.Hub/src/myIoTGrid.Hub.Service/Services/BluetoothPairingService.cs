using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using myIoTGrid.Shared.Common.DTOs;
using myIoTGrid.Shared.Contracts.Services;

namespace myIoTGrid.Hub.Service.Services;

/// <summary>
/// Service for Bluetooth device pairing using hcitool/btmgmt on Linux.
/// Uses hcitool lescan for BLE scanning (more reliable than bluetoothctl).
/// Sprint BT-02: Backend Bluetooth Pairing
/// </summary>
public class BluetoothPairingService : IBluetoothPairingService
{
    private readonly ILogger<BluetoothPairingService> _logger;
    private static readonly Regex DevicePattern = new(@"Device\s+([0-9A-Fa-f:]{17})\s+(.+)", RegexOptions.Compiled);
    // hcitool lescan output: "00:70:07:84:92:CE myIoTGrid-92CC" or "00:70:07:84:92:CE (unknown)"
    private static readonly Regex HcitoolPattern = new(@"([0-9A-Fa-f:]{17})\s+(.+)", RegexOptions.Compiled);
    // btmgmt find output: "hci0 dev_found: 00:70:07:84:92:CE type LE Public"
    private static readonly Regex BtmgmtPattern = new(@"dev_found:\s+([0-9A-Fa-f:]{17})\s+type\s+LE", RegexOptions.Compiled);

    public BluetoothPairingService(ILogger<BluetoothPairingService> logger)
    {
        _logger = logger;
    }

    public async Task<List<ScannedBleDeviceDto>> ScanForDevicesAsync(int timeoutSeconds = 10, CancellationToken ct = default)
    {
        var devices = new List<ScannedBleDeviceDto>();
        var foundMacs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            _logger.LogInformation("Starting BLE scan using hcitool lescan for {Timeout} seconds...", timeoutSeconds);

            // Use hcitool lescan which works better for BLE devices
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "sudo",
                    Arguments = "hcitool lescan",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            var output = new StringBuilder();
            process.OutputDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    output.AppendLine(args.Data);
                    _logger.LogDebug("hcitool: {Line}", args.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();

            // Wait for scan duration
            await Task.Delay(timeoutSeconds * 1000, ct);

            // Kill the process (hcitool lescan runs indefinitely)
            try
            {
                process.Kill();
                await process.WaitForExitAsync(ct);
            }
            catch { /* Process may have already exited */ }

            // Parse output
            var lines = output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var match = HcitoolPattern.Match(line);
                if (match.Success)
                {
                    var mac = match.Groups[1].Value.ToUpperInvariant();
                    var name = match.Groups[2].Value.Trim();

                    // Skip duplicates
                    if (foundMacs.Contains(mac))
                        continue;

                    // Filter for myIoTGrid devices or include all if name contains our prefix
                    if (name.StartsWith("myIoTGrid-", StringComparison.OrdinalIgnoreCase) ||
                        name.StartsWith("ESP32-", StringComparison.OrdinalIgnoreCase))
                    {
                        foundMacs.Add(mac);
                        devices.Add(new ScannedBleDeviceDto(mac, name));
                        _logger.LogInformation("Found myIoTGrid device: {Name} ({Mac})", name, mac);
                    }
                    // Also check for our known MAC pattern (00:70:07:84:92:xx)
                    else if (mac.StartsWith("00:70:07:84:92:", StringComparison.OrdinalIgnoreCase))
                    {
                        foundMacs.Add(mac);
                        var deviceName = name == "(unknown)" ? $"myIoTGrid-{mac[^2..]}" : name;
                        devices.Add(new ScannedBleDeviceDto(mac, deviceName));
                        _logger.LogInformation("Found likely myIoTGrid device by MAC: {Name} ({Mac})", deviceName, mac);
                    }
                }
            }

            _logger.LogInformation("BLE scan complete. Found {Count} myIoTGrid devices", devices.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during BLE scan with hcitool");

            // Fallback to btmgmt find
            _logger.LogInformation("Trying fallback scan with btmgmt find...");
            devices = await ScanWithBtmgmtAsync(timeoutSeconds, ct);
        }

        return devices;
    }

    private async Task<List<ScannedBleDeviceDto>> ScanWithBtmgmtAsync(int timeoutSeconds, CancellationToken ct)
    {
        var devices = new List<ScannedBleDeviceDto>();
        var foundMacs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "sudo",
                    Arguments = "btmgmt find",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();

            // btmgmt find has its own timeout, wait for it to complete
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var timeoutTask = Task.Delay(timeoutSeconds * 1000, ct);

            await Task.WhenAny(outputTask, timeoutTask);

            try
            {
                process.Kill();
            }
            catch { }

            var output = outputTask.IsCompleted ? await outputTask : "";

            // Parse btmgmt output for LE devices
            var matches = BtmgmtPattern.Matches(output);
            foreach (Match match in matches)
            {
                var mac = match.Groups[1].Value.ToUpperInvariant();

                if (foundMacs.Contains(mac))
                    continue;

                // Check for our known MAC pattern
                if (mac.StartsWith("00:70:07:84:92:", StringComparison.OrdinalIgnoreCase))
                {
                    foundMacs.Add(mac);
                    var deviceName = $"myIoTGrid-{mac[^2..]}";
                    devices.Add(new ScannedBleDeviceDto(mac, deviceName));
                    _logger.LogInformation("Found likely myIoTGrid device via btmgmt: {Name} ({Mac})", deviceName, mac);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during btmgmt scan");
        }

        return devices;
    }

    public async Task<BlePairingResultDto> PairDeviceAsync(string macAddress, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Pairing with BLE device {MacAddress}...", macAddress);

            // For BLE devices, we need to:
            // 1. Run hcitool lescan briefly to make the device known to BlueZ
            // 2. Use bluetoothctl to pair/trust

            // Step 1: Quick scan to register device with BlueZ
            _logger.LogDebug("Running quick BLE scan to register device...");
            var scanProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "sudo",
                    Arguments = "timeout 5 hcitool lescan",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            scanProcess.Start();
            await scanProcess.WaitForExitAsync(ct);

            // Step 2: Use bluetoothctl with timeout for each command
            _logger.LogDebug("Starting bluetoothctl pairing sequence...");

            // Remove existing pairing first
            await RunBluetoothctlCommandWithTimeoutAsync($"remove {macAddress}", 2, ct);

            // Set up agent
            await RunBluetoothctlCommandWithTimeoutAsync("agent on", 1, ct);
            await RunBluetoothctlCommandWithTimeoutAsync("default-agent", 1, ct);

            // Start LE scan in bluetoothctl
            await RunBluetoothctlCommandWithTimeoutAsync("menu scan", 1, ct);
            await RunBluetoothctlCommandWithTimeoutAsync("transport le", 1, ct);
            await RunBluetoothctlCommandWithTimeoutAsync("back", 1, ct);
            await RunBluetoothctlCommandWithTimeoutAsync("scan on", 1, ct);

            // Wait for device discovery
            await Task.Delay(5000, ct);

            await RunBluetoothctlCommandWithTimeoutAsync("scan off", 1, ct);

            // Try to pair
            var pairResult = await RunBluetoothctlCommandWithTimeoutAsync($"pair {macAddress}", 10, ct);
            _logger.LogDebug("Pair result: {Result}", pairResult);

            var pairingSuccess = pairResult.Contains("Pairing successful") ||
                                  pairResult.Contains("already paired") ||
                                  pairResult.Contains("org.bluez.Error.AlreadyExists");

            // Trust the device for auto-reconnect
            var trustResult = await RunBluetoothctlCommandWithTimeoutAsync($"trust {macAddress}", 3, ct);
            _logger.LogDebug("Trust result: {Result}", trustResult);

            // Get device info
            var infoResult = await RunBluetoothctlCommandWithTimeoutAsync($"info {macAddress}", 2, ct);

            // Extract device name
            var nameMatch = Regex.Match(infoResult, @"Name:\s+(.+)");
            var deviceName = nameMatch.Success ? nameMatch.Groups[1].Value.Trim() : $"myIoTGrid-{macAddress[^2..]}";

            // Check if device is now paired
            var isPaired = infoResult.Contains("Paired: yes");

            if (isPaired || pairingSuccess)
            {
                _logger.LogInformation("Successfully paired with {MacAddress} ({Name})", macAddress, deviceName);
                return new BlePairingResultDto(true, macAddress, deviceName, "Pairing successful");
            }
            else
            {
                _logger.LogWarning("Pairing may have failed for {MacAddress}. Info: {Info}", macAddress, infoResult);
                return new BlePairingResultDto(false, macAddress, deviceName, "Pairing status uncertain - device may not support pairing");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pairing with device {MacAddress}", macAddress);
            return new BlePairingResultDto(false, macAddress, null, ex.Message);
        }
    }

    private async Task<string> RunBluetoothctlCommandWithTimeoutAsync(string command, int timeoutSeconds, CancellationToken ct)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "bash",
                    Arguments = $"-c \"echo '{command}' | timeout {timeoutSeconds} bluetoothctl 2>&1\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync(ct);

            return output;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error running bluetoothctl command: {Command}", command);
            return string.Empty;
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

