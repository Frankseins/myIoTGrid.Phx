import { Component, OnInit, OnDestroy, inject, signal, computed } from '@angular/core';
import { CommonModule, DatePipe, DecimalPipe } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTabsModule } from '@angular/material/tabs';
import { Subject, takeUntil } from 'rxjs';

import { ChartApiService, SignalRService } from '@myiotgrid/shared/data-access';
import {
  ChartData,
  ChartInterval,
  ReadingsList,
  ChartIntervalLabels
} from '@myiotgrid/shared/models';
import {
  LineChartComponent,
  IntervalSelectorComponent,
  StatsCardsComponent,
  ReadingsTableComponent
} from '@myiotgrid/shared/ui';

@Component({
  selector: 'myiotgrid-widget-detail',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatTabsModule,
    DatePipe,
    DecimalPipe,
    LineChartComponent,
    IntervalSelectorComponent,
    StatsCardsComponent,
    ReadingsTableComponent,
  ],
  templateUrl: './widget-detail.component.html',
  styleUrl: './widget-detail.component.scss'
})
export class WidgetDetailComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly chartApi = inject(ChartApiService);
  private readonly signalR = inject(SignalRService);
  private readonly destroy$ = new Subject<void>();

  // Route params
  nodeId = '';
  assignmentId = '';
  measurementType = '';

  // State signals
  chartData = signal<ChartData | null>(null);
  readingsList = signal<ReadingsList | null>(null);
  selectedInterval = signal<ChartInterval>(ChartInterval.OneDay);
  chartLoading = signal(false);
  tableLoading = signal(false);
  error = signal<string | null>(null);

  // Computed values
  readonly currentValue = computed(() => this.chartData()?.currentValue ?? 0);
  readonly unit = computed(() => this.chartData()?.unit ?? '');
  readonly color = computed(() => this.chartData()?.color ?? '#1976d2');
  readonly sensorName = computed(() => this.chartData()?.sensorName ?? '');
  readonly nodeName = computed(() => this.chartData()?.nodeName ?? '');
  readonly locationName = computed(() => this.chartData()?.locationName ?? '');
  readonly measurementLabel = computed(() => this.getMeasurementLabel(this.measurementType));
  readonly lastUpdate = computed(() => this.chartData()?.lastUpdate ?? '');
  readonly intervalLabel = computed(() => ChartIntervalLabels[this.selectedInterval()]);

  ngOnInit(): void {
    this.route.params.pipe(takeUntil(this.destroy$)).subscribe(params => {
      this.nodeId = params['nodeId'];
      this.assignmentId = params['assignmentId'];
      this.measurementType = params['measurementType'];
      this.loadChartData();
      this.loadReadingsList();
    });

    // SignalR live updates
    this.setupSignalR();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onIntervalChange(interval: ChartInterval): void {
    this.selectedInterval.set(interval);
    this.loadChartData();
  }

  onPageChange(event: { page: number; pageSize: number }): void {
    this.loadReadingsList(event.page, event.pageSize);
  }

  onExportCsv(): void {
    this.chartApi.exportToCsv(
      this.nodeId,
      this.assignmentId,
      this.measurementType
    ).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `${this.measurementType}_${new Date().toISOString().slice(0, 10)}.csv`;
        a.click();
        window.URL.revokeObjectURL(url);
      },
      error: (err) => console.error('CSV Export failed:', err)
    });
  }

  goBack(): void {
    this.router.navigate(['/dashboard']);
  }

  loadChartData(): void {
    this.chartLoading.set(true);
    this.error.set(null);

    this.chartApi.getChartData(
      this.nodeId,
      this.assignmentId,
      this.measurementType,
      this.selectedInterval()
    ).pipe(takeUntil(this.destroy$)).subscribe({
      next: (data) => {
        this.chartData.set(data);
        this.chartLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load chart data:', err);
        this.error.set('Chartdaten konnten nicht geladen werden');
        this.chartLoading.set(false);
      }
    });
  }

  private loadReadingsList(page = 1, pageSize = 20): void {
    this.tableLoading.set(true);

    this.chartApi.getReadingsList(
      this.nodeId,
      this.assignmentId,
      this.measurementType,
      { page, pageSize }
    ).pipe(takeUntil(this.destroy$)).subscribe({
      next: (data) => {
        this.readingsList.set(data);
        this.tableLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load readings list:', err);
        this.tableLoading.set(false);
      }
    });
  }

  private async setupSignalR(): Promise<void> {
    try {
      await this.signalR.startConnection();

      this.signalR.onNewReading((reading) => {
        // Check if this reading is for our widget
        if (reading.nodeId === this.nodeId &&
            reading.assignmentId === this.assignmentId &&
            reading.measurementType.toLowerCase() === this.measurementType.toLowerCase()) {
          // Reload data to get updated stats
          this.loadChartData();
          this.loadReadingsList();
        }
      });
    } catch (error) {
      console.error('SignalR connection failed:', error);
    }
  }

  private getMeasurementLabel(type: string): string {
    const labels: Record<string, string> = {
      'temperature': 'Temperatur',
      'water_temperature': 'Wassertemperatur',
      'humidity': 'Luftfeuchtigkeit',
      'pressure': 'Luftdruck',
      'co2': 'CO2',
      'pm25': 'Feinstaub PM2.5',
      'pm10': 'Feinstaub PM10',
      'soil_moisture': 'Bodenfeuchtigkeit',
      'light': 'Helligkeit',
      'illuminance': 'Beleuchtungsstärke',
      'uv': 'UV-Index',
      'wind_speed': 'Windgeschwindigkeit',
      'rainfall': 'Niederschlag',
      'water_level': 'Wasserstand',
      'battery': 'Batterie',
      'rssi': 'Signalstärke'
    };
    return labels[type.toLowerCase()] || type;
  }

  getMeasurementIcon(): string {
    const icons: Record<string, string> = {
      'temperature': 'thermostat',
      'water_temperature': 'water',
      'humidity': 'water_drop',
      'pressure': 'speed',
      'co2': 'air',
      'pm25': 'air',
      'pm10': 'air',
      'soil_moisture': 'grass',
      'light': 'light_mode',
      'illuminance': 'light_mode',
      'uv': 'wb_sunny',
      'wind_speed': 'air',
      'rainfall': 'water',
      'water_level': 'waves',
      'battery': 'battery_full',
      'rssi': 'signal_cellular_alt'
    };
    return icons[this.measurementType.toLowerCase()] || 'sensors';
  }
}
