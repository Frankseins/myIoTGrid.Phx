#!/usr/bin/env python3
"""
myIoTGrid BLE Hybrid Client
Combines:
1. Beacon scanning: Read sensor data from advertising packets (no connection)
2. GATT client: Connect for bidirectional config exchange

Usage:
    source ~/ble-test/bin/activate
    pip install bleak
    python ble_hybrid_client.py [command]

Commands:
    scan        - Scan for myIoTGrid devices and show sensor data
    connect     - Connect and read device info
    config      - Send config to device
    monitor     - Continuous monitoring with optional connection

Sprint BT-01: Bluetooth Infrastructure (Hybrid Mode)
"""

import asyncio
import struct
import sys
import json
from datetime import datetime
from bleak import BleakScanner, BleakClient

# Service and Characteristic UUIDs (must match ESP32)
CONFIG_SERVICE_UUID = "4d494f54-4752-4944-434f-4e4649470000"
CONFIG_WRITE_CHAR_UUID = "4d494f54-4752-4944-434f-4e4649470001"
CONFIG_READ_CHAR_UUID = "4d494f54-4752-4944-434f-4e4649470002"
SENSOR_DATA_CHAR_UUID = "4d494f54-4752-4944-434f-4e4649470003"

# Command codes
CMD_SET_WIFI = 0x01
CMD_SET_HUB_URL = 0x02
CMD_SET_NODE_ID = 0x03
CMD_SET_INTERVAL = 0x04
CMD_FACTORY_RESET = 0xFE
CMD_REBOOT = 0xFF

# Response codes
RESP_OK = 0x00
RESP_ERROR = 0x01
RESP_INVALID_CMD = 0x02
RESP_INVALID_DATA = 0x03

# myIoTGrid beacon constants
MYIOTGRID_COMPANY_ID = 0xFFFF


def parse_advertising_data(manufacturer_data: bytes) -> dict:
    """Parse sensor data from advertising manufacturer data.

    Format: [temp:2][humidity:2][pressure:2][battery:2] = 8 bytes
    Company ID (0xFFFF) is prepended by bleak
    """
    if len(manufacturer_data) < 10:  # 2 bytes company ID + 8 bytes data
        return None

    try:
        # Unpack: company_id(2) + temp(2) + humidity(2) + pressure(2) + battery(2)
        company_id, temp_raw, humidity_raw, pressure_raw, battery = struct.unpack(
            '<HhhHH', manufacturer_data[:10]
        )

        return {
            'temperature': temp_raw / 100.0,
            'humidity': humidity_raw / 100.0,
            'pressure': (pressure_raw + 50000) / 100.0,
            'battery_mv': battery,
        }
    except Exception as e:
        print(f"Parse error: {e}")
        return None


class BLEHybridClient:
    """BLE Hybrid client for myIoTGrid sensors"""

    def __init__(self):
        self.target_device = None
        self.target_address = None
        self.last_sensor_data = {}

    async def scan_for_devices(self, timeout: float = 10.0) -> list:
        """Scan for myIoTGrid devices and return list of found devices"""
        devices_found = []

        def detection_callback(device, advertisement_data):
            name = device.name or ""
            if not name.startswith("myIoTGrid"):
                return

            # Parse advertising data
            sensor_data = None
            if advertisement_data.manufacturer_data:
                for company_id, data in advertisement_data.manufacturer_data.items():
                    full_data = struct.pack('<H', company_id) + bytes(data)
                    sensor_data = parse_advertising_data(full_data)
                    if sensor_data:
                        break

            device_info = {
                'name': name,
                'address': device.address,
                'rssi': advertisement_data.rssi,
                'sensor_data': sensor_data
            }

            # Check if already found
            existing = next((d for d in devices_found if d['address'] == device.address), None)
            if existing:
                existing.update(device_info)
            else:
                devices_found.append(device_info)

        print(f"Scanning for myIoTGrid devices ({timeout}s)...")
        scanner = BleakScanner(detection_callback=detection_callback)

        await scanner.start()
        await asyncio.sleep(timeout)
        await scanner.stop()

        return devices_found

    async def connect_and_read(self, address: str) -> dict:
        """Connect to device and read device info via GATT"""
        print(f"\nConnecting to {address}...")

        result = {
            'connected': False,
            'device_info': None,
            'sensor_data': None,
            'error': None
        }

        try:
            async with BleakClient(address, timeout=30.0) as client:
                print(f"Connected: {client.is_connected}")
                result['connected'] = True

                # Discover services
                print("\nDiscovering services...")
                for service in client.services:
                    if CONFIG_SERVICE_UUID.lower() in service.uuid.lower():
                        print(f"Found config service: {service.uuid}")

                        for char in service.characteristics:
                            print(f"  Characteristic: {char.uuid}")
                            print(f"    Properties: {', '.join(char.properties)}")

                            # Read device info
                            if CONFIG_READ_CHAR_UUID.lower() in char.uuid.lower():
                                try:
                                    data = await client.read_gatt_char(char.uuid)
                                    device_info = data.decode('utf-8')
                                    result['device_info'] = json.loads(device_info)
                                    print(f"\n  Device Info: {device_info}")
                                except Exception as e:
                                    print(f"  Error reading device info: {e}")

                            # Read sensor data
                            if SENSOR_DATA_CHAR_UUID.lower() in char.uuid.lower():
                                try:
                                    data = await client.read_gatt_char(char.uuid)
                                    sensor_json = data.decode('utf-8')
                                    result['sensor_data'] = json.loads(sensor_json)
                                    print(f"  Sensor Data: {sensor_json}")
                                except Exception as e:
                                    print(f"  Error reading sensor data: {e}")

        except Exception as e:
            result['error'] = str(e)
            print(f"Connection error: {e}")

        return result

    async def send_config(self, address: str, command: int, data: bytes = b'') -> bool:
        """Send config command to device via GATT"""
        print(f"\nConnecting to {address} to send config...")

        try:
            async with BleakClient(address, timeout=30.0) as client:
                print(f"Connected: {client.is_connected}")

                # Build command packet
                packet = bytes([command]) + data
                print(f"Sending command: 0x{command:02X}, data length: {len(data)}")

                # Write to config characteristic
                await client.write_gatt_char(CONFIG_WRITE_CHAR_UUID, packet, response=True)
                print("Command sent successfully")

                # Wait for response (read the config read characteristic)
                await asyncio.sleep(0.5)
                response = await client.read_gatt_char(CONFIG_READ_CHAR_UUID)

                if response and len(response) > 0:
                    resp_code = response[0]
                    resp_name = {
                        RESP_OK: "OK",
                        RESP_ERROR: "ERROR",
                        RESP_INVALID_CMD: "INVALID_CMD",
                        RESP_INVALID_DATA: "INVALID_DATA"
                    }.get(resp_code, f"UNKNOWN(0x{resp_code:02X})")
                    print(f"Response: {resp_name}")
                    return resp_code == RESP_OK

                return True

        except Exception as e:
            print(f"Error: {e}")
            return False

    async def monitor(self, duration: float = 60.0):
        """Continuous monitoring of myIoTGrid devices"""
        print(f"Monitoring for {duration} seconds...")
        print("=" * 60)

        seen_devices = {}

        def detection_callback(device, advertisement_data):
            name = device.name or ""
            if not name.startswith("myIoTGrid"):
                return

            # Parse advertising data
            sensor_data = None
            if advertisement_data.manufacturer_data:
                for company_id, data in advertisement_data.manufacturer_data.items():
                    full_data = struct.pack('<H', company_id) + bytes(data)
                    sensor_data = parse_advertising_data(full_data)
                    if sensor_data:
                        break

            # Only print if data changed or new device
            current = json.dumps(sensor_data) if sensor_data else ""
            if device.address not in seen_devices or seen_devices[device.address] != current:
                seen_devices[device.address] = current
                timestamp = datetime.now().strftime("%H:%M:%S")

                print(f"\n[{timestamp}] {name} ({device.address})")
                print(f"  RSSI: {advertisement_data.rssi} dBm")

                if sensor_data:
                    print(f"  Temperature: {sensor_data['temperature']:.2f} °C")
                    print(f"  Humidity:    {sensor_data['humidity']:.1f} %")
                    print(f"  Pressure:    {sensor_data['pressure']:.1f} hPa")
                    print(f"  Battery:     {sensor_data['battery_mv']} mV")
                else:
                    print(f"  Manufacturer data: {advertisement_data.manufacturer_data}")

        scanner = BleakScanner(detection_callback=detection_callback)

        try:
            await scanner.start()
            await asyncio.sleep(duration)
        finally:
            await scanner.stop()

        print("\n" + "=" * 60)
        print("Monitoring ended")


async def main():
    client = BLEHybridClient()

    # Parse command line arguments
    command = sys.argv[1] if len(sys.argv) > 1 else "scan"

    if command == "scan":
        # Scan and show devices with advertising data
        devices = await client.scan_for_devices(timeout=10.0)

        print("\n" + "=" * 60)
        print(f"Found {len(devices)} myIoTGrid device(s)")
        print("=" * 60)

        for device in devices:
            print(f"\n{device['name']} ({device['address']})")
            print(f"  RSSI: {device['rssi']} dBm")

            if device['sensor_data']:
                sd = device['sensor_data']
                print(f"  Temperature: {sd['temperature']:.2f} °C")
                print(f"  Humidity:    {sd['humidity']:.1f} %")
                print(f"  Pressure:    {sd['pressure']:.1f} hPa")
                print(f"  Battery:     {sd['battery_mv']} mV")
            else:
                print("  No sensor data in advertising")

    elif command == "connect":
        # Scan first, then connect to first device
        devices = await client.scan_for_devices(timeout=5.0)

        if not devices:
            print("No myIoTGrid devices found")
            return

        device = devices[0]
        print(f"\nConnecting to {device['name']}...")

        result = await client.connect_and_read(device['address'])

        if result['connected']:
            print("\n" + "=" * 60)
            print("Connection successful!")

            if result['device_info']:
                print("\nDevice Info:")
                for key, value in result['device_info'].items():
                    print(f"  {key}: {value}")

            if result['sensor_data']:
                print("\nSensor Data (via GATT):")
                for key, value in result['sensor_data'].items():
                    print(f"  {key}: {value}")

    elif command == "reboot":
        # Send reboot command
        devices = await client.scan_for_devices(timeout=5.0)

        if not devices:
            print("No myIoTGrid devices found")
            return

        device = devices[0]
        print(f"\nSending reboot command to {device['name']}...")

        success = await client.send_config(device['address'], CMD_REBOOT)
        print(f"Reboot {'succeeded' if success else 'failed'}")

    elif command == "monitor":
        # Continuous monitoring
        duration = float(sys.argv[2]) if len(sys.argv) > 2 else 60.0
        await client.monitor(duration)

    else:
        print(f"Unknown command: {command}")
        print("\nAvailable commands:")
        print("  scan     - Scan for devices and show advertising data")
        print("  connect  - Connect via GATT and read device info")
        print("  reboot   - Send reboot command to device")
        print("  monitor  - Continuous monitoring (default: 60s)")


if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("\nStopped by user.")
