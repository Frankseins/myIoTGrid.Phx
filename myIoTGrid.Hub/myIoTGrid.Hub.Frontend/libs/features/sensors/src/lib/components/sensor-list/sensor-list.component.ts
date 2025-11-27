import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { FormsModule } from '@angular/forms';
import { SensorApiService, SensorDataApiService } from '@myiotgrid/shared/data-access';
import { Sensor, SensorData } from '@myiotgrid/shared/models';
import { LoadingSpinnerComponent, EmptyStateComponent } from '@myiotgrid/shared/ui';
import { RelativeTimePipe, SensorUnitPipe } from '@myiotgrid/shared/utils';

@Component({
  selector: 'myiotgrid-sensor-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatChipsModule,
    MatFormFieldModule,
    MatSelectModule,
    FormsModule,
    LoadingSpinnerComponent,
    EmptyStateComponent,
    RelativeTimePipe,
    SensorUnitPipe
  ],
  templateUrl: './sensor-list.component.html',
  styleUrl: './sensor-list.component.scss'
})
export class SensorListComponent implements OnInit {
  private readonly sensorApiService = inject(SensorApiService);
  private readonly sensorDataApiService = inject(SensorDataApiService);

  readonly isLoading = signal(true);
  readonly sensors = signal<Sensor[]>([]);
  readonly latestData = signal<Map<string, SensorData>>(new Map());

  filterStatus: 'all' | 'online' | 'offline' = 'all';

  get filteredSensors(): Sensor[] {
    const all = this.sensors();
    switch (this.filterStatus) {
      case 'online':
        return all.filter(s => s.isOnline);
      case 'offline':
        return all.filter(s => !s.isOnline);
      default:
        return all;
    }
  }

  async ngOnInit(): Promise<void> {
    await this.loadSensors();
  }

  private async loadSensors(): Promise<void> {
    this.isLoading.set(true);
    try {
      const [sensors, data] = await Promise.all([
        this.sensorApiService.getAll().toPromise(),
        this.sensorDataApiService.getLatest().toPromise()
      ]);
      this.sensors.set(sensors || []);

      if (data) {
        const dataMap = new Map<string, SensorData>();
        data.forEach(d => {
          const key = d.sensorId || d.hubId;
          const existing = dataMap.get(key);
          if (!existing || new Date(d.timestamp) > new Date(existing.timestamp)) {
            dataMap.set(key, d);
          }
        });
        this.latestData.set(dataMap);
      }
    } catch (error) {
      console.error('Error loading sensors:', error);
    } finally {
      this.isLoading.set(false);
    }
  }

  getLatestData(sensorId: string): SensorData | undefined {
    return this.latestData().get(sensorId);
  }

  getSensorIcon(typeCode: string): string {
    switch (typeCode?.toLowerCase()) {
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
  }
}
