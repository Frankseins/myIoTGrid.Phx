import { Component, ElementRef, Input, OnDestroy, AfterViewInit, ViewChild } from '@angular/core';

// Use global Leaflet loaded via index.html to avoid adding build-time deps
declare const L: any;

export interface MapPoint {
  lat: number;
  lon: number;
  ts: string;
  hdop?: number;
  speed?: number;
  temperature?: number;
  humidity?: number;
  altitude?: number;
  pressure?: number;
  illuminance?: number;
  waterTemperature?: number;
  gpsSatellites?: number;
  gpsFix?: number;
}

@Component({
  selector: 'myiotgrid-leaflet-map',
  standalone: true,
  template: `
    <div #mapEl class="leaflet-map" style="width: 100%; height: 500px; border-radius: 8px; overflow: hidden;"></div>
  `
})
export class LeafletMapComponent implements AfterViewInit, OnDestroy {
  @ViewChild('mapEl', { static: true }) mapEl!: ElementRef<HTMLDivElement>;

  private map: any;
  private marker: any;
  private polyline: any;
  private markersLayer: any;
  private didFit = false;
  private leafletOk = false;

  private _lat: number | null = null;
  private _lon: number | null = null;
  private _trail: [number, number][] = [];
  private _points: MapPoint[] = [];

  @Input()
  set lat(v: number | null) { this._lat = v; this.syncPosition(); }
  get lat() { return this._lat; }

  @Input()
  set lon(v: number | null) { this._lon = v; this.syncPosition(); }
  get lon() { return this._lon; }

  @Input()
  set trail(v: [number, number][]) { this._trail = v || []; this.syncTrail(); }
  get trail() { return this._trail; }

  @Input()
  set points(v: MapPoint[]) { this._points = v || []; this.syncMarkers(); }
  get points() { return this._points; }

  ngAfterViewInit(): void {
    // Guard against missing Leaflet (e.g., CDN blocked or offline)
    try {
      if (typeof (L as any) === 'undefined') {
        // Provide a friendly inline message so users see what’s wrong
        this.mapEl.nativeElement.style.display = 'flex';
        this.mapEl.nativeElement.style.alignItems = 'center';
        this.mapEl.nativeElement.style.justifyContent = 'center';
        this.mapEl.nativeElement.style.background = '#fafafa';
        this.mapEl.nativeElement.style.color = '#666';
        this.mapEl.nativeElement.innerText = 'Map library failed to load. Please check your network/CSP.';
        console.error('[LeafletMapComponent] Global Leaflet (L) not found. Ensure leaflet.js is loaded.');
        return;
      }
      this.leafletOk = true;
      this.map = L.map(this.mapEl.nativeElement, { center: [52.52, 13.405], zoom: 12 });
      // Revert to standard OSM template (subdomains a/b/c) – actual allowance handled via CSP in nginx
      L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; OpenStreetMap contributors'
      }).addTo(this.map);
      this.markersLayer = L.layerGroup().addTo(this.map);
      this.syncAll();
    } catch (err) {
      console.error('[LeafletMapComponent] init failed:', err);
      this.mapEl.nativeElement.style.display = 'flex';
      this.mapEl.nativeElement.style.alignItems = 'center';
      this.mapEl.nativeElement.style.justifyContent = 'center';
      this.mapEl.nativeElement.style.background = '#fafafa';
      this.mapEl.nativeElement.style.color = '#666';
      this.mapEl.nativeElement.innerText = 'Map initialisation failed. See console for details.';
    }
  }

  ngOnDestroy(): void {
    if (this.map) this.map.remove();
  }

  private syncAll(): void {
    this.didFit = false;
    this.syncPosition();
    this.syncTrail();
    this.syncMarkers();
  }

  private syncPosition(): void {
    if (!this.leafletOk || !this.map || this._lat == null || this._lon == null) return;
    const pos: [number, number] = [this._lat, this._lon];
    if (!this.marker) {
      this.marker = L.circleMarker(pos, { radius: 6, color: '#1565C0', weight: 2, fillColor: '#42A5F5', fillOpacity: 0.8 })
        .addTo(this.map);
    } else {
      this.marker.setLatLng(pos);
    }
    if (!this.didFit) this.map.setView(pos, 14);
  }

  private syncTrail(): void {
    if (!this.leafletOk || !this.map || !this._trail || this._trail.length === 0) return;
    if (!this.polyline) {
      this.polyline = L.polyline(this._trail, { color: '#1976D2', weight: 3 }).addTo(this.map);
    } else {
      this.polyline.setLatLngs(this._trail);
    }
    if (!this.didFit) {
      this.map.fitBounds(this.polyline.getBounds(), { padding: [20, 20] });
      this.didFit = true;
    }
  }

  private syncMarkers(): void {
    if (!this.leafletOk || !this.map || !this.markersLayer) return;
    this.markersLayer.clearLayers();
    for (const p of this._points) {
      const m = L.circleMarker([p.lat, p.lon], { radius: 3, color: '#1E88E5', weight: 1, fillColor: '#90CAF9', fillOpacity: 0.9 });
      m.bindPopup(this.buildPopupHtml(p));
      m.addTo(this.markersLayer);
    }
  }

  private buildPopupHtml(p: MapPoint): string {
    const base: string[] = [
      `Lat: ${p.lat?.toFixed ? (p.lat as number).toFixed(6) : p.lat}`,
      `Lon: ${p.lon?.toFixed ? (p.lon as number).toFixed(6) : p.lon}`,
      `Time: ${new Date(p.ts).toLocaleString()}`,
    ];
    const labels: Record<string, string> = {
      temperature: 'Temperatur',
      humidity: 'Luftfeuchtigkeit',
      waterTemperature: 'Wassertemperatur',
      pressure: 'Luftdruck',
      illuminance: 'Helligkeit',
      altitude: 'Höhe',
      speed: 'Geschwindigkeit',
      hdop: 'HDOP',
      gpsSatellites: 'Satelliten',
      gpsFix: 'Fix',
    };
    const units: Record<string, string> = {
      temperature: '°C',
      humidity: '%',
      waterTemperature: '°C',
      pressure: ' hPa',
      illuminance: ' lux',
      altitude: ' m',
      speed: ' km/h',
      hdop: '',
      gpsSatellites: '',
      gpsFix: '',
    };
    const decimals: Record<string, number> = {
      temperature: 1,
      humidity: 1,
      waterTemperature: 1,
      pressure: 1,
      illuminance: 0,
      altitude: 1,
      speed: 2,
      hdop: 2,
      gpsSatellites: 0,
      gpsFix: 0,
    };
    const skip = new Set(['lat', 'lon', 'ts']);
    for (const [k, v] of Object.entries(p)) {
      if (skip.has(k)) continue;
      if (v == null || typeof v !== 'number') continue;
      const label = (labels as any)[k] ?? k;
      const unit = (units as any)[k] ?? '';
      const dec = (decimals as any)[k];
      const valueStr = Number.isFinite(v) ? (dec != null ? (v as number).toFixed(dec) : String(v)) : String(v);
      base.push(`${label}: ${valueStr}${unit}`);
    }
    return base.join('<br>');
  }
}
