import { Component, OnInit, OnDestroy, inject, signal, input, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatTabsModule } from '@angular/material/tabs';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { FormsModule } from '@angular/forms';
import {
  SensorApiService,
  SensorDataApiService,
  SignalRService
} from '@myiotgrid/shared/data-access';
import { Sensor, SensorData, SensorDataFilter } from '@myiotgrid/shared/models';
import { LoadingSpinnerComponent, EmptyStateComponent } from '@myiotgrid/shared/ui';
import { RelativeTimePipe, SensorUnitPipe } from '@myiotgrid/shared/utils';

@Component({
  selector: 'myiotgrid-sensor-detail',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatChipsModule,
    MatTabsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    FormsModule,
    LoadingSpinnerComponent,
    EmptyStateComponent,
    RelativeTimePipe,
    SensorUnitPipe
  ],
  templateUrl: './sensor-detail.component.html',
  styleUrl: './sensor-detail.component.scss'
})
export class SensorDetailComponent implements OnInit, OnDestroy {
  private readonly router = inject(Router);
  private readonly sensorApiService = inject(SensorApiService);
  private readonly sensorDataApiService = inject(SensorDataApiService);
  private readonly signalRService = inject(SignalRService);

  id = input.required<string>();

  readonly isLoading = signal(true);
  readonly sensor = signal<Sensor | null>(null);
  readonly sensorData = signal<SensorData[]>([]);
  readonly latestValue = signal<SensorData | null>(null);

  timeRange: '24h' | '7d' | '30d' = '24h';

  readonly sensorIcon = computed(() => {
    const code = this.sensor()?.sensorTypeCode?.toLowerCase();
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

  async ngOnInit(): Promise<void> {
    await this.loadSensor();
    this.setupSignalR();
  }

  ngOnDestroy(): void {
    this.signalRService.off('NewSensorData');
  }

  private async loadSensor(): Promise<void> {
    this.isLoading.set(true);
    try {
      const sensor = await this.sensorApiService.getById(this.id()).toPromise();
      this.sensor.set(sensor || null);

      if (sensor) {
        await this.loadSensorData();
      }
    } catch (error) {
      console.error('Error loading sensor:', error);
    } finally {
      this.isLoading.set(false);
    }
  }

  async loadSensorData(): Promise<void> {
    const now = new Date();
    let from: Date;

    switch (this.timeRange) {
      case '7d':
        from = new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000);
        break;
      case '30d':
        from = new Date(now.getTime() - 30 * 24 * 60 * 60 * 1000);
        break;
      default:
        from = new Date(now.getTime() - 24 * 60 * 60 * 1000);
    }

    const filter: SensorDataFilter = {
      sensorId: this.id(),
      from: from.toISOString(),
      to: now.toISOString()
    };

    try {
      const result = await this.sensorDataApiService.getFiltered(filter).toPromise();
      const data = result?.items || [];
      this.sensorData.set(data);

      if (data.length > 0) {
        this.latestValue.set(data[0]);
      }
    } catch (error) {
      console.error('Error loading sensor data:', error);
    }
  }

  private setupSignalR(): void {
    this.signalRService.onNewSensorData((data: SensorData) => {
      if (data.sensorId === this.id()) {
        this.latestValue.set(data);
        this.sensorData.update(existing => [data, ...existing]);
      }
    });
  }

  onTimeRangeChange(): void {
    this.loadSensorData();
  }

  goBack(): void {
    this.router.navigate(['/sensors']);
  }
}
