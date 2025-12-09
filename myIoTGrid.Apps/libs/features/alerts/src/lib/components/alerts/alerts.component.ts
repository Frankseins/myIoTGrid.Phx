import { Component, OnInit, OnDestroy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatTabsModule } from '@angular/material/tabs';
import { AlertApiService, SignalRService } from '@myiotgrid/shared/data-access';
import { Alert, AlertLevel } from '@myiotgrid/shared/models';
import { LoadingSpinnerComponent, EmptyStateComponent } from '@myiotgrid/shared/ui';
import { RelativeTimePipe, AlertLevelPipe } from '@myiotgrid/shared/utils';

@Component({
  selector: 'myiotgrid-alerts',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatChipsModule,
    MatTabsModule,
    LoadingSpinnerComponent,
    EmptyStateComponent,
    RelativeTimePipe,
    AlertLevelPipe
  ],
  templateUrl: './alerts.component.html',
  styleUrl: './alerts.component.scss'
})
export class AlertsComponent implements OnInit, OnDestroy {
  private readonly alertApiService = inject(AlertApiService);
  private readonly signalRService = inject(SignalRService);

  readonly isLoading = signal(true);
  readonly alerts = signal<Alert[]>([]);
  readonly selectedTab = signal(0);

  readonly activeAlerts = computed(() =>
    this.alerts().filter(a => a.isActive)
  );

  readonly acknowledgedAlerts = computed(() =>
    this.alerts().filter(a => !a.isActive)
  );

  readonly criticalCount = computed(() =>
    this.activeAlerts().filter(a => a.level === AlertLevel.Critical).length
  );

  readonly warningCount = computed(() =>
    this.activeAlerts().filter(a => a.level === AlertLevel.Warning).length
  );

  async ngOnInit(): Promise<void> {
    await this.loadAlerts();
    this.setupSignalR();
  }

  ngOnDestroy(): void {
    this.signalRService.off('AlertReceived');
    this.signalRService.off('AlertAcknowledged');
  }

  private async loadAlerts(): Promise<void> {
    this.isLoading.set(true);
    try {
      const alerts = await this.alertApiService.getActive().toPromise();
      this.alerts.set(alerts || []);
    } catch (error) {
      console.error('Error loading alerts:', error);
    } finally {
      this.isLoading.set(false);
    }
  }

  private setupSignalR(): void {
    this.signalRService.onAlertReceived((alert: Alert) => {
      this.alerts.update(alerts => [alert, ...alerts]);
    });

    this.signalRService.onAlertAcknowledged((alertId: string) => {
      this.alerts.update(alerts =>
        alerts.map(a => a.id === alertId ? { ...a, isActive: false, acknowledgedAt: new Date().toISOString() } : a)
      );
    });
  }

  async acknowledgeAlert(alertId: string): Promise<void> {
    try {
      await this.alertApiService.acknowledge(alertId).toPromise();
      this.alerts.update(alerts =>
        alerts.map(a => a.id === alertId ? { ...a, isActive: false, acknowledgedAt: new Date().toISOString() } : a)
      );
    } catch (error) {
      console.error('Error acknowledging alert:', error);
    }
  }

  getAlertIcon(level: AlertLevel): string {
    switch (level) {
      case AlertLevel.Critical:
        return 'error';
      case AlertLevel.Warning:
        return 'warning';
      case AlertLevel.Info:
        return 'info';
      default:
        return 'check_circle';
    }
  }

  getAlertClass(level: AlertLevel): string {
    switch (level) {
      case AlertLevel.Critical:
        return 'alert-critical';
      case AlertLevel.Warning:
        return 'alert-warning';
      case AlertLevel.Info:
        return 'alert-info';
      default:
        return 'alert-ok';
    }
  }
}
