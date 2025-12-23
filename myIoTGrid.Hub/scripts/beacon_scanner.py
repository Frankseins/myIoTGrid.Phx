#!/usr/bin/env python3
"""
myIoTGrid BLE Beacon Scanner
Reads sensor data from ESP32 advertising packets - NO CONNECTION REQUIRED!

Usage:
    source ~/ble-test/bin/activate
    pip install bleak
    python beacon_scanner.py

Sprint BT-01: Bluetooth Infrastructure (Beacon Mode)
"""

import asyncio
import struct
from bleak import BleakScanner
from datetime import datetime

# myIoTGrid beacon constants (must match ESP32 config)
MYIOTGRID_COMPANY_ID = 0xFFFF
MYIOTGRID_DEVICE_TYPE = 0x01

def parse_sensor_data(manufacturer_data: bytes) -> dict:
    """Parse sensor data from manufacturer data bytes."""
    if len(manufacturer_data) < 15:
        return None

    try:
        # Unpack the BeaconSensorData structure
        # Format: <HBB4shhHHB (little-endian)
        # companyId(2) + deviceType(1) + version(1) + nodeIdHash(4) +
        # temperature(2) + humidity(2) + pressure(2) + battery(2) + flags(1)
        company_id, device_type, version = struct.unpack_from('<HBB', manufacturer_data, 0)

        if company_id != MYIOTGRID_COMPANY_ID or device_type != MYIOTGRID_DEVICE_TYPE:
            return None

        node_hash = manufacturer_data[4:8].hex().upper()
        temp_raw, humidity_raw, pressure_raw, battery, flags = struct.unpack_from('<hhHHB', manufacturer_data, 8)

        return {
            'version': version,
            'node_hash': node_hash,
            'temperature': temp_raw / 100.0,
            'humidity': humidity_raw / 100.0,
            'pressure': (pressure_raw + 50000) / 100.0,  # Convert back to hPa
            'battery_mv': battery,
            'battery_pct': min(100, max(0, (battery - 3000) / 12)),  # Rough estimate
            'has_gps': bool(flags & 0x01),
            'low_battery': bool(flags & 0x02),
            'error': bool(flags & 0x04),
        }
    except Exception as e:
        print(f"Parse error: {e}")
        return None


def detection_callback(device, advertisement_data):
    """Called for each detected BLE device."""
    name = device.name or ""

    # Filter for myIoTGrid devices
    if not (name.startswith("myIoTGrid-") or name.startswith("ESP32-")):
        return

    # Check for manufacturer data
    if not advertisement_data.manufacturer_data:
        return

    # Parse manufacturer data (key is company ID)
    for company_id, data in advertisement_data.manufacturer_data.items():
        # Reconstruct full data with company ID prefix
        full_data = struct.pack('<H', company_id) + bytes(data)
        sensor_data = parse_sensor_data(full_data)

        if sensor_data:
            timestamp = datetime.now().strftime("%H:%M:%S")
            print(f"\n[{timestamp}] {name} ({device.address})")
            print(f"  Temperature: {sensor_data['temperature']:.2f} °C")
            print(f"  Humidity:    {sensor_data['humidity']:.1f} %")
            print(f"  Pressure:    {sensor_data['pressure']:.1f} hPa")
            print(f"  Battery:     {sensor_data['battery_mv']} mV ({sensor_data['battery_pct']:.0f}%)")
            print(f"  Node Hash:   {sensor_data['node_hash']}")
            if sensor_data['low_battery']:
                print(f"  ⚠️  LOW BATTERY!")
            if sensor_data['error']:
                print(f"  ❌ SENSOR ERROR!")
            print(f"  RSSI:        {advertisement_data.rssi} dBm")


async def main():
    print("=" * 50)
    print("myIoTGrid BLE Beacon Scanner")
    print("NO CONNECTION REQUIRED - just reading advertisements!")
    print("=" * 50)
    print("\nScanning for myIoTGrid beacons...")
    print("Press Ctrl+C to stop\n")

    scanner = BleakScanner(detection_callback=detection_callback)

    try:
        while True:
            await scanner.start()
            await asyncio.sleep(5.0)
            await scanner.stop()
    except asyncio.CancelledError:
        pass
    finally:
        await scanner.stop()
        print("\nScanner stopped.")


if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("\nStopped by user.")
