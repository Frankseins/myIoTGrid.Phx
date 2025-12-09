import { Component, Input, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { NodeGpsStatus, GpsQualityLevel, getGpsQualityLevel } from '@myiotgrid/shared/models';

@Component({
  selector: 'myiotgrid-gps-status-badge',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatTooltipModule],
  templateUrl: './gps-status-badge.component.html',
  styleUrl: './gps-status-badge.component.scss'
})
export class GpsStatusBadgeComponent {
  private readonly _gpsStatus = signal<NodeGpsStatus | null>(null);

  @Input()
  set gpsStatus(value: NodeGpsStatus | null | undefined) {
    this._gpsStatus.set(value ?? null);
  }

  @Input() compact = false;

  readonly hasGps = computed(() => this._gpsStatus()?.hasGps ?? false);

  readonly satellites = computed(() => this._gpsStatus()?.satellites ?? 0);

  readonly fixType = computed(() => this._gpsStatus()?.fixType ?? 0);

  readonly fixTypeText = computed(() => this._gpsStatus()?.fixTypeText ?? 'Kein Fix');

  readonly hdop = computed(() => this._gpsStatus()?.hdop ?? 99.99);

  readonly hdopQuality = computed(() => this._gpsStatus()?.hdopQuality ?? 'Poor');

  readonly hasFix = computed(() => this.fixType() >= 2);

  readonly qualityLevel = computed((): GpsQualityLevel => {
    return getGpsQualityLevel(this.hdop(), this.hasFix());
  });

  readonly satelliteIcon = computed(() => {
    if (!this.hasGps()) return 'gps_off';
    if (!this.hasFix()) return 'gps_not_fixed';
    return 'gps_fixed';
  });

  readonly signalBars = computed((): number[] => {
    const sats = this.satellites();
    if (sats >= 10) return [1, 2, 3, 4];
    if (sats >= 7) return [1, 2, 3];
    if (sats >= 4) return [1, 2];
    if (sats >= 1) return [1];
    return [];
  });

  readonly qualityClass = computed(() => {
    const level = this.qualityLevel();
    return `quality-${level}`;
  });

  readonly fixBadgeClass = computed(() => {
    const fix = this.fixType();
    if (fix === 3) return 'fix-3d';
    if (fix === 2) return 'fix-2d';
    return 'fix-none';
  });

  readonly tooltip = computed(() => {
    const status = this._gpsStatus();
    if (!status) return 'Kein GPS-Status verfügbar';
    if (!status.hasGps) return 'Kein GPS-Modul vorhanden';

    const parts: string[] = [];
    parts.push(`Satelliten: ${status.satellites ?? 0}`);
    parts.push(`Fix: ${status.fixTypeText ?? 'Unbekannt'}`);

    // HDOP with null check
    if (status.hdop != null) {
      parts.push(`HDOP: ${status.hdop.toFixed(2)} (${status.hdopQuality ?? 'Unbekannt'})`);
    }

    // Position with null check
    if (status.latitude != null && status.longitude != null) {
      parts.push(`Position: ${status.latitude.toFixed(6)}, ${status.longitude.toFixed(6)}`);
    }

    // Altitude with null check
    if (status.altitude != null) {
      parts.push(`Höhe: ${status.altitude.toFixed(1)} m`);
    }

    // Speed with null check
    if (status.speed != null) {
      parts.push(`Geschwindigkeit: ${status.speed.toFixed(1)} km/h`);
    }

    if (status.lastUpdate) {
      const date = new Date(status.lastUpdate);
      parts.push(`Aktualisiert: ${date.toLocaleTimeString('de-DE')}`);
    }

    return parts.join('\n');
  });
}
