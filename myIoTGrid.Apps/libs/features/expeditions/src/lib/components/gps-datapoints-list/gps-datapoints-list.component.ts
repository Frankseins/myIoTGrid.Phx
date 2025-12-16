import { Component, input, output, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ScrollingModule } from '@angular/cdk/scrolling';

/** Sensor reading snapshot for a GPS point */
export interface SensorReadingSnapshot {
  sensorId: string;
  sensorName: string;
  sensorType: string;
  value: number;
  unit: string;
}

/** GPS data point with sensor readings */
export interface GpsDataPoint {
  timestamp: string;
  latitude: number;
  longitude: number;
  altitude?: number;
  speed?: number;
  heading?: number;
  sensorReadings: SensorReadingSnapshot[];
}

@Component({
  selector: 'myiotgrid-gps-datapoints-list',
  standalone: true,
  imports: [
    CommonModule,
    MatExpansionModule,
    MatIconModule,
    MatButtonModule,
    MatTooltipModule,
    MatProgressSpinnerModule,
    ScrollingModule
  ],
  templateUrl: './gps-datapoints-list.component.html',
  styleUrl: './gps-datapoints-list.component.scss'
})
export class GpsDatapointsListComponent {
  /** Input: Array of GPS data points */
  dataPoints = input.required<GpsDataPoint[]>();

  /** Input: Loading state */
  isLoading = input<boolean>(false);

  /** Input: Auto-scroll enabled */
  autoScrollEnabled = input<boolean>(true);

  /** Output: Emits when a point is clicked */
  pointClick = output<GpsDataPoint>();

  /** Sort order: newest first or oldest first */
  sortNewestFirst = signal(true);

  /** Auto-scroll toggle */
  autoScroll = signal(true);

  /** Sorted data points */
  sortedDataPoints = computed(() => {
    const points = [...this.dataPoints()];
    if (this.sortNewestFirst()) {
      return points.sort((a, b) =>
        new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime()
      );
    }
    return points.sort((a, b) =>
      new Date(a.timestamp).getTime() - new Date(b.timestamp).getTime()
    );
  });

  /** Handle point click */
  onPointClick(point: GpsDataPoint, event: Event): void {
    event.stopPropagation();
    this.pointClick.emit(point);
  }

  /** Toggle sort order */
  toggleSort(): void {
    this.sortNewestFirst.update(v => !v);
  }

  /** Toggle auto-scroll */
  toggleAutoScroll(): void {
    this.autoScroll.update(v => !v);
  }

  /** Get sensor icon based on type */
  getSensorIcon(sensorType: string): string {
    const iconMap: Record<string, string> = {
      temperature: 'thermostat',
      humidity: 'water_drop',
      pressure: 'speed',
      waterTemperature: 'waves',
      illuminance: 'light_mode',
      uv: 'wb_sunny',
      co2: 'co2',
      pm25: 'air',
      pm10: 'air',
      soil_moisture: 'grass',
      wind_speed: 'air',
      rainfall: 'water',
      battery: 'battery_full',
      rssi: 'signal_cellular_alt'
    };
    return iconMap[sensorType] || 'sensors';
  }

  /** Format timestamp for display */
  formatTime(timestamp: string): string {
    return new Date(timestamp).toLocaleTimeString('de-DE', {
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit'
    });
  }

  /** Format date for display */
  formatDate(timestamp: string): string {
    return new Date(timestamp).toLocaleDateString('de-DE', {
      day: '2-digit',
      month: '2-digit'
    });
  }

  /** Convert speed from m/s to km/h */
  formatSpeed(speed: number | undefined): string {
    if (speed == null) return '-';
    return (speed * 3.6).toFixed(1);
  }

  /** Format altitude */
  formatAltitude(altitude: number | undefined): string {
    if (altitude == null) return '-';
    return altitude.toFixed(1);
  }

  /** Format coordinates */
  formatCoordinate(value: number, decimals: number = 6): string {
    return value.toFixed(decimals);
  }

  /** Track by function for ngFor */
  trackByTimestamp(index: number, point: GpsDataPoint): string {
    return point.timestamp;
  }
}
