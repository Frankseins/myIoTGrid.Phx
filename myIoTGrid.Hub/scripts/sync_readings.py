#!/usr/bin/env python3
"""
myIoTGrid Reading Sync Script
Zieht Readings aus der Hub API und spielt sie in eine andere API ein.

Usage:
    python3 sync_readings.py
    python3 sync_readings.py --source https://hub1.local:5001 --target https://hub2.local:5001
    python3 sync_readings.py --from 2024-01-01 --to 2024-12-31
"""

import argparse
import json
import requests
import urllib3
from datetime import datetime, timedelta
from typing import Optional

# SSL-Warnungen unterdrücken für self-signed certs
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)


class ReadingSyncClient:
    def __init__(
        self,
        source_url: str,
        target_url: str,
        source_tenant: str = "00000000-0000-0000-0000-000000000001",
        target_tenant: str = "00000000-0000-0000-0000-000000000001",
        verify_ssl: bool = False
    ):
        self.source_url = source_url.rstrip('/')
        self.target_url = target_url.rstrip('/')
        self.source_tenant = source_tenant
        self.target_tenant = target_tenant
        self.verify_ssl = verify_ssl

        self.source_headers = {
            "Content-Type": "application/json",
            "X-Tenant-Id": source_tenant
        }
        self.target_headers = {
            "Content-Type": "application/json",
            "X-Tenant-Id": target_tenant
        }

    def fetch_readings(
        self,
        node_id: Optional[str] = None,
        from_date: Optional[datetime] = None,
        to_date: Optional[datetime] = None,
        page: int = 1,
        page_size: int = 100
    ) -> dict:
        """Readings aus der Source API abrufen"""
        params = {
            "page": page,
            "pageSize": page_size
        }

        if node_id:
            params["nodeId"] = node_id
        if from_date:
            params["from"] = from_date.isoformat()
        if to_date:
            params["to"] = to_date.isoformat()

        url = f"{self.source_url}/api/readings"
        print(f"[GET] {url}")
        print(f"      Params: {params}")

        response = requests.get(
            url,
            params=params,
            headers=self.source_headers,
            verify=self.verify_ssl,
            timeout=30
        )
        response.raise_for_status()
        return response.json()

    def fetch_all_readings(
        self,
        node_id: Optional[str] = None,
        from_date: Optional[datetime] = None,
        to_date: Optional[datetime] = None,
        page_size: int = 100
    ) -> list:
        """Alle Readings paginiert abrufen"""
        all_readings = []
        page = 1

        while True:
            result = self.fetch_readings(
                node_id=node_id,
                from_date=from_date,
                to_date=to_date,
                page=page,
                page_size=page_size
            )

            items = result.get("items", result.get("data", []))
            if not items:
                break

            all_readings.extend(items)
            print(f"      Seite {page}: {len(items)} Readings geladen (Gesamt: {len(all_readings)})")

            # Prüfen ob weitere Seiten vorhanden
            total = result.get("totalCount", result.get("total", 0))
            if len(all_readings) >= total:
                break

            page += 1

        return all_readings

    def transform_reading(self, reading: dict) -> dict:
        """Reading in das Format für die Target API transformieren"""
        # Standard CreateSensorReadingDto Format
        return {
            "deviceId": reading.get("nodeId") or reading.get("nodeName", "unknown"),
            "type": reading.get("measurementType", "unknown"),
            "value": reading.get("value", reading.get("rawValue", 0)),
            "unit": reading.get("unit"),
            "timestamp": self._to_unix_timestamp(reading.get("timestamp")),
            "endpointId": reading.get("endpointId")
        }

    def _to_unix_timestamp(self, timestamp_str: Optional[str]) -> Optional[int]:
        """ISO timestamp zu Unix timestamp konvertieren"""
        if not timestamp_str:
            return None
        try:
            # ISO format parsen
            if timestamp_str.endswith('Z'):
                timestamp_str = timestamp_str[:-1] + '+00:00'
            dt = datetime.fromisoformat(timestamp_str.replace('Z', '+00:00'))
            return int(dt.timestamp())
        except:
            return None

    def push_reading(self, reading: dict) -> dict:
        """Einzelnes Reading zur Target API senden"""
        url = f"{self.target_url}/api/readings"

        response = requests.post(
            url,
            json=reading,
            headers=self.target_headers,
            verify=self.verify_ssl,
            timeout=30
        )
        response.raise_for_status()
        return response.json()

    def push_readings_batch(self, readings: list, node_id: str) -> dict:
        """Mehrere Readings als Batch zur Target API senden"""
        url = f"{self.target_url}/api/readings/batch"

        # In Batch-Format konvertieren
        batch_readings = []
        for r in readings:
            batch_readings.append({
                "endpointId": r.get("endpointId", 0),
                "measurementType": r.get("type", "unknown"),
                "rawValue": r.get("value", 0)
            })

        payload = {
            "nodeId": node_id,
            "hubId": None,
            "readings": batch_readings
        }

        response = requests.post(
            url,
            json=payload,
            headers=self.target_headers,
            verify=self.verify_ssl,
            timeout=60
        )
        response.raise_for_status()
        return response.json()

    def sync(
        self,
        node_id: Optional[str] = None,
        from_date: Optional[datetime] = None,
        to_date: Optional[datetime] = None,
        batch_size: int = 50,
        dry_run: bool = False
    ) -> dict:
        """Komplette Synchronisation durchführen"""
        print("\n" + "=" * 60)
        print("myIoTGrid Reading Sync")
        print("=" * 60)
        print(f"Source: {self.source_url}")
        print(f"Target: {self.target_url}")
        print(f"Node:   {node_id or 'alle'}")
        print(f"Von:    {from_date or 'Anfang'}")
        print(f"Bis:    {to_date or 'jetzt'}")
        print(f"Batch:  {batch_size}")
        print(f"Dry-Run: {dry_run}")
        print("=" * 60 + "\n")

        # Readings abrufen
        print("[1/3] Readings aus Source API laden...")
        readings = self.fetch_all_readings(
            node_id=node_id,
            from_date=from_date,
            to_date=to_date
        )
        print(f"      -> {len(readings)} Readings geladen\n")

        if not readings:
            print("Keine Readings gefunden. Sync beendet.")
            return {"synced": 0, "failed": 0, "total": 0}

        # Transformieren
        print("[2/3] Readings transformieren...")
        transformed = [self.transform_reading(r) for r in readings]
        print(f"      -> {len(transformed)} Readings transformiert\n")

        if dry_run:
            print("[DRY-RUN] Würde folgende Readings senden:")
            for i, r in enumerate(transformed[:5]):
                print(f"  {i+1}. {r['type']}: {r['value']} {r.get('unit', '')}")
            if len(transformed) > 5:
                print(f"  ... und {len(transformed) - 5} weitere")
            return {"synced": 0, "failed": 0, "total": len(transformed), "dry_run": True}

        # Senden
        print("[3/3] Readings zur Target API senden...")
        synced = 0
        failed = 0

        for i, reading in enumerate(transformed):
            try:
                self.push_reading(reading)
                synced += 1
                if (i + 1) % 10 == 0:
                    print(f"      -> {i + 1}/{len(transformed)} gesendet...")
            except requests.RequestException as e:
                failed += 1
                print(f"      [FEHLER] Reading {i + 1}: {e}")

        print(f"\n      -> Erfolgreich: {synced}")
        print(f"      -> Fehlgeschlagen: {failed}")

        result = {
            "synced": synced,
            "failed": failed,
            "total": len(transformed)
        }

        print("\n" + "=" * 60)
        print("Sync abgeschlossen!")
        print("=" * 60)

        return result


def main():
    parser = argparse.ArgumentParser(
        description="Synchronisiert Readings zwischen zwei myIoTGrid APIs"
    )

    parser.add_argument(
        "--source",
        default="https://localhost:5001",
        help="Source API URL (default: https://localhost:5001)"
    )
    parser.add_argument(
        "--target",
        default="https://localhost:5002",
        help="Target API URL (default: https://localhost:5002)"
    )
    parser.add_argument(
        "--source-tenant",
        default="00000000-0000-0000-0000-000000000001",
        help="Source Tenant ID"
    )
    parser.add_argument(
        "--target-tenant",
        default="00000000-0000-0000-0000-000000000001",
        help="Target Tenant ID"
    )
    parser.add_argument(
        "--node",
        help="Node ID filtern (optional)"
    )
    parser.add_argument(
        "--from",
        dest="from_date",
        help="Von Datum (YYYY-MM-DD)"
    )
    parser.add_argument(
        "--to",
        dest="to_date",
        help="Bis Datum (YYYY-MM-DD)"
    )
    parser.add_argument(
        "--batch-size",
        type=int,
        default=50,
        help="Batch-Größe für Upload (default: 50)"
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Nur simulieren, nichts senden"
    )
    parser.add_argument(
        "--verify-ssl",
        action="store_true",
        help="SSL-Zertifikate verifizieren"
    )

    args = parser.parse_args()

    # Datumsangaben parsen
    from_date = None
    to_date = None

    if args.from_date:
        from_date = datetime.fromisoformat(args.from_date)
    if args.to_date:
        to_date = datetime.fromisoformat(args.to_date)

    # Client erstellen und synchronisieren
    client = ReadingSyncClient(
        source_url=args.source,
        target_url=args.target,
        source_tenant=args.source_tenant,
        target_tenant=args.target_tenant,
        verify_ssl=args.verify_ssl
    )

    result = client.sync(
        node_id=args.node,
        from_date=from_date,
        to_date=to_date,
        batch_size=args.batch_size,
        dry_run=args.dry_run
    )

    # Exit code basierend auf Ergebnis
    if result.get("failed", 0) > 0:
        exit(1)
    exit(0)


if __name__ == "__main__":
    main()
