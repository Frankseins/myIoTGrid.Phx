import { Component, OnInit, OnDestroy, inject, signal, ViewChild, ElementRef, TemplateRef, ViewContainerRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatMenuModule } from '@angular/material/menu';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatInputModule } from '@angular/material/input';
import { MatDividerModule } from '@angular/material/divider';
import { FormsModule } from '@angular/forms';
import { Overlay, OverlayRef, OverlayModule } from '@angular/cdk/overlay';
import { TemplatePortal } from '@angular/cdk/portal';
import { forkJoin } from 'rxjs';
import { HubApiService, NodeApiService, ReadingApiService, SignalRService } from '@myiotgrid/shared/data-access';
import { Hub, Node, Reading, NodeProvisioningStatus, NodeSensorsLatest, Protocol } from '@myiotgrid/shared/models';
import { LoadingSpinnerComponent, EmptyStateComponent, ConfirmDialogService, NodeCardComponent } from '@myiotgrid/shared/ui';

type SortField = 'name' | 'nodeId' | 'lastSeen' | 'createdAt';
type SortDirection = 'asc' | 'desc';

@Component({
  selector: 'myiotgrid-node-list',
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
    MatSnackBarModule,
    MatMenuModule,
    MatTooltipModule,
    MatToolbarModule,
    MatInputModule,
    MatDividerModule,
    FormsModule,
    OverlayModule,
    LoadingSpinnerComponent,
    EmptyStateComponent,
    NodeCardComponent
  ],
  templateUrl: './node-list.component.html',
  styleUrl: './node-list.component.scss'
})
export class NodeListComponent implements OnInit, OnDestroy {
  @ViewChild('searchInput') searchInput!: ElementRef<HTMLInputElement>;
  @ViewChild('filterDrawer') filterDrawerTemplate!: TemplateRef<unknown>;

  private readonly router = inject(Router);
  private readonly overlay = inject(Overlay);
  private readonly viewContainerRef = inject(ViewContainerRef);
  private readonly hubApiService = inject(HubApiService);
  private readonly nodeApiService = inject(NodeApiService);
  private readonly readingApiService = inject(ReadingApiService);
  private readonly signalRService = inject(SignalRService);
  private readonly confirmDialogService = inject(ConfirmDialogService);
  private readonly snackBar = inject(MatSnackBar);
  private filterOverlayRef: OverlayRef | null = null;

  readonly isLoading = signal(true);
  readonly initialLoadDone = signal(false);
  readonly hubs = signal<Hub[]>([]);
  readonly nodes = signal<Node[]>([]);
  readonly latestReadings = signal<Map<string, Reading[]>>(new Map());
  readonly sensorsLatestMap = signal<Map<string, NodeSensorsLatest>>(new Map());
  readonly isDeleting = signal<string | null>(null);
  readonly expandedNodes = signal<Set<string>>(new Set());
  readonly lastUpdated = signal<Map<string, string>>(new Map());

  // Toolbar State
  isSearchOpen = false;
  isFilterOpen = false;
  searchTerm = '';
  filterStatus: 'all' | 'online' | 'offline' = 'all';
  sortField: SortField = 'name';
  sortDirection: SortDirection = 'asc';

  readonly sortOptions: { value: SortField; label: string }[] = [
    { value: 'name', label: 'Name' },
    { value: 'nodeId', label: 'Node ID' },
    { value: 'lastSeen', label: 'Zuletzt gesehen' },
    { value: 'createdAt', label: 'Erstellt' }
  ];

  get filteredAndSortedNodes(): Node[] {
    let result = this.nodes();

    // Filter by status
    if (this.filterStatus === 'online') {
      result = result.filter(n => n.isOnline);
    } else if (this.filterStatus === 'offline') {
      result = result.filter(n => !n.isOnline);
    }

    // Filter by search term
    if (this.searchTerm.trim().length >= 2) {
      const term = this.searchTerm.toLowerCase();
      result = result.filter(n =>
        n.name.toLowerCase().includes(term) ||
        n.nodeId.toLowerCase().includes(term) ||
        (n.location?.name?.toLowerCase().includes(term) ?? false)
      );
    }

    // Sort
    result = [...result].sort((a, b) => {
      let comparison = 0;
      switch (this.sortField) {
        case 'name':
          comparison = (a.location?.name || a.name).localeCompare(b.location?.name || b.name);
          break;
        case 'nodeId':
          comparison = a.nodeId.localeCompare(b.nodeId);
          break;
        case 'lastSeen':
          const aDate = a.lastSeen ? new Date(a.lastSeen).getTime() : 0;
          const bDate = b.lastSeen ? new Date(b.lastSeen).getTime() : 0;
          comparison = bDate - aDate; // Newest first by default
          break;
        case 'createdAt':
          comparison = new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
          break;
      }
      return this.sortDirection === 'asc' ? comparison : -comparison;
    });

    return result;
  }

  get hasActiveFilters(): boolean {
    return this.filterStatus !== 'all';
  }

  async ngOnInit(): Promise<void> {
    await this.loadData();
    await this.setupSignalR();
  }

  ngOnDestroy(): void {
    // SignalR Event-Handler entfernen
    this.signalRService.off('NewReading');
    this.signalRService.off('NodeStatusChanged');
    this.signalRService.off('NodeRegistered');
  }

  private async setupSignalR(): Promise<void> {
    try {
      // SignalR-Verbindung starten (falls noch nicht verbunden)
      await this.signalRService.startConnection();

      // Neue Readings empfangen
      this.signalRService.onNewReading((reading: Reading) => {
        this.updateReading(reading);
      });

      // Node-Status-Änderungen empfangen
      this.signalRService.onNodeStatusChanged((updatedNode: Node) => {
        this.nodes.update(nodes =>
          nodes.map(n => n.id === updatedNode.id ? { ...n, ...updatedNode } : n)
        );
      });

      // Neue Node-Registrierungen empfangen
      this.signalRService.onNodeRegistered((newNode: Node) => {
        this.nodes.update(nodes => {
          // Prüfen ob Node bereits existiert
          if (nodes.some(n => n.id === newNode.id)) {
            return nodes.map(n => n.id === newNode.id ? newNode : n);
          }
          return [...nodes, newNode];
        });
      });
    } catch (error) {
      console.error('Error setting up SignalR:', error);
    }
  }

  private updateReading(reading: Reading): void {
    this.latestReadings.update(readingMap => {
      const newMap = new Map(readingMap);
      const nodeReadings = newMap.get(reading.nodeId) || [];

      // Existierendes Reading für diesen Sensor ersetzen oder hinzufügen
      const existingIndex = nodeReadings.findIndex(r => r.sensorId === reading.sensorId);
      if (existingIndex >= 0) {
        nodeReadings[existingIndex] = reading;
      } else {
        nodeReadings.push(reading);
      }

      newMap.set(reading.nodeId, [...nodeReadings]);
      return newMap;
    });

    // Last Updated für diesen Node aktualisieren
    this.lastUpdated.update(map => {
      const newMap = new Map(map);
      newMap.set(reading.nodeId, reading.timestamp);
      return newMap;
    });

    // Node lastSeen aktualisieren
    this.nodes.update(nodes =>
      nodes.map(n => {
        if (n.id === reading.nodeId) {
          return { ...n, lastSeen: reading.timestamp, isOnline: true };
        }
        return n;
      })
    );
  }

  toggleSensorsExpanded(nodeId: string, event: Event): void {
    event.stopPropagation();
    this.expandedNodes.update(set => {
      const newSet = new Set(set);
      if (newSet.has(nodeId)) {
        newSet.delete(nodeId);
      } else {
        newSet.add(nodeId);
      }
      return newSet;
    });
  }

  isSensorsExpanded(nodeId: string): boolean {
    return this.expandedNodes().has(nodeId);
  }

  getLastUpdated(nodeId: string): string | undefined {
    return this.lastUpdated().get(nodeId);
  }

  private async loadData(): Promise<void> {
    this.isLoading.set(true);
    try {
      // Parallel: Hubs und Nodes
      const [hubs, nodes] = await Promise.all([
        this.hubApiService.getAll().toPromise(),
        this.nodeApiService.getAll().toPromise()
      ]);

      this.hubs.set(hubs || []);
      this.nodes.set(nodes || []);

      // Load sensors latest for each node (parallel)
      if (nodes && nodes.length > 0) {
        this.loadSensorsLatestForNodes(nodes);
      }
    } catch (error) {
      console.error('Error loading data:', error);
    } finally {
      this.isLoading.set(false);
      this.initialLoadDone.set(true);
    }
  }

  private loadSensorsLatestForNodes(nodes: Node[]): void {
    const requests = nodes.reduce((acc, node) => {
      acc[node.id] = this.nodeApiService.getSensorsLatest(node.id);
      return acc;
    }, {} as Record<string, ReturnType<typeof this.nodeApiService.getSensorsLatest>>);

    forkJoin(requests).subscribe({
      next: (results) => {
        const newMap = new Map<string, NodeSensorsLatest>();
        Object.entries(results).forEach(([nodeId, sensorsLatest]) => {
          newMap.set(nodeId, sensorsLatest);
        });
        this.sensorsLatestMap.set(newMap);
      },
      error: (err) => {
        console.error('Error loading sensors latest:', err);
      }
    });
  }

  getSensorsLatest(nodeId: string): NodeSensorsLatest | undefined {
    return this.sensorsLatestMap().get(nodeId);
  }

  getRelativeTime(timestamp: string): string {
    if (!timestamp) return '';
    const date = new Date(timestamp);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);

    if (diffMins < 1) return 'gerade eben';
    if (diffMins < 60) return `vor ${diffMins} Min.`;
    const diffHours = Math.floor(diffMins / 60);
    if (diffHours < 24) return `vor ${diffHours} Std.`;
    const diffDays = Math.floor(diffHours / 24);
    return `vor ${diffDays} Tag${diffDays > 1 ? 'en' : ''}`;
  }

  getLatestTimestamp(sensorsLatest: NodeSensorsLatest): string | null {
    let latest: string | null = null;
    for (const sensor of sensorsLatest.sensors) {
      for (const m of sensor.measurements) {
        if (!latest || new Date(m.timestamp) > new Date(latest)) {
          latest = m.timestamp;
        }
      }
    }
    return latest;
  }

  getNodeTimestamp(node: Node): string | null {
    const sensorsLatest = this.getSensorsLatest(node.id);
    if (sensorsLatest) {
      const sensorTimestamp = this.getLatestTimestamp(sensorsLatest);
      if (sensorTimestamp) {
        return sensorTimestamp;
      }
    }
    return node.lastSeen || null;
  }

  getLatestReadings(nodeId: string): Reading[] {
    return this.latestReadings().get(nodeId) || [];
  }

  getSensorReading(nodeId: string, sensorId: string): Reading | undefined {
    const readings = this.getLatestReadings(nodeId);
    return readings.find(r => r.sensorId === sensorId);
  }

  getHubName(hubId: string): string {
    const hub = this.hubs().find(h => h.id === hubId);
    return hub?.name || 'Unbekannt';
  }

  getNodeIcon(node: Node): string {
    const protocol = node.protocol;
    switch (protocol) {
      case Protocol.WLAN: return 'wifi';
      case Protocol.LoRaWAN: return 'cell_tower';
      default: return 'router';
    }
  }

  /**
   * Checks if node needs configuration (status != Configured)
   */
  isUnconfigured(node: Node): boolean {
    return node.status !== NodeProvisioningStatus.Configured;
  }

  /**
   * Returns status label for unconfigured nodes
   */
  getStatusLabel(node: Node): string {
    switch (node.status) {
      case NodeProvisioningStatus.Unconfigured: return 'Nicht konfiguriert';
      case NodeProvisioningStatus.Pairing: return 'Pairing läuft...';
      case NodeProvisioningStatus.Error: return 'Fehler';
      default: return '';
    }
  }

  /**
   * Returns status icon for unconfigured nodes
   */
  getStatusIcon(node: Node): string {
    switch (node.status) {
      case NodeProvisioningStatus.Unconfigured: return 'settings';
      case NodeProvisioningStatus.Pairing: return 'bluetooth_searching';
      case NodeProvisioningStatus.Error: return 'error';
      default: return 'check_circle';
    }
  }

  getSensorIcon(sensor: { icon?: string }): string {
    return sensor.icon || 'sensors';
  }

  getSensorColor(sensor: { color?: string }): string {
    return sensor.color || '#607d8b';
  }

  /**
   * Get icon for a measurement type when no sensor is assigned
   */
  getReadingIcon(measurementType: string): string {
    const iconMap: Record<string, string> = {
      'temperature': 'thermostat',
      'humidity': 'water_drop',
      'pressure': 'speed',
      'co2': 'co2',
      'pm25': 'air',
      'pm10': 'air',
      'light': 'light_mode',
      'lux': 'light_mode',
      'bh1750': 'light_mode',
      'ds18b20': 'thermostat',
      'dht22': 'thermostat',
      'bme280': 'thermostat',
      'bme680': 'air',
      'soil_moisture': 'grass',
      'battery': 'battery_full',
      'rssi': 'signal_cellular_alt'
    };
    return iconMap[measurementType.toLowerCase()] || 'sensors';
  }

  /**
   * Get color for a measurement type when no sensor is assigned
   */
  getReadingColor(measurementType: string): string {
    const colorMap: Record<string, string> = {
      'temperature': '#ff5722',
      'humidity': '#2196f3',
      'pressure': '#9c27b0',
      'co2': '#4caf50',
      'pm25': '#607d8b',
      'pm10': '#607d8b',
      'light': '#ffeb3b',
      'lux': '#ffeb3b',
      'bh1750': '#ffeb3b',
      'ds18b20': '#ff5722',
      'dht22': '#ff5722',
      'bme280': '#ff5722',
      'bme680': '#4caf50',
      'soil_moisture': '#795548',
      'battery': '#8bc34a',
      'rssi': '#00bcd4'
    };
    return colorMap[measurementType.toLowerCase()] || '#607d8b';
  }

  /**
   * Get unit for a measurement type when no unit is provided
   */
  getReadingUnit(measurementType: string): string {
    const unitMap: Record<string, string> = {
      'temperature': '°C',
      'humidity': '%',
      'pressure': 'hPa',
      'co2': 'ppm',
      'pm25': 'µg/m³',
      'pm10': 'µg/m³',
      'light': 'lux',
      'lux': 'lux',
      'bh1750': 'lux',
      'ds18b20': '°C',
      'dht22': '°C',
      'bme280': '°C',
      'bme680': '',
      'soil_moisture': '%',
      'battery': '%',
      'rssi': 'dBm'
    };
    return unitMap[measurementType.toLowerCase()] || '';
  }

  // Toolbar Actions
  toggleSearch(): void {
    this.isSearchOpen = !this.isSearchOpen;
    if (this.isSearchOpen) {
      setTimeout(() => this.searchInput?.nativeElement?.focus(), 100);
    } else {
      this.searchTerm = '';
    }
  }

  closeSearch(): void {
    this.isSearchOpen = false;
    this.searchTerm = '';
  }

  clearSearch(): void {
    this.searchTerm = '';
    this.searchInput?.nativeElement?.focus();
  }

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

    // Trigger animation after attach
    requestAnimationFrame(() => {
      this.filterOverlayRef?.addPanelClass('open');
    });
  }

  closeFilter(): void {
    if (this.filterOverlayRef) {
      this.filterOverlayRef.removePanelClass('open');
      // Wait for animation to complete before disposing
      setTimeout(() => {
        this.filterOverlayRef?.dispose();
        this.filterOverlayRef = null;
      }, 200);
    }
    this.isFilterOpen = false;
  }

  clearFilters(): void {
    this.filterStatus = 'all';
    this.sortField = 'name';
    this.sortDirection = 'asc';
  }

  onSearchBlur(): void {
    if (!this.searchTerm) {
      this.isSearchOpen = false;
    }
  }

  onRefresh(): void {
    this.loadData();
  }

  toggleSortDirection(): void {
    this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
  }

  onCreate(): void {
    this.router.navigate(['/nodes', 'new']);
  }

  onStartWizard(): void {
    this.router.navigate(['/setup']);
  }

  onCardClick(node: Node): void {
    this.router.navigate(['/nodes', node.id]);
  }

  onEdit(node: Node, event: Event): void {
    event.stopPropagation();
    this.router.navigate(['/nodes', node.id, 'edit']);
  }

  onConfigure(node: Node): void {
    this.router.navigate(['/nodes', node.id, 'edit']);
  }

  onDelete(node: Node, event: Event): void {
    event.stopPropagation();
    const nodeName = node.location?.name || node.name || node.nodeId;

    this.confirmDialogService.confirmDelete(nodeName, 'Node').subscribe(confirmed => {
      if (confirmed) {
        this.isDeleting.set(node.id);
        this.nodeApiService.remove(node.id).subscribe({
          next: () => {
            this.snackBar.open(`Node "${nodeName}" wurde gelöscht`, 'OK', { duration: 3000 });
            this.nodes.update(nodes => nodes.filter(n => n.id !== node.id));
            this.isDeleting.set(null);
          },
          error: (error) => {
            console.error('Error deleting node:', error);
            this.snackBar.open('Fehler beim Löschen des Nodes', 'Schließen', {
              duration: 5000,
              panelClass: ['snackbar-error']
            });
            this.isDeleting.set(null);
          }
        });
      }
    });
  }
}
