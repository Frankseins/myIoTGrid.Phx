import { Component, OnDestroy, OnInit, computed, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { LeafletMapComponent, MapPoint } from '../map/leaflet-map.component';
import { MapDataService, PositionPoint } from '../../services/map-data.service';

@Component({
  selector: 'myiotgrid-map-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatToolbarModule,
    MatFormFieldModule,
    MatSelectModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    LeafletMapComponent
  ],
  template: `
  <div class="map-dashboard-container">
    <header class="page-header">
      <div class="header-content">
        <h2 class="page-title">Map</h2>
        <p class="page-subtitle">Positionsverlauf und aktuelle Position</p>
      </div>
    </header>

    <mat-toolbar class="toolbar outlook-toolbar" style="margin-bottom: 12px;">
      <div class="left-actions" style="display:flex; gap:12px; align-items:center; flex-wrap: wrap;">
        <mat-form-field appearance="outline" style="min-width: 220px;">
          <mat-select placeholder="Node" [(ngModel)]="selectedNodeId" (ngModelChange)="onNodeChanged()">
            <mat-option *ngFor="let n of nodes()" [value]="n.id">{{ n.name || n.id }}</mat-option>
          </mat-select>
        </mat-form-field>

        <mat-form-field appearance="outline" style="width: 220px;">
          <mat-label>Von</mat-label>
          <input matInput type="datetime-local" [(ngModel)]="fromLocal" step="1" />
        </mat-form-field>

        <mat-form-field appearance="outline" style="width: 220px;">
          <mat-label>Bis</mat-label>
          <input matInput type="datetime-local" [(ngModel)]="toLocal" step="1" />
        </mat-form-field>

        <mat-form-field appearance="outline" style="width: 140px;">
          <mat-label>Intervall (s)</mat-label>
          <input matInput type="number" min="5" step="5" [(ngModel)]="intervalSec" />
        </mat-form-field>

        <button mat-stroked-button color="primary" (click)="refresh()">
          <mat-icon>refresh</mat-icon>
          Aktualisieren
        </button>
        <button mat-stroked-button color="primary" (click)="togglePolling()">
          <mat-icon>{{ isPolling() ? 'pause' : 'play_arrow' }}</mat-icon>
          {{ isPolling() ? 'Stop' : 'Start' }}
        </button>
      </div>
      <span class="spacer"></span>
      <span class="results-count">{{ points().length }} Punkte</span>
    </mat-toolbar>

    <div class="map-container">
      <myiotgrid-leaflet-map
        [lat]="lat()"
        [lon]="lon()"
        [trail]="trail()"
        [points]="$any(points())"
      ></myiotgrid-leaflet-map>
    </div>
  </div>
  `,
  styles: [`
    .map-dashboard-container {
      display: flex;
      flex-direction: column;
      height: 100%;
      width: 100%;
    }

    .map-container {
      flex: 1;
      min-height: 0;
      position: relative;
    }

    .spacer {
      flex: 1;
    }
  `]
})
export class MapDashboardComponent implements OnInit, OnDestroy {
  private readonly data = inject(MapDataService);

  readonly nodes = signal<{ id: string; name?: string }[]>([]);
  selectedNodeId: string | null = null;

  fromLocal = '';
  toLocal = '';
  intervalSec = 30;
  private timer: any;

  readonly points = signal<PositionPoint[]>([]);
  readonly lat = computed(() => this.points().length ? this.points()[this.points().length - 1].lat : null);
  readonly lon = computed(() => this.points().length ? this.points()[this.points().length - 1].lon : null);
  readonly trail = computed<[number, number][]>(() => this.points().map(p => [p.lat, p.lon] as [number, number]));
  readonly isPolling = signal<boolean>(false);

  async ngOnInit(): Promise<void> {
    console.debug('[MapDashboard] ngOnInit');
    await this.loadNodes();
    if (!this.selectedNodeId && this.nodes().length > 0) this.selectedNodeId = this.nodes()[0].id;
    await this.refresh();
  }

  ngOnDestroy(): void {
    this.stopPolling();
  }

  async loadNodes(): Promise<void> {
    try {
      const list = await this.data.listNodes();
      this.nodes.set(list);
    } catch (e) {
      console.error('Load nodes failed', e);
    }
  }

  async onNodeChanged(): Promise<void> {
    console.debug('[MapDashboard] Node changed to:', this.selectedNodeId);
    await this.loadDateRangeForNode();
    await this.refresh();
  }

  private async loadDateRangeForNode(): Promise<void> {
    if (!this.selectedNodeId) return;
    try {
      // Lade alle Positionen ohne Zeitfilter um Min/Max zu ermitteln
      const pts = await this.data.buildPositions(this.selectedNodeId, undefined, undefined);
      if (pts.length > 0) {
        // Erstes und letztes Datum extrahieren
        const firstDate = new Date(pts[0].ts);
        const lastDate = new Date(pts[pts.length - 1].ts);
        
        // In lokale datetime-local Format konvertieren (YYYY-MM-DDTHH:mm:ss)
        this.fromLocal = this.toDatetimeLocal(firstDate);
        this.toLocal = this.toDatetimeLocal(lastDate);
        
        console.debug('[MapDashboard] Date range set:', this.fromLocal, 'to', this.toLocal);
      }
    } catch (e) {
      console.error('[MapDashboard] Load date range failed', e);
    }
  }

  private toDatetimeLocal(date: Date): string {
    const pad = (n: number) => String(n).padStart(2, '0');
    const year = date.getFullYear();
    const month = pad(date.getMonth() + 1);
    const day = pad(date.getDate());
    const hours = pad(date.getHours());
    const minutes = pad(date.getMinutes());
    const seconds = pad(date.getSeconds());
    return `${year}-${month}-${day}T${hours}:${minutes}:${seconds}`;
  }

  async refresh(): Promise<void> {
    if (!this.selectedNodeId) return;
    try {
      const pts = await this.data.buildPositions(this.selectedNodeId, this.fromLocal || undefined, this.toLocal || undefined);
      this.points.set(pts);
      console.debug('[MapDashboard] refresh: points=', pts.length);
    } catch (e) {
      console.error('[MapDashboard] Load positions failed', e);
    }
  }

  togglePolling(): void {
    if (this.isPolling()) {
      this.stopPolling();
    } else {
      this.startPolling();
    }
  }

  private startPolling(): void {
    this.isPolling.set(true);
    this.stopPolling();
    const ms = Math.max(5, Number(this.intervalSec || 30)) * 1000;
    this.timer = setInterval(() => {
      this.refresh();
    }, ms);
  }

  private stopPolling(): void {
    if (this.timer) clearInterval(this.timer);
    this.timer = undefined;
    this.isPolling.set(false);
  }
}
