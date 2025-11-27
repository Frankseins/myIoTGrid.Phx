import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatListModule } from '@angular/material/list';
import { MatDividerModule } from '@angular/material/divider';
import { HealthApiService, SensorTypeApiService } from '@myiotgrid/shared/data-access';
import { SensorType } from '@myiotgrid/shared/models';
import { LoadingSpinnerComponent } from '@myiotgrid/shared/ui';

interface HealthStatus {
  status: string;
  version?: string;
  uptime?: string;
}

@Component({
  selector: 'myiotgrid-settings',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatListModule,
    MatDividerModule,
    LoadingSpinnerComponent
  ],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.scss'
})
export class SettingsComponent implements OnInit {
  private readonly healthApiService = inject(HealthApiService);
  private readonly sensorTypeApiService = inject(SensorTypeApiService);

  readonly isLoading = signal(true);
  readonly healthStatus = signal<HealthStatus | null>(null);
  readonly sensorTypes = signal<SensorType[]>([]);

  async ngOnInit(): Promise<void> {
    await this.loadData();
  }

  private async loadData(): Promise<void> {
    this.isLoading.set(true);
    try {
      const [health, types] = await Promise.all([
        this.healthApiService.check().toPromise(),
        this.sensorTypeApiService.getAll().toPromise()
      ]);
      this.healthStatus.set(health || null);
      this.sensorTypes.set(types || []);
    } catch (error) {
      console.error('Error loading settings:', error);
    } finally {
      this.isLoading.set(false);
    }
  }

  getSensorTypeIcon(code: string): string {
    switch (code?.toLowerCase()) {
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
      case 'pm25':
      case 'pm10':
        return 'blur_on';
      case 'uv':
        return 'wb_sunny';
      case 'wind_speed':
        return 'air';
      case 'rainfall':
        return 'water';
      case 'water_level':
        return 'waves';
      case 'battery':
        return 'battery_full';
      case 'rssi':
        return 'signal_cellular_alt';
      default:
        return 'sensors';
    }
  }
}
