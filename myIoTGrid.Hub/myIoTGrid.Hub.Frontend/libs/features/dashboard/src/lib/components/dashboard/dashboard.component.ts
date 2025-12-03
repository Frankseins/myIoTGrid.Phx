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
  HubApiService,
  NodeApiService,
  ReadingApiService,
  AlertApiService
} from '@myiotgrid/shared/data-access';
import { Hub, Node, Reading, Alert, AlertLevel } from '@myiotgrid/shared/models';
import { LoadingSpinnerComponent, ConnectionStatusComponent, EmptyStateComponent } from '@myiotgrid/shared/ui';
import { NodeCardComponent } from '../node-card/node-card.component';
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
    NodeCardComponent,
    AlertBannerComponent
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit, OnDestroy {
  private readonly signalRService = inject(SignalRService);
  private readonly hubApiService = inject(HubApiService);
  private readonly nodeApiService = inject(NodeApiService);
  private readonly readingApiService = inject(ReadingApiService);
  private readonly alertApiService = inject(AlertApiService);

  readonly isLoading = signal(true);
  readonly hubs = signal<Hub[]>([]);
  readonly nodes = signal<Node[]>([]);
  readonly latestReadings = signal<Map<string, Reading[]>>(new Map());
  readonly activeAlerts = signal<Alert[]>([]);

  readonly criticalAlerts = computed(() =>
    this.activeAlerts().filter(a => a.level === AlertLevel.Critical)
  );

  readonly warningAlerts = computed(() =>
    this.activeAlerts().filter(a => a.level === AlertLevel.Warning)
  );

  readonly onlineHubs = computed(() =>
    this.hubs().filter(h => h.isOnline)
  );

  readonly offlineHubs = computed(() =>
    this.hubs().filter(h => !h.isOnline)
  );

  readonly onlineNodes = computed(() =>
    this.nodes().filter(n => n.isOnline)
  );

  readonly offlineNodes = computed(() =>
    this.nodes().filter(n => !n.isOnline)
  );

  async ngOnInit(): Promise<void> {
    await this.loadData();
    await this.setupSignalR();
  }

  ngOnDestroy(): void {
    this.signalRService.off('NewReading');
    this.signalRService.off('AlertReceived');
    this.signalRService.off('HubStatusChanged');
    this.signalRService.off('NodeStatusChanged');
  }

  private async loadData(): Promise<void> {
    this.isLoading.set(true);
    try {
      // Load hubs, nodes, alerts, and latest readings
      const [hubs, nodes, alerts, latestReadings] = await Promise.all([
        this.hubApiService.getAll().toPromise(),
        this.nodeApiService.getAll().toPromise(),
        this.alertApiService.getActive().toPromise(),
        this.readingApiService.getLatest().toPromise()
      ]);

      this.hubs.set(hubs || []);
      this.nodes.set(nodes || []);
      this.activeAlerts.set(alerts || []);

      if (latestReadings) {
        const readingMap = new Map<string, Reading[]>();
        latestReadings.forEach(reading => {
          const nodeId = reading.nodeId;
          const existing = readingMap.get(nodeId) || [];
          existing.push(reading);
          readingMap.set(nodeId, existing);
        });
        this.latestReadings.set(readingMap);
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

      this.signalRService.onNewReading((reading: Reading) => {
        this.latestReadings.update(map => {
          const newMap = new Map(map);
          const nodeId = reading.nodeId;
          const existing = newMap.get(nodeId) || [];
          // Update or add the reading for this measurement type
          const index = existing.findIndex(r => r.measurementType === reading.measurementType);
          if (index >= 0) {
            existing[index] = reading;
          } else {
            existing.push(reading);
          }
          newMap.set(nodeId, [...existing]);
          return newMap;
        });
      });

      this.signalRService.onAlertReceived((alert: Alert) => {
        this.activeAlerts.update(alerts => [alert, ...alerts]);
      });

      this.signalRService.onHubStatusChanged((hub: Hub) => {
        this.hubs.update(hubs =>
          hubs.map(h => h.id === hub.id ? { ...h, ...hub } : h)
        );
      });

      this.signalRService.onNodeStatusChanged((node: Node) => {
        this.nodes.update(nodes =>
          nodes.map(n => n.id === node.id ? { ...n, ...node } : n)
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

  getLatestReadingsForNode(nodeId: string): Reading[] {
    return this.latestReadings().get(nodeId) || [];
  }
}
