#!/usr/bin/env python3
"""
BLE Bridge for macOS Development
Scans for myIoTGrid ESP32 devices and forwards sensor data to the Hub API.

Usage:
    pip install bleak aiohttp
    python ble_bridge.py

This script bridges BLE sensor data from ESP32 to the Hub API on macOS,
where the .NET InTheHand.BluetoothLE library doesn't work properly.
"""

import asyncio
import json
import ssl
import aiohttp
from bleak import BleakClient, BleakScanner
from datetime import datetime

# Configuration
HUB_API_URL = "https://localhost:5001"
VERIFY_SSL = False  # Set to True in production

# BLE UUIDs (must match ESP32 config.h)
SERVICE_UUID = "12345678-1234-5678-1234-56789abcdef0"
SENSOR_DATA_UUID = "12345678-1234-5678-1234-56789abcdef1"
DEVICE_INFO_UUID = "12345678-1234-5678-1234-56789abcdef2"

# Device name prefixes to scan for
DEVICE_PREFIXES = ["myIoTGrid-", "ESP32-"]


async def send_to_hub(session: aiohttp.ClientSession, data: dict):
    """Send sensor reading to Hub API"""
    try:
        # Parse sensor data from ESP32 format
        node_id = data.get("nodeId", "unknown")
        sensors = data.get("sensors", [])

        for sensor in sensors:
            reading = {
                "deviceId": node_id,
                "type": sensor.get("type", "unknown"),
                "value": sensor.get("value", 0),
                "unit": sensor.get("unit", "")
            }

            async with session.post(
                f"{HUB_API_URL}/api/readings",
                json=reading,
                ssl=False if not VERIFY_SSL else None
            ) as response:
                if response.status in (200, 201):
                    print(f"  -> Sent {sensor['type']}: {sensor['value']} {sensor.get('unit', '')}")
                else:
                    print(f"  -> Error: {response.status}")

    except Exception as e:
        print(f"  -> Error sending to Hub: {e}")


def notification_handler(session: aiohttp.ClientSession, loop: asyncio.AbstractEventLoop):
    """Create a notification handler for BLE sensor data"""
    def handler(sender, data: bytearray):
        try:
            json_str = data.decode('utf-8')
            sensor_data = json.loads(json_str)
            print(f"\n[BLE] Received sensor data from {sensor_data.get('nodeId', 'unknown')}")

            # Schedule the async send on the event loop
            asyncio.run_coroutine_threadsafe(
                send_to_hub(session, sensor_data),
                loop
            )
        except Exception as e:
            print(f"[BLE] Error parsing data: {e}")
            print(f"[BLE] Raw data: {data}")

    return handler


async def connect_and_subscribe(device, session: aiohttp.ClientSession):
    """Connect to a BLE device and subscribe to sensor data notifications"""
    print(f"\n[BLE] Connecting to {device.name} ({device.address})...")

    loop = asyncio.get_event_loop()

    async with BleakClient(device.address) as client:
        print(f"[BLE] Connected to {device.name}")

        # Check if our service exists
        services = client.services
        our_service = None
        for service in services:
            if service.uuid.lower() == SERVICE_UUID.lower():
                our_service = service
                break

        if not our_service:
            print(f"[BLE] Service {SERVICE_UUID} not found on {device.name}")
            print(f"[BLE] Available services: {[s.uuid for s in services]}")
            return

        print(f"[BLE] Found myIoTGrid service")

        # Read device info
        try:
            device_info_data = await client.read_gatt_char(DEVICE_INFO_UUID)
            device_info = json.loads(device_info_data.decode('utf-8'))
            print(f"[BLE] Device Info: {device_info}")
        except Exception as e:
            print(f"[BLE] Could not read device info: {e}")

        # Subscribe to sensor data notifications
        print(f"[BLE] Subscribing to sensor data notifications...")
        handler = notification_handler(session, loop)
        await client.start_notify(SENSOR_DATA_UUID, handler)

        print(f"[BLE] Listening for sensor data... (Press Ctrl+C to stop)")

        # Keep connection alive
        while client.is_connected:
            await asyncio.sleep(1)


async def scan_and_connect():
    """Scan for myIoTGrid devices and connect to them"""
    print("=" * 60)
    print("myIoTGrid BLE Bridge for macOS")
    print("=" * 60)
    print(f"Hub API: {HUB_API_URL}")
    print(f"Service UUID: {SERVICE_UUID}")
    print("=" * 60)

    # Create SSL context that ignores certificate verification (dev only)
    ssl_context = ssl.create_default_context()
    if not VERIFY_SSL:
        ssl_context.check_hostname = False
        ssl_context.verify_mode = ssl.CERT_NONE

    connector = aiohttp.TCPConnector(ssl=ssl_context)

    async with aiohttp.ClientSession(connector=connector) as session:
        while True:
            print("\n[BLE] Scanning for myIoTGrid devices...")

            devices = await BleakScanner.discover(timeout=5.0)

            myiotgrid_devices = []
            for device in devices:
                if device.name:
                    for prefix in DEVICE_PREFIXES:
                        if device.name.startswith(prefix):
                            myiotgrid_devices.append(device)
                            print(f"  Found: {device.name} ({device.address})")
                            break

            if not myiotgrid_devices:
                print("  No myIoTGrid devices found. Retrying in 10s...")
                await asyncio.sleep(10)
                continue

            # Connect to the first found device
            device = myiotgrid_devices[0]

            try:
                await connect_and_subscribe(device, session)
            except Exception as e:
                print(f"[BLE] Connection error: {e}")
                print("[BLE] Retrying in 5s...")
                await asyncio.sleep(5)


def main():
    print("\nStarting BLE Bridge...")
    print("Make sure the Hub API is running on https://localhost:5001")
    print("Press Ctrl+C to stop\n")

    try:
        asyncio.run(scan_and_connect())
    except KeyboardInterrupt:
        print("\n\nBLE Bridge stopped.")


if __name__ == "__main__":
    main()
