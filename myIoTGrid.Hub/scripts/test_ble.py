#!/usr/bin/env python3
import asyncio
from bleak import BleakClient
MAC = "00:70:07:84:92:CE"
async def main():
    for i in range(1, 31):
        print(f"Versuch {i}/30...")
        try:
            async with BleakClient(MAC, timeout=15.0) as client:
                if client.is_connected:
                    print(f"ERFOLG nach {i} Versuchen!")
                    for s in client.services:
                        print(f"  Service: {s.uuid}")
                    await asyncio.sleep(5)
                    return
        except Exception as e:
            print(f"  Fehler: {str(e)[:50]}")
        await asyncio.sleep(2)
    print("Fehlgeschlagen")
asyncio.run(main())
