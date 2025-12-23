#!/usr/bin/env python3
"""
BLE Connection Test Script for myIoTGrid
Tests connection to ESP32 sensor using bleak library with retry logic.

Usage:
    pip3 install bleak
    python3 test_ble_connection.py 00:70:07:84:92:CE

Sprint BT-01: Bluetooth Infrastructure
"""

import asyncio
import sys
from bleak import BleakClient, BleakScanner

# myIoTGrid BLE Sensor Mode UUIDs (must match ESP32 config.h)
SERVICE_UUID = "12345678-1234-5678-1234-56789abcdef0"
CHAR_SENSOR_DATA_UUID = "12345678-1234-5678-1234-56789abcdef1"
CHAR_DEVICE_INFO_UUID = "12345678-1234-5678-1234-56789abcdef2"

MAX_RETRIES = 30
RETRY_DELAY = 2.0  # seconds
CONNECTION_TIMEOUT = 15.0  # seconds


async def connect_with_retry(address: str):
    """Try to connect with multiple retries."""
    print(f"Attempting to connect to {address}")
    print(f"Max retries: {MAX_RETRIES}, timeout: {CONNECTION_TIMEOUT}s")
    print("-" * 50)

    for attempt in range(1, MAX_RETRIES + 1):
        try:
            print(f"Attempt {attempt}/{MAX_RETRIES}...")

            async with BleakClient(address, timeout=CONNECTION_TIMEOUT) as client:
                if client.is_connected:
                    print(f"\n{'='*50}")
                    print(f"SUCCESS! Connected on attempt {attempt}")
                    print(f"{'='*50}")

                    # Try to read device info
                    try:
                        device_info = await client.read_gatt_char(CHAR_DEVICE_INFO_UUID)
                        print(f"Device Info: {device_info.decode('utf-8')}")
                    except Exception as e:
                        print(f"Could not read device info: {e}")

                    # List services
                    print("\nServices:")
                    for service in client.services:
                        print(f"  - {service.uuid}: {service.description}")
                        for char in service.characteristics:
                            print(f"      - {char.uuid}: {char.properties}")

                    # Subscribe to sensor data notifications
                    print("\nSubscribing to sensor data notifications...")

                    def notification_handler(sender, data):
                        print(f"Received: {data.decode('utf-8')}")

                    try:
                        await client.start_notify(CHAR_SENSOR_DATA_UUID, notification_handler)
                        print("Subscribed! Waiting for sensor data (60 seconds)...")
                        await asyncio.sleep(60)
                        await client.stop_notify(CHAR_SENSOR_DATA_UUID)
                    except Exception as e:
                        print(f"Notification error: {e}")

                    return True

        except asyncio.TimeoutError:
            print(f"  Timeout after {CONNECTION_TIMEOUT}s")
        except Exception as e:
            error_msg = str(e)
            if "le-connection-abort-by-local" in error_msg:
                print(f"  Connection aborted (known issue, retrying...)")
            else:
                print(f"  Error: {error_msg}")

        if attempt < MAX_RETRIES:
            print(f"  Waiting {RETRY_DELAY}s before retry...")
            await asyncio.sleep(RETRY_DELAY)

    print(f"\nFailed after {MAX_RETRIES} attempts")
    return False


async def scan_for_devices():
    """Scan for myIoTGrid devices."""
    print("Scanning for BLE devices (10 seconds)...")
    devices = await BleakScanner.discover(timeout=10.0)

    myiotgrid_devices = []
    for device in devices:
        name = device.name or ""
        if name.startswith("myIoTGrid-") or name.startswith("ESP32-"):
            myiotgrid_devices.append(device)
            print(f"  Found: {device.name} ({device.address})")

    if not myiotgrid_devices:
        print("  No myIoTGrid devices found")

    return myiotgrid_devices


async def main():
    if len(sys.argv) < 2:
        print("Usage: python3 test_ble_connection.py <MAC_ADDRESS>")
        print("       python3 test_ble_connection.py scan")
        print("\nExample: python3 test_ble_connection.py 00:70:07:84:92:CE")
        return

    arg = sys.argv[1]

    if arg.lower() == "scan":
        await scan_for_devices()
    else:
        await connect_with_retry(arg)


if __name__ == "__main__":
    asyncio.run(main())
