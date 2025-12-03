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
  NodeApiService,
  ReadingApiService,
  SignalRService
} from '@myiotgrid/shared/data-access';
import { Node, Reading, ReadingFilter } from '@myiotgrid/shared/models';
import { LoadingSpinnerComponent, EmptyStateComponent } from '@myiotgrid/shared/ui';
import { RelativeTimePipe, SensorUnitPipe } from '@myiotgrid/shared/utils';

@Component({
  selector: 'myiotgrid-node-detail',
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
  templateUrl: './node-detail.component.html',
  styleUrl: './node-detail.component.scss'
})
export class NodeDetailComponent implements OnInit, OnDestroy {
  private readonly router = inject(Router);
  private readonly nodeApiService = inject(NodeApiService);
  private readonly readingApiService = inject(ReadingApiService);
  private readonly signalRService = inject(SignalRService);

  id = input.required<string>();

  readonly isLoading = signal(true);
  readonly node = signal<Node | null>(null);
  readonly readings = signal<Reading[]>([]);
  readonly latestReadings = signal<Reading[]>([]);

  timeRange: '24h' | '7d' | '30d' = '24h';

  readonly nodeIcon = computed(() => {
    const protocol = this.node()?.protocol;
    switch (protocol) {
      case 1: // WLAN
        return 'wifi';
      case 2: // LoRaWAN
        return 'cell_tower';
      default:
        return 'router';
    }
  });

  async ngOnInit(): Promise<void> {
    await this.loadNode();
    this.setupSignalR();
  }

  ngOnDestroy(): void {
    this.signalRService.off('NewReading');
  }

  private async loadNode(): Promise<void> {
    this.isLoading.set(true);
    try {
      const node = await this.nodeApiService.getById(this.id()).toPromise();
      this.node.set(node || null);

      if (node) {
        await this.loadReadings();
      }
    } catch (error) {
      console.error('Error loading node:', error);
    } finally {
      this.isLoading.set(false);
    }
  }

  async loadReadings(): Promise<void> {
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

    const filter: ReadingFilter = {
      nodeId: this.id(),
      from: from.toISOString(),
      to: now.toISOString()
    };

    try {
      const result = await this.readingApiService.getFiltered(filter).toPromise();
      const data = result?.items || [];
      this.readings.set(data);

      // Get latest reading per measurement type
      const latest = new Map<string, Reading>();
      data.forEach(r => {
        const existing = latest.get(r.measurementType);
        if (!existing || new Date(r.timestamp) > new Date(existing.timestamp)) {
          latest.set(r.measurementType, r);
        }
      });
      this.latestReadings.set(Array.from(latest.values()));
    } catch (error) {
      console.error('Error loading readings:', error);
    }
  }

  private setupSignalR(): void {
    this.signalRService.onNewReading((reading: Reading) => {
      if (reading.nodeId === this.id()) {
        // Update readings
        this.readings.update(existing => [reading, ...existing]);

        // Update latest readings
        this.latestReadings.update(latest => {
          const newLatest = [...latest];
          const index = newLatest.findIndex(r => r.measurementType === reading.measurementType);
          if (index >= 0) {
            newLatest[index] = reading;
          } else {
            newLatest.push(reading);
          }
          return newLatest;
        });
      }
    });
  }

  getSensorIcon(reading: Reading): string {
    // Use icon from sensor or fallback based on measurement type
    const measurementType = reading.measurementType.toLowerCase();
    if (measurementType.includes('temp')) return 'thermostat';
    if (measurementType.includes('humid') || measurementType.includes('feucht')) return 'water_drop';
    if (measurementType.includes('co2')) return 'air';
    if (measurementType.includes('pressure') || measurementType.includes('druck')) return 'speed';
    if (measurementType.includes('light') || measurementType.includes('licht')) return 'light_mode';
    if (measurementType.includes('pm25') || measurementType.includes('pm10')) return 'cloud';
    return 'sensors';
  }

  getSensorColor(reading: Reading): string {
    // Use color from sensor or fallback based on measurement type
    const measurementType = reading.measurementType.toLowerCase();
    if (measurementType.includes('temp')) return '#ff6b6b';
    if (measurementType.includes('humid') || measurementType.includes('feucht')) return '#4dabf7';
    if (measurementType.includes('co2')) return '#a9e34b';
    if (measurementType.includes('pressure') || measurementType.includes('druck')) return '#9775fa';
    if (measurementType.includes('light') || measurementType.includes('licht')) return '#ffd43b';
    if (measurementType.includes('pm')) return '#868e96';
    return '#495057';
  }

  onTimeRangeChange(): void {
    this.loadReadings();
  }

  goBack(): void {
    this.router.navigate(['/nodes']);
  }
}
