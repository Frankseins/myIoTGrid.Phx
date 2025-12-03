import { Component, input, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Node, Reading, Protocol } from '@myiotgrid/shared/models';
import { RelativeTimePipe, SensorUnitPipe } from '@myiotgrid/shared/utils';

@Component({
  selector: 'myiotgrid-node-card',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatCardModule,
    MatIconModule,
    MatChipsModule,
    MatTooltipModule,
    RelativeTimePipe,
    SensorUnitPipe
  ],
  templateUrl: './node-card.component.html',
  styleUrl: './node-card.component.scss'
})
export class NodeCardComponent {
  node = input.required<Node>();
  latestReadings = input<Reading[]>([]);

  readonly locationName = computed(() =>
    this.node().location?.name || 'Unbekannt'
  );

  readonly protocolIcon = computed(() => {
    switch (this.node().protocol) {
      case Protocol.WLAN:
        return 'wifi';
      case Protocol.LoRaWAN:
        return 'cell_tower';
      default:
        return 'device_hub';
    }
  });

  readonly protocolLabel = computed(() => {
    switch (this.node().protocol) {
      case Protocol.WLAN:
        return 'WLAN';
      case Protocol.LoRaWAN:
        return 'LoRaWAN';
      default:
        return 'Unbekannt';
    }
  });

  readonly primaryReading = computed(() => {
    const readings = this.latestReadings();
    if (readings.length === 0) return undefined;
    // Return first reading as primary
    return readings[0];
  });

  readonly secondaryReadings = computed(() => {
    const readings = this.latestReadings();
    if (readings.length <= 1) return [];
    return readings.slice(1, 4); // Max 3 secondary readings
  });

  readonly sensorCount = computed(() =>
    this.node().sensors?.length || 0
  );

  getIconForReading(reading: Reading): string {
    // Fallback based on measurement type
    const measurementType = reading.measurementType.toLowerCase();
    if (measurementType.includes('temp')) return 'thermostat';
    if (measurementType.includes('humid') || measurementType.includes('feucht')) return 'water_drop';
    if (measurementType.includes('co2')) return 'air';
    if (measurementType.includes('pressure') || measurementType.includes('druck')) return 'speed';
    if (measurementType.includes('light') || measurementType.includes('licht')) return 'light_mode';
    if (measurementType.includes('pm25') || measurementType.includes('pm10')) return 'cloud';
    return 'sensors';
  }

  getColorForReading(reading: Reading): string {
    // Fallback based on measurement type
    const measurementType = reading.measurementType.toLowerCase();
    if (measurementType.includes('temp')) return '#ff6b6b';
    if (measurementType.includes('humid') || measurementType.includes('feucht')) return '#4dabf7';
    if (measurementType.includes('co2')) return '#a9e34b';
    if (measurementType.includes('pressure') || measurementType.includes('druck')) return '#9775fa';
    if (measurementType.includes('light') || measurementType.includes('licht')) return '#ffd43b';
    if (measurementType.includes('pm')) return '#868e96';
    return '#495057';
  }
}
