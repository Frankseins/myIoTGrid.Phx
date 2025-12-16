import { Component, OnInit, OnDestroy, inject, signal, computed, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatTabsModule } from '@angular/material/tabs';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { filter } from 'rxjs/operators';
import { AlertApiService, SignalRService } from '@myiotgrid/shared/data-access';
import { Alert, AlertLevel, AlertSource } from '@myiotgrid/shared/models';
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
  private readonly destroyRef = inject(DestroyRef);

  // Expose enums to template
  readonly AlertSource = AlertSource;

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

  constructor() {
    // Setup reactive subscriptions to SignalR signals
    this.setupSignalREffects();
  }

  /**
   * Setup reactive subscriptions to SignalR signals.
   * Using toObservable + takeUntilDestroyed for automatic cleanup.
   */
  private setupSignalREffects(): void {
    // React to new alerts
    toObservable(this.signalRService.alertReceived)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        filter((alert): alert is Alert => alert !== null)
      )
      .subscribe(alert => {
        this.alerts.update(alerts => [alert, ...alerts]);
      });

    // React to acknowledged alerts
    toObservable(this.signalRService.alertAcknowledged)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        filter((alertId): alertId is string => alertId !== null)
      )
      .subscribe(alertId => {
        this.alerts.update(alerts =>
          alerts.map(a => a.id === alertId ? { ...a, isActive: false, acknowledgedAt: new Date().toISOString() } : a)
        );
      });
  }

  async ngOnInit(): Promise<void> {
    // Ensure SignalR is connected
    try {
      await this.signalRService.startConnection();
    } catch (error) {
      console.error('Error connecting to SignalR:', error);
    }

    await this.loadAlerts();
  }

  ngOnDestroy(): void {
    // Cleanup is handled by takeUntilDestroyed
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
