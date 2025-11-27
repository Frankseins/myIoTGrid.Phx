import { Component, input, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Sensor, SensorData, Protocol } from '@myiotgrid/shared/models';
import { RelativeTimePipe, SensorUnitPipe, ProtocolPipe } from '@myiotgrid/shared/utils';

@Component({
  selector: 'myiotgrid-sensor-card',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatCardModule,
    MatIconModule,
    MatChipsModule,
    MatTooltipModule,
    RelativeTimePipe,
    SensorUnitPipe,
    ProtocolPipe
  ],
  templateUrl: './sensor-card.component.html',
  styleUrl: './sensor-card.component.scss'
})
export class SensorCardComponent {
  sensor = input.required<Sensor>();
  latestData = input<SensorData | undefined>();

  readonly locationName = computed(() =>
    this.sensor().defaultLocation?.name || 'Unbekannt'
  );

  readonly sensorIcon = computed(() => {
    const code = this.sensor().sensorTypeCode?.toLowerCase();
    switch (code) {
      case 'temperature':
        return 'thermostat';
      case 'humidity':
        return 'water_drop';
      case 'co2':
        return 'air';
      case 'pressure':
        return 'speed';
      case 'light':
        return 'light_mode';
      case 'soil_moisture':
        return 'grass';
      default:
        return 'sensors';
    }
  });

  readonly protocolIcon = computed(() => {
    switch (this.sensor().sensorTypeId) {
      default:
        return 'wifi';
    }
  });
}
