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
import { HubApiService, NodeApiService, ReadingApiService, SensorTypeApiService, SignalRService } from '@myiotgrid/shared/data-access';
import { Hub, Node, Reading } from '@myiotgrid/shared/models';
import { LoadingSpinnerComponent, EmptyStateComponent, ConfirmDialogService } from '@myiotgrid/shared/ui';
import { RelativeTimePipe } from '@myiotgrid/shared/utils';

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
    RelativeTimePipe
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
  private readonly sensorTypeApiService = inject(SensorTypeApiService);
  private readonly signalRService = inject(SignalRService);
  private readonly confirmDialogService = inject(ConfirmDialogService);
  private readonly snackBar = inject(MatSnackBar);
  private filterOverlayRef: OverlayRef | null = null;

  readonly isLoading = signal(true);
  readonly initialLoadDone = signal(false);
  readonly hubs = signal<Hub[]>([]);
  readonly nodes = signal<Node[]>([]);
  readonly latestReadings = signal<Map<string, Reading[]>>(new Map());
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
    this.setupSignalR();
  }

  ngOnDestroy(): void {
    // SignalR Event-Handler entfernen
    this.signalRService.off('NewReading');
    this.signalRService.off('NodeStatusChanged');
    this.signalRService.off('NodeRegistered');
  }

  private setupSignalR(): void {
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
  }

  private updateReading(reading: Reading): void {
    this.latestReadings.update(readingMap => {
      const newMap = new Map(readingMap);
      const nodeReadings = newMap.get(reading.nodeId) || [];

      // Existierendes Reading für diesen SensorType ersetzen oder hinzufügen
      const existingIndex = nodeReadings.findIndex(r => r.sensorTypeId === reading.sensorTypeId);
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
      // SensorTypes zuerst laden (für Icons/Farben)
      await this.sensorTypeApiService.getAll().toPromise();

      // Dann parallel: Hubs, Nodes und Readings
      const [hubs, nodes, readings] = await Promise.all([
        this.hubApiService.getAll().toPromise(),
        this.nodeApiService.getAll().toPromise(),
        this.readingApiService.getLatest().toPromise()
      ]);

      this.hubs.set(hubs || []);
      this.nodes.set(nodes || []);

      if (readings) {
        const readingMap = new Map<string, Reading[]>();
        readings.forEach(r => {
          const existing = readingMap.get(r.nodeId) || [];
          existing.push(r);
          readingMap.set(r.nodeId, existing);
        });
        this.latestReadings.set(readingMap);
      }
    } catch (error) {
      console.error('Error loading data:', error);
    } finally {
      this.isLoading.set(false);
      this.initialLoadDone.set(true);
    }
  }

  getLatestReadings(nodeId: string): Reading[] {
    return this.latestReadings().get(nodeId) || [];
  }

  getSensorReading(nodeId: string, sensorTypeId: string): Reading | undefined {
    const readings = this.getLatestReadings(nodeId);
    return readings.find(r => r.sensorTypeId === sensorTypeId);
  }

  getHubName(hubId: string): string {
    const hub = this.hubs().find(h => h.id === hubId);
    return hub?.name || 'Unbekannt';
  }

  getNodeIcon(node: Node): string {
    const protocol = node.protocol;
    switch (protocol) {
      case 1: return 'wifi';
      case 2: return 'cell_tower';
      default: return 'router';
    }
  }

  getSensorIcon(typeId: string): string {
    return this.sensorTypeApiService.getIcon(typeId);
  }

  getSensorColor(typeId: string): string {
    return this.sensorTypeApiService.getColor(typeId);
  }

  getSensorTypeName(typeId: string): string {
    return this.sensorTypeApiService.getDisplayName(typeId);
  }

  getSensorUnit(typeId: string): string {
    return this.sensorTypeApiService.getUnit(typeId);
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
      .top('0')
      .bottom('0');

    this.filterOverlayRef = this.overlay.create({
      positionStrategy,
      hasBackdrop: true,
      backdropClass: 'gt-drawer-backdrop',
      panelClass: ['gt-drawer-panel', 'open'],
      width: '380px'
    });

    this.filterOverlayRef.backdropClick().subscribe(() => this.closeFilter());

    const portal = new TemplatePortal(this.filterDrawerTemplate, this.viewContainerRef);
    this.filterOverlayRef.attach(portal);
  }

  closeFilter(): void {
    if (this.filterOverlayRef) {
      this.filterOverlayRef.dispose();
      this.filterOverlayRef = null;
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

  onCardClick(node: Node): void {
    this.router.navigate(['/nodes', node.id]);
  }

  onEdit(node: Node, event: Event): void {
    event.stopPropagation();
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
