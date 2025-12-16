import { Component, OnInit, OnDestroy, inject, signal, computed, effect, ViewChild, ViewContainerRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { Overlay, OverlayRef } from '@angular/cdk/overlay';
import { ComponentPortal } from '@angular/cdk/portal';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';
import { Subject, takeUntil, switchMap, of, forkJoin } from 'rxjs';
import { ExpeditionApiService, SignalRService } from '@myiotgrid/shared/data-access';
import {
  Expedition,
  ExpeditionStatus,
  ExpeditionGpsData,
  ExpeditionGpsPoint,
  ChartPoint
} from '@myiotgrid/shared/models';
import { LineChartComponent } from '@myiotgrid/shared/ui';
import { LeafletMapComponent, MapPoint } from '../../../../../dashboard/src/lib/components/map/leaflet-map.component';
import { GpsDatapointsListComponent, GpsDataPoint } from '../gps-datapoints-list/gps-datapoints-list.component';
import { ExpeditionFormComponent } from '../expedition-form/expedition-form.component';

/** Configuration for a sensor type */
export interface SensorConfig {
  key: string;
  name: string;
  icon: string;
  color: string;
  unit: string;
  decimals: number;
  convertFn?: (value: number) => number;
}

/** All possible sensor configurations */
export const SENSOR_CONFIGS: SensorConfig[] = [
  { key: 'temperature', name: 'Temperatur', icon: 'thermostat', color: '#f44336', unit: '°C', decimals: 1 },
  { key: 'humidity', name: 'Luftfeuchtigkeit', icon: 'water_drop', color: '#2196f3', unit: '%', decimals: 0 },
  { key: 'pressure', name: 'Luftdruck', icon: 'compress', color: '#9c27b0', unit: 'hPa', decimals: 0 },
  { key: 'waterTemperature', name: 'Wassertemperatur', icon: 'waves', color: '#00bcd4', unit: '°C', decimals: 1 },
  { key: 'illuminance', name: 'Helligkeit', icon: 'light_mode', color: '#ff9800', unit: 'lux', decimals: 0 },
  { key: 'altitude', name: 'Höhe', icon: 'terrain', color: '#795548', unit: 'm', decimals: 0 },
  { key: 'speed', name: 'Geschwindigkeit', icon: 'speed', color: '#4caf50', unit: 'km/h', decimals: 1, convertFn: (v) => v * 3.6 },
  { key: 'gpsSatellites', name: 'GPS Satelliten', icon: 'satellite_alt', color: '#607d8b', unit: '', decimals: 0 },
  { key: 'hdop', name: 'GPS Genauigkeit', icon: 'gps_fixed', color: '#009688', unit: '', decimals: 2 },
];

@Component({
  selector: 'myiotgrid-expedition-detail',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    MatDividerModule,
    LeafletMapComponent,
    GpsDatapointsListComponent,
    LineChartComponent
  ],
  templateUrl: './expedition-detail.component.html',
  styleUrl: './expedition-detail.component.scss'
})
export class ExpeditionDetailComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly expeditionApi = inject(ExpeditionApiService);
  private readonly signalRService = inject(SignalRService);
  private readonly overlay = inject(Overlay);
  private readonly viewContainerRef = inject(ViewContainerRef);
  private readonly destroy$ = new Subject<void>();

  @ViewChild(LeafletMapComponent) mapComponent?: LeafletMapComponent;

  // Expose enum to template
  readonly ExpeditionStatus = ExpeditionStatus;

  // Form drawer
  private formOverlayRef: OverlayRef | null = null;

  // Core state
  readonly expedition = signal<Expedition | null>(null);
  readonly gpsData = signal<ExpeditionGpsData | null>(null);
  readonly isLoading = signal(true);
  readonly error = signal<string | null>(null);

  // Live update state
  readonly isLive = computed(() => {
    const exp = this.expedition();
    if (!exp) return false;

    const now = new Date().getTime();
    const start = new Date(exp.startTime).getTime();
    const end = exp.endTime ? new Date(exp.endTime).getTime() : null;

    // Active if: now >= start AND (no end time OR now <= end)
    return now >= start && (end === null || now <= end);
  });

  /** Computed status based on current time */
  readonly computedStatus = computed(() => {
    const exp = this.expedition();
    if (!exp) return ExpeditionStatus.Planned;

    const now = new Date().getTime();
    const start = new Date(exp.startTime).getTime();
    const end = exp.endTime ? new Date(exp.endTime).getTime() : null;

    // If current time is before start -> Planned
    if (now < start) {
      return ExpeditionStatus.Planned;
    }

    // If current time is between start and end (or no end) -> Active
    if (end === null || now <= end) {
      return ExpeditionStatus.Active;
    }

    // If current time is after end -> Completed
    return ExpeditionStatus.Completed;
  });

  readonly liveUpdatesEnabled = signal(false);

  // Highlighted point (when clicked in GPS list)
  readonly highlightedPoint = signal<GpsDataPoint | null>(null);

  // Sidebar visibility
  readonly sidebarOpen = signal(true);

  // Map section collapsed state
  readonly mapCollapsed = signal(false);

  // Maximized sensor chart
  readonly maximizedSensor = signal<SensorConfig | null>(null);

  /** Convert GPS points to MapPoint format for the LeafletMapComponent */
  readonly mapPoints = computed<MapPoint[]>(() => {
    const data = this.gpsData();
    if (!data?.points?.length) return [];

    return data.points.map(p => ({
      lat: p.latitude,
      lon: p.longitude,
      ts: p.timestamp,
      speed: p.speed,
      temperature: p.temperature,
      humidity: p.humidity,
      altitude: p.altitude,
      pressure: p.pressure,
      illuminance: p.illuminance,
      waterTemperature: p.waterTemperature,
      gpsSatellites: p.gpsSatellites,
      gpsFix: p.gpsFix,
      hdop: p.hdop
    }));
  });

  /** Convert GPS points to GpsDataPoint format for the list component */
  readonly gpsDataPoints = computed<GpsDataPoint[]>(() => {
    const data = this.gpsData();
    if (!data?.points?.length) return [];

    return data.points.map(p => ({
      timestamp: p.timestamp,
      latitude: p.latitude,
      longitude: p.longitude,
      altitude: p.altitude,
      speed: p.speed,
      heading: undefined,
      sensorReadings: this.extractSensorReadings(p)
    }));
  });

  /** Trail coordinates for polyline */
  readonly trail = computed<[number, number][]>(() => {
    const data = this.gpsData();
    if (!data?.trail?.length) return [];
    return data.trail.map(t => [t[0], t[1]] as [number, number]);
  });

  /** Current position (last point) */
  readonly currentLat = computed(() => {
    const points = this.mapPoints();
    return points.length > 0 ? points[points.length - 1].lat : null;
  });

  readonly currentLon = computed(() => {
    const points = this.mapPoints();
    return points.length > 0 ? points[points.length - 1].lon : null;
  });

  /** Statistics computed from expedition data */
  readonly stats = computed(() => {
    const exp = this.expedition();
    if (!exp) return null;

    return {
      distance: exp.totalDistanceKm ?? 0,
      readings: exp.totalReadings ?? 0,
      avgSpeed: exp.averageSpeedKmh ?? 0,
      maxSpeed: exp.maxSpeedKmh ?? 0,
      duration: exp.duration
    };
  });

  /** Available sensors - only those with actual data */
  readonly availableSensors = computed<SensorConfig[]>(() => {
    const points = this.gpsData()?.points;
    console.log('[Expedition] GPS points count:', points?.length || 0);
    if (!points?.length) return [];

    const available = SENSOR_CONFIGS.filter(config => {
      return points.some(p => {
        const value = (p as unknown as Record<string, unknown>)[config.key];
        return value != null;
      });
    });
    console.log('[Expedition] Available sensors:', available.map(s => s.key));
    return available;
  });

  constructor() {
    // Setup SignalR subscription when expedition is active
    effect(() => {
      const live = this.isLive();
      const exp = this.expedition();
      const enabled = this.liveUpdatesEnabled();

      console.log('[Expedition] Live check - isLive:', live, 'liveUpdatesEnabled:', enabled, 'expedition:', exp?.id);

      if (live && !enabled && exp) {
        this.startLiveUpdates();
      }
    });

    // React to new readings - refresh GPS data when new reading arrives for this node
    effect(() => {
      const reading = this.signalRService.latestReading();
      const exp = this.expedition();

      if (reading && exp) {
        console.log('[Expedition] Reading received:', reading.nodeId, 'expedition node:', exp.nodeId);

        // Check if reading is for this expedition's node
        if (reading.nodeId === exp.nodeId) {
          console.log('[Expedition] Matching reading! Refreshing GPS data...');
          this.refreshGpsData();
        }
      }
    }, { allowSignalWrites: true });
  }

  ngOnInit(): void {
    this.route.params
      .pipe(
        takeUntil(this.destroy$),
        switchMap(params => {
          const id = params['id'];
          if (!id) {
            this.error.set('Keine Expedition-ID angegeben');
            this.isLoading.set(false);
            return of(null);
          }

          this.isLoading.set(true);
          return forkJoin({
            expedition: this.expeditionApi.getById(id),
            gpsData: this.expeditionApi.getGpsData(id)
          });
        })
      )
      .subscribe({
        next: result => {
          if (result) {
            this.expedition.set(result.expedition);
            this.gpsData.set(result.gpsData);
          }
          this.isLoading.set(false);
        },
        error: err => {
          console.error('Error loading expedition:', err);
          this.error.set('Fehler beim Laden der Expedition');
          this.isLoading.set(false);
        }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.stopLiveUpdates();
  }

  /** Start SignalR live updates for active expeditions */
  async startLiveUpdates(): Promise<void> {
    const exp = this.expedition();
    if (!exp) {
      console.log('[Expedition] Cannot start live updates - no expedition');
      return;
    }

    console.log('[Expedition] Starting live updates for expedition:', exp.id, 'node:', exp.nodeId);

    // Ensure SignalR is connected
    const connectionState = this.signalRService.connectionState();
    console.log('[Expedition] SignalR connection state:', connectionState);

    if (connectionState !== 'connected') {
      try {
        console.log('[Expedition] Starting SignalR connection...');
        await this.signalRService.startConnection();
        console.log('[Expedition] SignalR connection started successfully');
      } catch (error) {
        console.error('[Expedition] Failed to start SignalR connection:', error);
        return;
      }
    }

    // Join the node group to receive readings for this node
    try {
      console.log('[Expedition] Joining node group:', exp.nodeId);
      await this.signalRService.joinNodeGroup(exp.nodeId);
      this.liveUpdatesEnabled.set(true);
      console.log('[Expedition] Successfully joined node group. Live updates enabled.');
    } catch (error) {
      console.error('[Expedition] Failed to join node group:', error);
    }
  }

  /** Stop SignalR live updates */
  stopLiveUpdates(): void {
    const exp = this.expedition();
    if (exp && this.liveUpdatesEnabled()) {
      this.signalRService.leaveNodeGroup(exp.nodeId);
      this.liveUpdatesEnabled.set(false);
      console.log('[Expedition] Live updates stopped');
    }
  }

  /** Toggle live updates manually */
  toggleLiveUpdates(): void {
    if (this.liveUpdatesEnabled()) {
      this.stopLiveUpdates();
    } else {
      this.startLiveUpdates();
    }
  }

  /** Refresh GPS data (for live updates) */
  private refreshGpsData(): void {
    const exp = this.expedition();
    if (!exp) return;

    this.expeditionApi.getGpsData(exp.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: data => this.gpsData.set(data),
        error: err => console.error('Error refreshing GPS data:', err)
      });
  }

  /** Handle GPS point click from the list - fly to position */
  onGpsPointClick(point: GpsDataPoint): void {
    this.highlightedPoint.set(point);
  }

  /** Toggle sidebar visibility */
  toggleSidebar(): void {
    this.sidebarOpen.update(v => !v);
  }

  /** Toggle map section collapsed state */
  toggleMapCollapsed(): void {
    this.mapCollapsed.update(v => !v);
  }

  /** Open maximized chart view */
  maximizeChart(sensor: SensorConfig): void {
    this.maximizedSensor.set(sensor);
  }

  /** Close maximized chart view */
  closeMaximizedChart(): void {
    this.maximizedSensor.set(null);
  }

  /** Fit map to show entire route */
  fitMapToBounds(): void {
    if (this.mapComponent) {
      this.mapComponent.fitBoundsToTrail();
    }
    this.highlightedPoint.set(null);
  }

  goBack(): void {
    this.router.navigate(['/expeditions']);
  }

  // ========== Sensor Data Methods ==========

  /** Check if sensor data exists for a given type */
  hasSensorData(sensorType: string): boolean {
    const points = this.gpsData()?.points;
    if (!points?.length) return false;

    return points.some(p => {
      const value = (p as unknown as Record<string, unknown>)[sensorType];
      return value != null;
    });
  }

  /** Get the latest sensor value */
  getLatestSensorValue(sensorType: string): number | null {
    const points = this.gpsData()?.points;
    if (!points?.length) return null;

    // Find the most recent point with this sensor value
    for (let i = points.length - 1; i >= 0; i--) {
      const value = (points[i] as unknown as Record<string, unknown>)[sensorType];
      if (value != null && typeof value === 'number') {
        return value;
      }
    }
    return null;
  }

  /** Get minimum sensor value */
  getSensorMin(sensorType: string): number | null {
    const points = this.gpsData()?.points;
    if (!points?.length) return null;

    const values = points
      .map(p => (p as unknown as Record<string, unknown>)[sensorType])
      .filter((v): v is number => v != null && typeof v === 'number');

    return values.length > 0 ? Math.min(...values) : null;
  }

  /** Get maximum sensor value */
  getSensorMax(sensorType: string): number | null {
    const points = this.gpsData()?.points;
    if (!points?.length) return null;

    const values = points
      .map(p => (p as unknown as Record<string, unknown>)[sensorType])
      .filter((v): v is number => v != null && typeof v === 'number');

    return values.length > 0 ? Math.max(...values) : null;
  }

  /** Format speed from m/s to km/h */
  formatSpeedKmh(speedMs: number | null): string {
    if (speedMs == null) return '-';
    return (speedMs * 3.6).toFixed(1);
  }

  /** Get chart data points for a sensor config (applies conversion if defined) */
  getChartDataForSensor(config: SensorConfig): ChartPoint[] {
    const points = this.gpsData()?.points;
    if (!points?.length) {
      console.log('[Expedition] getChartDataForSensor - no points');
      return [];
    }

    const chartData = points
      .filter(p => (p as unknown as Record<string, unknown>)[config.key] != null)
      .map(p => {
        const rawValue = (p as unknown as Record<string, unknown>)[config.key] as number;
        const value = config.convertFn ? config.convertFn(rawValue) : rawValue;
        return {
          timestamp: p.timestamp,
          value
        };
      })
      .sort((a, b) => new Date(a.timestamp).getTime() - new Date(b.timestamp).getTime());

    console.log('[Expedition] Chart data for', config.key, ':', chartData.length, 'points');
    return chartData;
  }

  /** Get latest value for a sensor config */
  getLatestValueForSensor(config: SensorConfig): number | null {
    const points = this.gpsData()?.points;
    if (!points?.length) return null;

    for (let i = points.length - 1; i >= 0; i--) {
      const rawValue = (points[i] as unknown as Record<string, unknown>)[config.key];
      if (rawValue != null && typeof rawValue === 'number') {
        return config.convertFn ? config.convertFn(rawValue) : rawValue;
      }
    }
    return null;
  }

  /** Get min value for a sensor config */
  getMinValueForSensor(config: SensorConfig): number | null {
    const points = this.gpsData()?.points;
    if (!points?.length) return null;

    const values = points
      .map(p => (p as unknown as Record<string, unknown>)[config.key])
      .filter((v): v is number => v != null && typeof v === 'number')
      .map(v => config.convertFn ? config.convertFn(v) : v);

    return values.length > 0 ? Math.min(...values) : null;
  }

  /** Get max value for a sensor config */
  getMaxValueForSensor(config: SensorConfig): number | null {
    const points = this.gpsData()?.points;
    if (!points?.length) return null;

    const values = points
      .map(p => (p as unknown as Record<string, unknown>)[config.key])
      .filter((v): v is number => v != null && typeof v === 'number')
      .map(v => config.convertFn ? config.convertFn(v) : v);

    return values.length > 0 ? Math.max(...values) : null;
  }

  /** Format value with correct decimals */
  formatSensorValue(value: number | null, decimals: number): string {
    if (value == null) return '-';
    return value.toFixed(decimals);
  }

  /** Get chart data points for a specific sensor type */
  getChartDataPoints(sensorType: string): ChartPoint[] {
    const points = this.gpsData()?.points;
    if (!points?.length) return [];

    return points
      .filter(p => (p as unknown as Record<string, unknown>)[sensorType] != null)
      .map(p => ({
        timestamp: p.timestamp,
        value: (p as unknown as Record<string, unknown>)[sensorType] as number
      }))
      .sort((a, b) => new Date(a.timestamp).getTime() - new Date(b.timestamp).getTime());
  }

  /** Get chart data points for speed (converted to km/h) */
  getSpeedChartDataPoints(): ChartPoint[] {
    const points = this.gpsData()?.points;
    if (!points?.length) return [];

    return points
      .filter(p => p.speed != null)
      .map(p => ({
        timestamp: p.timestamp,
        value: (p.speed ?? 0) * 3.6 // Convert m/s to km/h
      }))
      .sort((a, b) => new Date(a.timestamp).getTime() - new Date(b.timestamp).getTime());
  }

  /** Open sensor detail view (drill-down) - navigates to dashboard widget detail */
  openSensorDetail(sensorKey: string): void {
    const exp = this.expedition();
    if (!exp) return;

    // Map sensor key to measurementType for dashboard
    const measurementTypeMap: Record<string, string> = {
      'temperature': 'temperature',
      'humidity': 'humidity',
      'pressure': 'pressure',
      'waterTemperature': 'water_temperature',
      'illuminance': 'illuminance',
      'altitude': 'altitude',
      'speed': 'speed',
      'gpsSatellites': 'gps_satellites',
      'hdop': 'hdop'
    };

    const measurementType = measurementTypeMap[sensorKey] || sensorKey;

    // Navigate to dashboard widget detail
    // URL: /dashboard/widget/{nodeId}/{assignmentId}/{measurementType}
    // Note: assignmentId comes from the node's sensor assignment
    // For expeditions, we use 'expedition' as placeholder - backend should handle this
    this.router.navigate(['/dashboard/widget', exp.nodeId, 'expedition', measurementType]);
  }

  /** Extract sensor readings from GPS point for display in list */
  private extractSensorReadings(point: ExpeditionGpsPoint): GpsDataPoint['sensorReadings'] {
    const readings: GpsDataPoint['sensorReadings'] = [];

    if (point.temperature != null) {
      readings.push({
        sensorId: 'temperature',
        sensorName: 'Temperatur',
        sensorType: 'temperature',
        value: point.temperature,
        unit: '°C'
      });
    }

    if (point.humidity != null) {
      readings.push({
        sensorId: 'humidity',
        sensorName: 'Luftfeuchtigkeit',
        sensorType: 'humidity',
        value: point.humidity,
        unit: '%'
      });
    }

    if (point.pressure != null) {
      readings.push({
        sensorId: 'pressure',
        sensorName: 'Luftdruck',
        sensorType: 'pressure',
        value: point.pressure,
        unit: 'hPa'
      });
    }

    if (point.waterTemperature != null) {
      readings.push({
        sensorId: 'waterTemperature',
        sensorName: 'Wassertemperatur',
        sensorType: 'waterTemperature',
        value: point.waterTemperature,
        unit: '°C'
      });
    }

    if (point.illuminance != null) {
      readings.push({
        sensorId: 'illuminance',
        sensorName: 'Helligkeit',
        sensorType: 'illuminance',
        value: point.illuminance,
        unit: 'lux'
      });
    }

    if (point.altitude != null) {
      readings.push({
        sensorId: 'altitude',
        sensorName: 'Höhe',
        sensorType: 'altitude',
        value: point.altitude,
        unit: 'm'
      });
    }

    if (point.speed != null) {
      readings.push({
        sensorId: 'speed',
        sensorName: 'Geschwindigkeit',
        sensorType: 'speed',
        value: point.speed * 3.6, // Convert m/s to km/h
        unit: 'km/h'
      });
    }

    return readings;
  }

  getStatusClass(status: ExpeditionStatus): string {
    switch (status) {
      case ExpeditionStatus.Planned:
        return 'status-planned';
      case ExpeditionStatus.Active:
        return 'status-active';
      case ExpeditionStatus.Completed:
        return 'status-completed';
      case ExpeditionStatus.Archived:
        return 'status-archived';
      default:
        return '';
    }
  }

  getStatusLabel(status: ExpeditionStatus): string {
    switch (status) {
      case ExpeditionStatus.Planned:
        return 'Geplant';
      case ExpeditionStatus.Active:
        return 'Aktiv';
      case ExpeditionStatus.Completed:
        return 'Abgeschlossen';
      case ExpeditionStatus.Archived:
        return 'Archiviert';
      default:
        return status;
    }
  }

  getStatusIcon(status: ExpeditionStatus): string {
    switch (status) {
      case ExpeditionStatus.Planned:
        return 'schedule';
      case ExpeditionStatus.Active:
        return 'play_arrow';
      case ExpeditionStatus.Completed:
        return 'check_circle';
      case ExpeditionStatus.Archived:
        return 'archive';
      default:
        return 'help';
    }
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleString('de-DE', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  formatDuration(duration: string): string {
    const parts = duration.split(':');
    if (parts.length >= 2) {
      const hours = parseInt(parts[0], 10);
      const minutes = parseInt(parts[1], 10);
      if (hours > 0) {
        return `${hours}h ${minutes}min`;
      }
      return `${minutes}min`;
    }
    return duration;
  }

  // ========== Start/End Time ==========

  setStartTime(): void {
    const exp = this.expedition();
    if (!exp) return;

    const now = new Date().toISOString();
    this.expeditionApi.update(exp.id, { startTime: now }).subscribe({
      next: (updated) => {
        this.expedition.set(updated);
      },
      error: (err) => {
        console.error('Failed to set start time:', err);
      }
    });
  }

  setEndTime(): void {
    const exp = this.expedition();
    if (!exp) return;

    const now = new Date().toISOString();
    this.expeditionApi.update(exp.id, { endTime: now }).subscribe({
      next: (updated) => {
        this.expedition.set(updated);
      },
      error: (err) => {
        console.error('Failed to set end time:', err);
      }
    });
  }

  // ========== Edit Drawer ==========

  openEditDrawer(): void {
    if (this.formOverlayRef) return;

    const positionStrategy = this.overlay
      .position()
      .global()
      .right('0')
      .top('0');

    this.formOverlayRef = this.overlay.create({
      positionStrategy,
      hasBackdrop: true,
      backdropClass: 'gt-drawer-backdrop',
      panelClass: ['gt-drawer-panel'],
      width: '420px',
      height: '100vh',
      scrollStrategy: this.overlay.scrollStrategies.block()
    });

    const portal = new ComponentPortal(ExpeditionFormComponent, this.viewContainerRef);
    const componentRef = this.formOverlayRef.attach(portal);

    // Set inputs
    componentRef.setInput('mode', 'edit');
    componentRef.setInput('expedition', this.expedition());

    // Subscribe to outputs
    componentRef.instance.saved.subscribe(async () => {
      this.closeFormDrawer();
      // Reload expedition data
      this.loadExpeditionData();
    });

    componentRef.instance.cancelled.subscribe(() => {
      this.closeFormDrawer();
    });

    this.formOverlayRef.backdropClick().subscribe(() => this.closeFormDrawer());

    // Trigger slide-in animation after attach
    requestAnimationFrame(() => {
      this.formOverlayRef?.addPanelClass('open');
    });
  }

  private closeFormDrawer(): void {
    if (this.formOverlayRef) {
      this.formOverlayRef.removePanelClass('open');
      setTimeout(() => {
        this.formOverlayRef?.dispose();
        this.formOverlayRef = null;
      }, 250);
    }
  }

  private loadExpeditionData(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) return;

    this.isLoading.set(true);
    forkJoin({
      expedition: this.expeditionApi.getById(id),
      gpsData: this.expeditionApi.getGpsData(id)
    }).subscribe({
      next: ({ expedition, gpsData }) => {
        this.expedition.set(expedition);
        this.gpsData.set(gpsData);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to reload expedition:', err);
        this.isLoading.set(false);
      }
    });
  }

  // ========== CSV Export ==========

  exportToCsv(): void {
    const points = this.gpsData()?.points;
    const exp = this.expedition();
    if (!points?.length || !exp) return;

    // Build CSV header
    const headers = [
      'Timestamp',
      'Latitude',
      'Longitude',
      'Altitude (m)',
      'Speed (km/h)',
      'Temperature (°C)',
      'Humidity (%)',
      'Pressure (hPa)',
      'Water Temperature (°C)',
      'Illuminance (lux)',
      'GPS Satellites',
      'HDOP'
    ];

    // Build CSV rows
    const rows = points.map(p => [
      p.timestamp,
      p.latitude?.toString() ?? '',
      p.longitude?.toString() ?? '',
      p.altitude?.toString() ?? '',
      p.speed != null ? (p.speed * 3.6).toFixed(2) : '',
      p.temperature?.toString() ?? '',
      p.humidity?.toString() ?? '',
      p.pressure?.toString() ?? '',
      p.waterTemperature?.toString() ?? '',
      p.illuminance?.toString() ?? '',
      p.gpsSatellites?.toString() ?? '',
      p.hdop?.toString() ?? ''
    ]);

    // Combine header and rows
    const csvContent = [
      headers.join(';'),
      ...rows.map(row => row.join(';'))
    ].join('\n');

    // Create and download file
    const blob = new Blob(['\ufeff' + csvContent], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);

    // Generate filename with expedition name and date
    const startDate = new Date(exp.startTime).toISOString().split('T')[0];
    const filename = `${exp.name.replace(/[^a-z0-9]/gi, '_')}_${startDate}.csv`;

    link.setAttribute('href', url);
    link.setAttribute('download', filename);
    link.style.visibility = 'hidden';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  }
}
