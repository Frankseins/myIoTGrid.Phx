import { Component, OnInit, OnDestroy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import {
  SignalRService,
  SensorApiService,
  SensorDataApiService,
  AlertApiService
} from '@myiotgrid/shared/data-access';
import { Sensor, SensorData, Alert, AlertLevel } from '@myiotgrid/shared/models';
import { LoadingSpinnerComponent, ConnectionStatusComponent, EmptyStateComponent } from '@myiotgrid/shared/ui';
import { RelativeTimePipe, SensorUnitPipe, AlertLevelPipe } from '@myiotgrid/shared/utils';
import { SensorCardComponent } from '../sensor-card/sensor-card.component';
import { AlertBannerComponent } from '../alert-banner/alert-banner.component';

@Component({
  selector: 'myiotgrid-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatChipsModule,
    MatTooltipModule,
    LoadingSpinnerComponent,
    ConnectionStatusComponent,
    EmptyStateComponent,
    SensorCardComponent,
    AlertBannerComponent,
    RelativeTimePipe,
    SensorUnitPipe,
    AlertLevelPipe
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit, OnDestroy {
  private readonly signalRService = inject(SignalRService);
  private readonly sensorApiService = inject(SensorApiService);
  private readonly sensorDataApiService = inject(SensorDataApiService);
  private readonly alertApiService = inject(AlertApiService);

  readonly isLoading = signal(true);
  readonly sensors = signal<Sensor[]>([]);
  readonly latestSensorData = signal<Map<string, SensorData>>(new Map());
  readonly activeAlerts = signal<Alert[]>([]);

  readonly criticalAlerts = computed(() =>
    this.activeAlerts().filter(a => a.level === AlertLevel.Critical)
  );

  readonly warningAlerts = computed(() =>
    this.activeAlerts().filter(a => a.level === AlertLevel.Warning)
  );

  readonly onlineSensors = computed(() =>
    this.sensors().filter(s => s.isOnline)
  );

  readonly offlineSensors = computed(() =>
    this.sensors().filter(s => !s.isOnline)
  );

  async ngOnInit(): Promise<void> {
    await this.loadData();
    await this.setupSignalR();
  }

  ngOnDestroy(): void {
    this.signalRService.off('NewSensorData');
    this.signalRService.off('AlertReceived');
    this.signalRService.off('SensorStatusChanged');
  }

  private async loadData(): Promise<void> {
    this.isLoading.set(true);
    try {
      const [sensors, alerts, latestData] = await Promise.all([
        this.sensorApiService.getAll().toPromise(),
        this.alertApiService.getActive().toPromise(),
        this.sensorDataApiService.getLatest().toPromise()
      ]);

      this.sensors.set(sensors || []);
      this.activeAlerts.set(alerts || []);

      if (latestData) {
        const dataMap = new Map<string, SensorData>();
        latestData.forEach(data => {
          const key = data.sensorId || data.hubId;
          const existing = dataMap.get(key);
          if (!existing || new Date(data.timestamp) > new Date(existing.timestamp)) {
            dataMap.set(key, data);
          }
        });
        this.latestSensorData.set(dataMap);
      }
    } catch (error) {
      console.error('Error loading dashboard data:', error);
    } finally {
      this.isLoading.set(false);
    }
  }

  private async setupSignalR(): Promise<void> {
    try {
      await this.signalRService.startConnection();

      this.signalRService.onNewSensorData((data: SensorData) => {
        this.latestSensorData.update(map => {
          const newMap = new Map(map);
          const key = data.sensorId || data.hubId;
          newMap.set(key, data);
          return newMap;
        });
      });

      this.signalRService.onAlertReceived((alert: Alert) => {
        this.activeAlerts.update(alerts => [alert, ...alerts]);
      });

      this.signalRService.onSensorStatusChanged((sensorId: string, isOnline: boolean) => {
        this.sensors.update(sensors =>
          sensors.map(s => s.id === sensorId ? { ...s, isOnline } : s)
        );
      });
    } catch (error) {
      console.error('Error connecting to SignalR:', error);
    }
  }

  async acknowledgeAlert(alertId: string): Promise<void> {
    try {
      await this.alertApiService.acknowledge(alertId).toPromise();
      this.activeAlerts.update(alerts =>
        alerts.filter(a => a.id !== alertId)
      );
    } catch (error) {
      console.error('Error acknowledging alert:', error);
    }
  }

  getLatestDataForSensor(sensorId: string): SensorData | undefined {
    return this.latestSensorData().get(sensorId);
  }
}
