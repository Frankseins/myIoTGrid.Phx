import { Component, OnInit, OnDestroy, inject, signal, computed, ViewChild, TemplateRef, ViewContainerRef, effect, DestroyRef } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { filter } from 'rxjs/operators';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatTabsModule } from '@angular/material/tabs';
import { MatChipsModule } from '@angular/material/chips';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';
import { FormsModule } from '@angular/forms';
import { Overlay, OverlayRef, OverlayModule } from '@angular/cdk/overlay';
import { TemplatePortal } from '@angular/cdk/portal';
import {
  SignalRService,
  DashboardApiService,
  AlertApiService
} from '@myiotgrid/shared/data-access';
import {
  LocationDashboard,
  LocationGroup,
  SensorWidget,
  Alert,
  AlertLevel,
  Reading,
  DashboardFilterOptions
} from '@myiotgrid/shared/models';
import {
  LoadingSpinnerComponent,
  ConnectionStatusComponent,
  EmptyStateComponent,
  SensorWidgetComponent
} from '@myiotgrid/shared/ui';
import { LayoutService } from '@myiotgrid/core/shell';
import { MapDashboardComponent } from '../map-dashboard/map-dashboard.component';
import { AlertBannerComponent } from '../alert-banner/alert-banner.component';

@Component({
  selector: 'myiotgrid-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatSelectModule,
    MatFormFieldModule,
    MatToolbarModule,
    MatTabsModule,
    MatChipsModule,
    MatCheckboxModule,
    MatTooltipModule,
    MatDividerModule,
    OverlayModule,
    LoadingSpinnerComponent,
    ConnectionStatusComponent,
    EmptyStateComponent,
    SensorWidgetComponent,
    AlertBannerComponent,
    MapDashboardComponent
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit, OnDestroy {
  @ViewChild('filterDrawer') filterDrawerTemplate!: TemplateRef<unknown>;

  private readonly router = inject(Router);
  private readonly overlay = inject(Overlay);
  private readonly viewContainerRef = inject(ViewContainerRef);
  private readonly signalRService = inject(SignalRService);
  private readonly dashboardApiService = inject(DashboardApiService);
  private readonly alertApiService = inject(AlertApiService);
  private readonly destroyRef = inject(DestroyRef);
  readonly layout = inject(LayoutService);

  private filterOverlayRef: OverlayRef | null = null;

  constructor() {
    // Subscribe to SignalR signals using effect
    this.setupSignalREffects();
  }

  /**
   * Setup reactive subscriptions to SignalR signals.
   * Using effects instead of manual callbacks.
   */
  private setupSignalREffects(): void {
    // React to new readings
    toObservable(this.signalRService.latestReading)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        filter((reading): reading is Reading => reading !== null)
      )
      .subscribe(reading => {
        this.updateWidgetWithReading(reading);
      });

    // React to new alerts
    toObservable(this.signalRService.alertReceived)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        filter((alert): alert is Alert => alert !== null)
      )
      .subscribe(alert => {
        this.activeAlerts.update(alerts => [alert, ...alerts]);
      });
  }

  // Responsive layout signals
  readonly isMobile = this.layout.isMobile;
  readonly isTablet = this.layout.isTablet;
  readonly isDesktop = this.layout.isDesktop;

  readonly isLoading = signal(true);
  readonly initialLoadDone = signal(false);
  readonly dashboard = signal<LocationDashboard | null>(null);
  readonly activeAlerts = signal<Alert[]>([]);
  readonly filterOptions = signal<DashboardFilterOptions | null>(null);
  readonly selectedTab = signal(0);

  // Filter State - only location and measurement type filters
  // Period filter removed - dashboard always shows latest values
  readonly selectedLocations = signal<string[]>([]);
  readonly selectedMeasurementTypes = signal<string[]>([]);
  isFilterOpen = false;

  readonly criticalAlerts = computed(() =>
    this.activeAlerts().filter(a => a.level === AlertLevel.Critical)
  );

  readonly warningAlerts = computed(() =>
    this.activeAlerts().filter(a => a.level === AlertLevel.Warning)
  );

  readonly heroLocations = computed(() =>
    this.dashboard()?.locations.filter(l => l.isHero) || []
  );

  readonly regularLocations = computed(() =>
    this.dashboard()?.locations.filter(l => !l.isHero) || []
  );

  readonly totalWidgets = computed(() =>
    this.dashboard()?.locations.reduce((sum, l) => sum + l.widgets.length, 0) || 0
  );

  readonly hasActiveFilters = computed(() =>
    this.selectedLocations().length > 0 || this.selectedMeasurementTypes().length > 0
  );

  async ngOnInit(): Promise<void> {
    // Ensure SignalR is connected
    try {
      await this.signalRService.startConnection();
    } catch (error) {
      console.error('Error connecting to SignalR:', error);
    }

    await this.loadFilterOptions();
    await this.loadData();
  }

  ngOnDestroy(): void {
    // Cleanup overlays (SignalR cleanup is handled by takeUntilDestroyed)
    this.closeFilter();
  }

  private async loadFilterOptions(): Promise<void> {
    try {
      const options = await this.dashboardApiService.getFilterOptions().toPromise();
      this.filterOptions.set(options || null);
    } catch (error) {
      console.error('Error loading filter options:', error);
    }
  }

  private async loadData(): Promise<void> {
    this.isLoading.set(true);
    try {
      // Load all nodes with their latest values - no period filtering
      // Period is only used on the widget detail page for historical data
      const filter = {
        locations: this.selectedLocations().length > 0 ? this.selectedLocations() : undefined,
        measurementTypes: this.selectedMeasurementTypes().length > 0 ? this.selectedMeasurementTypes() : undefined
        // No period - always show latest values
      };

      const [dashboard, alerts] = await Promise.all([
        this.dashboardApiService.getFilteredDashboard(filter).toPromise(),
        this.alertApiService.getActive().toPromise()
      ]);

      this.dashboard.set(dashboard || null);
      this.activeAlerts.set(alerts || []);
    } catch (error) {
      console.error('Error loading dashboard data:', error);
    } finally {
      this.isLoading.set(false);
      this.initialLoadDone.set(true);
    }
  }

  private updateWidgetWithReading(reading: Reading): void {
    const currentDashboard = this.dashboard();
    if (!currentDashboard) return;

    const updatedLocations = currentDashboard.locations.map(location => ({
      ...location,
      widgets: location.widgets.map(widget => {
        if (widget.nodeId === reading.nodeId &&
            widget.measurementType === reading.measurementType) {
          return {
            ...widget,
            currentValue: reading.value,
            lastUpdate: reading.timestamp,
            minMax: this.updateMinMax(widget.minMax, reading.value, reading.timestamp),
            isOnline: true
          };
        }
        return widget;
      })
    }));

    this.dashboard.set({ locations: updatedLocations });
  }

  private updateMinMax(minMax: SensorWidget['minMax'], value: number, timestamp: string): SensorWidget['minMax'] {
    if (!minMax) {
      return {
        minValue: value,
        minTimestamp: timestamp,
        maxValue: value,
        maxTimestamp: timestamp
      };
    }

    let updated = { ...minMax };

    if (value < minMax.minValue) {
      updated = { ...updated, minValue: value, minTimestamp: timestamp };
    }
    if (value > minMax.maxValue) {
      updated = { ...updated, maxValue: value, maxTimestamp: timestamp };
    }

    return updated;
  }

  // Filter Drawer
  toggleFilter(): void {
    if (this.isFilterOpen) {
      this.closeFilter();
    } else {
      this.openFilter();
    }
  }

  openFilter(): void {
    if (this.filterOverlayRef) return;

    this.isFilterOpen = true;

    const positionStrategy = this.overlay.position()
      .global()
      .right('0')
      .top('0');

    this.filterOverlayRef = this.overlay.create({
      positionStrategy,
      hasBackdrop: true,
      backdropClass: 'gt-drawer-backdrop',
      panelClass: ['gt-drawer-panel'],
      width: '380px',
      height: '100vh',
      scrollStrategy: this.overlay.scrollStrategies.block()
    });

    this.filterOverlayRef.backdropClick().subscribe(() => this.closeFilter());

    const portal = new TemplatePortal(this.filterDrawerTemplate, this.viewContainerRef);
    this.filterOverlayRef.attach(portal);

    requestAnimationFrame(() => {
      this.filterOverlayRef?.addPanelClass('open');
    });
  }

  closeFilter(): void {
    if (this.filterOverlayRef) {
      this.filterOverlayRef.removePanelClass('open');
      setTimeout(() => {
        this.filterOverlayRef?.dispose();
        this.filterOverlayRef = null;
      }, 200);
    }
    this.isFilterOpen = false;
  }

  // Filter Actions
  isLocationSelected(location: string): boolean {
    return this.selectedLocations().includes(location);
  }

  toggleLocation(location: string): void {
    this.selectedLocations.update(locations => {
      if (locations.includes(location)) {
        return locations.filter(l => l !== location);
      }
      return [...locations, location];
    });
  }

  isMeasurementTypeSelected(type: string): boolean {
    return this.selectedMeasurementTypes().includes(type);
  }

  toggleMeasurementType(type: string): void {
    this.selectedMeasurementTypes.update(types => {
      if (types.includes(type)) {
        return types.filter(t => t !== type);
      }
      return [...types, type];
    });
  }

  clearFilters(): void {
    this.selectedLocations.set([]);
    this.selectedMeasurementTypes.set([]);
  }

  async applyFilters(): Promise<void> {
    this.closeFilter();
    await this.loadData();
  }

  // Other Actions
  async refresh(): Promise<void> {
    await this.loadData();
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

  getLocationIcon(location: LocationGroup): string {
    return location.locationIcon || 'location_on';
  }

  getMeasurementTypeLabel(type: string): string {
    const labels: Record<string, string> = {
      'temperature': 'Temperatur',
      'water_temperature': 'Wassertemperatur',
      'humidity': 'Luftfeuchtigkeit',
      'pressure': 'Luftdruck',
      'illuminance': 'Helligkeit',
      'light': 'Helligkeit',
      'co2': 'CO2',
      'pm25': 'Feinstaub PM2.5',
      'pm10': 'Feinstaub PM10',
      'soil_moisture': 'Bodenfeuchtigkeit',
      'uv': 'UV-Index',
      'wind_speed': 'Windgeschwindigkeit',
      'rainfall': 'Niederschlag',
      'water_level': 'Wasserstand',
      'battery': 'Batterie',
      'rssi': 'Signalstärke'
    };
    return labels[type.toLowerCase()] || type;
  }

  getMeasurementTypeIcon(type: string): string {
    const icons: Record<string, string> = {
      'temperature': 'thermostat',
      'water_temperature': 'thermostat',
      'humidity': 'water_drop',
      'pressure': 'speed',
      'illuminance': 'light_mode',
      'light': 'light_mode',
      'co2': 'air',
      'pm25': 'blur_on',
      'pm10': 'blur_on',
      'soil_moisture': 'grass',
      'uv': 'wb_sunny',
      'wind_speed': 'air',
      'rainfall': 'water',
      'water_level': 'waves',
      'battery': 'battery_full',
      'rssi': 'signal_cellular_alt'
    };
    return icons[type.toLowerCase()] || 'sensors';
  }

  navigateToNodes(): void {
    this.router.navigate(['/nodes']);
  }

  onWidgetClick(widget: SensorWidget): void {
    if (widget.assignmentId) {
      this.router.navigate(['/dashboard/widget', widget.nodeId, widget.assignmentId, widget.measurementType]);
    }
  }
}
