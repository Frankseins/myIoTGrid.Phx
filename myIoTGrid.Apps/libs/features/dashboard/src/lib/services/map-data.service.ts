import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ReadingApiService } from '@myiotgrid/shared/data-access';
import { NodesApiService, NodeListItem } from '@myiotgrid/shared/data-access';
import { Reading } from '@myiotgrid/shared/models';

export interface PositionPoint {
  lat: number;
  lon: number;
  ts: string; // ISO
  hdop?: number;
  speed?: number; // km/h
  temperature?: number;
  humidity?: number;
  altitude?: number;
  pressure?: number;
  illuminance?: number;
  waterTemperature?: number;
  gpsSatellites?: number;
  gpsFix?: number;
}

@Injectable({ providedIn: 'root' })
export class MapDataService {
  private readonly readingApi = inject(ReadingApiService);
  private readonly nodesApi = inject(NodesApiService);

  async listNodes(): Promise<NodeListItem[]> {
    return await firstValueFrom(this.nodesApi.list());
  }

  /**
   * Load paged readings for a measurement type, newest first, then return ascending by timestamp.
   */
  private async getReadingsByTypePagedAll(
    nodeId: string,
    measurementType: string,
    desiredCount = 1200,
    pageSize = 300,
    maxPages = 20
  ): Promise<Reading[]> {
    const all: Reading[] = [];
    for (let page = 0; page < maxPages; page++) {
      const resp = await firstValueFrom(this.readingApi.getPaged({
        page,
        pageSize,
        sort: 'Timestamp,desc',
        'filters[nodeId]': nodeId as unknown as string,
        'filters[measurementType]': measurementType as unknown as string
      } as any));
      all.push(...resp.items);
      const noMore = resp.items.length < pageSize || (resp.totalPages != null && page + 1 >= resp.totalPages);
      if (all.length >= desiredCount || noMore) break;
    }
    const trimmed = all.slice(0, desiredCount);
    return trimmed.slice().reverse();
  }

  /** Build position points from readings of various measurement types (approx. to Vue logic). */
  async buildPositions(
    nodeId: string,
    from?: string,
    to?: string
  ): Promise<PositionPoint[]> {
    // Core types
    const [lats, lons] = await Promise.all([
      this.getReadingsByTypePagedAll(nodeId, 'latitude'),
      this.getReadingsByTypePagedAll(nodeId, 'longitude')
    ]);

    // Attach optional metrics (best-effort)
    const optTypes = [
      'hdop', 'speed', 'temperature', 'humidity', 'altitude', 'pressure',
      'illuminance', 'waterTemperature', 'gpsSatellites', 'gpsFix'
    ];
    const optMap = new Map<string, Reading[]>();
    await Promise.all(optTypes.map(async t => {
      try { optMap.set(t, await this.getReadingsByTypePagedAll(nodeId, t)); } catch { /* ignore */ }
    }));

    // Merge latitude and longitude by nearest timestamp within 2s
    const positions: PositionPoint[] = [];
    let j = 0;
    for (let i = 0; i < lats.length; i++) {
      const latR = lats[i];
      const latTs = new Date(latR.timestamp).getTime();
      // advance lon pointer j to closest time
      while (j + 1 < lons.length) {
        const d1 = Math.abs(new Date(lons[j].timestamp).getTime() - latTs);
        const d2 = Math.abs(new Date(lons[j + 1].timestamp).getTime() - latTs);
        if (d2 <= d1) j++; else break;
      }
      const lonR = lons[j];
      const dt = Math.abs(new Date(lonR.timestamp).getTime() - latTs);
      if (dt <= 2000) {
        const tsISO = new Date(Math.max(latTs, new Date(lonR.timestamp).getTime())).toISOString();
        const pos: PositionPoint = { lat: latR.value, lon: lonR.value, ts: tsISO } as PositionPoint;
        // enrich from optional metrics by nearest timestamp
        for (const t of optTypes) {
          const arr = optMap.get(t);
          if (!arr || arr.length === 0) continue;
          const nearest = this.findNearestByTs(arr, tsISO);
          if (nearest) (pos as any)[t] = nearest.value;
        }
        positions.push(pos);
      }
    }
    return positions;
  }

  private findNearestByTs(arr: Reading[], tsISO: string): Reading | null {
    const target = new Date(tsISO).getTime();
    // binary-like search by linear scan around matching index (arrays are ascending)
    let best: Reading | null = null;
    let bestDt = Number.MAX_SAFE_INTEGER;
    for (const r of arr) {
      const dt = Math.abs(new Date(r.timestamp).getTime() - target);
      if (dt < bestDt) { bestDt = dt; best = r; }
      // small optimization: break if dt starts increasing significantly
      if (bestDt <= 500 && dt > bestDt) break;
    }
    return best;
  }
}
