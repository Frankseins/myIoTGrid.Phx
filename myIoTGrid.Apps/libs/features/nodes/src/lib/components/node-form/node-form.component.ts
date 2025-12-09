import { Component, OnInit, OnDestroy, inject, signal, computed, ViewChild, TemplateRef, ViewContainerRef, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDividerModule } from '@angular/material/divider';
import { MatChipsModule } from '@angular/material/chips';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Overlay, OverlayRef, OverlayModule } from '@angular/cdk/overlay';
import { TemplatePortal } from '@angular/cdk/portal';
import { firstValueFrom } from 'rxjs';
import { NodeApiService, HubApiService, NodeSensorAssignmentApiService, SensorApiService, SignalRService, ReadingApiService, NodeDebugApiService } from '@myiotgrid/shared/data-access';
import {
  Node, CreateNodeDto, UpdateNodeDto, Hub, Protocol, StorageMode,
  NodeSensorAssignment, CreateNodeSensorAssignmentDto, UpdateNodeSensorAssignmentDto,
  Sensor, NodeSensorsLatest, Reading, QueryParams, NodeDebugConfiguration
} from '@myiotgrid/shared/models';
import { LoadingSpinnerComponent, GenericListComponent, GenericListColumn, ListLazyEvent, ListColumnTemplateDirective } from '@myiotgrid/shared/ui';
import { ConfirmDialogComponent } from '@myiotgrid/shared/ui';
import { DeleteReadingsDrawerComponent } from '../delete-readings-drawer/delete-readings-drawer.component';
import { NodeDebugControlComponent } from '../node-debug-control/node-debug-control.component';
import { LiveLogViewerComponent } from '../live-log-viewer/live-log-viewer.component';
import { HardwareStatusWidgetComponent } from '../hardware-status-widget/hardware-status-widget.component';

type FormMode = 'view' | 'edit' | 'create';

@Component({
  selector: 'myiotgrid-node-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatSnackBarModule,
    MatDividerModule,
    MatChipsModule,
    MatExpansionModule,
    MatTooltipModule,
    MatDialogModule,
    MatSlideToggleModule,
    MatProgressSpinnerModule,
    OverlayModule,
    LoadingSpinnerComponent,
    GenericListComponent,
    ListColumnTemplateDirective,
    DeleteReadingsDrawerComponent,
    NodeDebugControlComponent,
    LiveLogViewerComponent,
    HardwareStatusWidgetComponent
  ],
  templateUrl: './node-form.component.html',
  styleUrl: './node-form.component.scss'
})
export class NodeFormComponent implements OnInit, OnDestroy {
  @ViewChild('assignmentDrawer') assignmentDrawerTemplate!: TemplateRef<unknown>;
  @ViewChild('deleteReadingsDrawer') deleteReadingsDrawerTemplate!: TemplateRef<unknown>;
  @ViewChild('deleteNodeDrawer') deleteNodeDrawerTemplate!: TemplateRef<unknown>;
  @ViewChild('debugControl') debugControlComponent?: NodeDebugControlComponent;

  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly overlay = inject(Overlay);
  private readonly viewContainerRef = inject(ViewContainerRef);
  private readonly nodeApiService = inject(NodeApiService);
  private readonly hubApiService = inject(HubApiService);
  private readonly assignmentApiService = inject(NodeSensorAssignmentApiService);
  private readonly sensorApiService = inject(SensorApiService);
  private readonly signalRService = inject(SignalRService);
  private readonly readingApiService = inject(ReadingApiService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialog = inject(MatDialog);

  private assignmentOverlayRef: OverlayRef | null = null;
  private deleteOverlayRef: OverlayRef | null = null;
  private deleteNodeOverlayRef: OverlayRef | null = null;
  private timestampInterval: ReturnType<typeof setInterval> | null = null;
  private currentNodeId: string | null = null; // For SignalR group cleanup

  // Delete Node Drawer
  deleteConfirmInput = '';

  // Effect to sync isSimulation FormControl disabled state with view mode
  private readonly modeEffect = effect(() => {
    const viewMode = this.isViewMode();
    const isSimulationControl = this.form?.get('isSimulation');
    if (isSimulationControl) {
      if (viewMode) {
        isSimulationControl.disable({ emitEvent: false });
      } else {
        isSimulationControl.enable({ emitEvent: false });
      }
    }
  });

  readonly isLoading = signal(true);
  readonly isSaving = signal(false);
  readonly isDeleting = signal(false);
  readonly mode = signal<FormMode>('view');
  readonly node = signal<Node | null>(null);
  readonly hubs = signal<Hub[]>([]);

  // Sensor Assignment
  readonly assignments = signal<NodeSensorAssignment[]>([]);
  readonly availableSensors = signal<Sensor[]>([]);
  readonly isLoadingAssignments = signal(false);
  readonly isAssignmentDrawerOpen = signal(false);
  readonly editingAssignment = signal<NodeSensorAssignment | null>(null);
  assignmentForm!: FormGroup;

  // Sensor Latest Readings (US-8.5.2)
  readonly sensorsLatest = signal<NodeSensorsLatest | null>(null);
  readonly isLoadingSensorsLatest = signal(false);
  readonly timestampRefresh = signal(0); // Trigger for relative time refresh

  // Reading History (Generic Table)
  readonly readingHistory = signal<Reading[]>([]);
  readonly isLoadingHistory = signal(false);
  readonly historyTotalRecords = signal(0);
  readonly historyColumns: GenericListColumn[] = [
    { field: 'timestamp', header: 'Zeitstempel', sortable: true, type: 'date' },
    { field: 'sensorName', header: 'Sensor', sortable: true },
    { field: 'displayName', header: 'Messtyp', sortable: true },
    { field: 'value', header: 'Wert', sortable: true, type: 'number' }
  ];
  // Field mapping for sorting: frontend camelCase -> backend PascalCase
  private readonly historyFieldMapping: Record<string, string> = {
    timestamp: 'Timestamp',
    sensorName: 'SensorName',
    displayName: 'DisplayName',
    measurementType: 'MeasurementType',
    value: 'Value'
  };
  // Current filters for reading history
  private historyFilters: Record<string, unknown> = {};
  // Last lazy load event for refresh after filter change
  private lastHistoryEvent: ListLazyEvent | null = null;

  readonly isViewMode = computed(() => this.mode() === 'view');
  readonly isEditMode = computed(() => this.mode() === 'edit');
  readonly isCreateMode = computed(() => this.mode() === 'create');

  readonly pageTitle = computed(() => {
    switch (this.mode()) {
      case 'create': return 'Neuer Node';
      case 'edit': return 'Node bearbeiten';
      default: return 'Node Details';
    }
  });

  form!: FormGroup;

  readonly protocols: { value: Protocol; label: string }[] = [
    { value: Protocol.WLAN, label: 'WLAN' },
    { value: Protocol.LoRaWAN, label: 'LoRaWAN' }
  ];

  // Storage modes for offline storage (Sprint OS-01)
  readonly storageModes: { value: StorageMode; label: string; description: string }[] = [
    { value: StorageMode.RemoteOnly, label: 'Nur Remote', description: 'Messwerte werden nur an den Hub gesendet' },
    { value: StorageMode.LocalAndRemote, label: 'Lokal + Remote', description: 'Lokale Speicherung und Senden an Hub' },
    { value: StorageMode.LocalOnly, label: 'Nur Lokal', description: 'Nur lokale Speicherung, kein Senden' },
    { value: StorageMode.LocalAutoSync, label: 'Auto-Sync', description: 'Lokal speichern, bei WiFi automatisch synchronisieren' }
  ];

  // Node types (Sensorart) - Placeholder for future backend enum
  readonly nodeTypes: { value: string; label: string }[] = [
    { value: 'esp32', label: 'ESP32' },
    { value: 'softsensor', label: 'Softsensor' },
    { value: 'raspberry', label: 'Raspberry Pi' },
    { value: 'other', label: 'Sonstige' }
  ];

  ngOnInit(): void {
    this.initForm();
    this.initAssignmentForm();
    this.loadHubs();

    const id = this.route.snapshot.paramMap.get('id');
    const url = this.route.snapshot.url.map(s => s.path).join('/');

    // Check if this is the 'new' route (no :id parameter)
    if (!id || url.includes('new')) {
      this.mode.set('create');
      this.isLoading.set(false);
    } else {
      // Check if editing (route ends with /edit)
      const isEditRoute = url.endsWith('edit');
      this.mode.set(isEditRoute ? 'edit' : 'view');
      this.loadNode(id);
      this.loadAssignments(id);
      this.loadAvailableSensors();
      this.loadSensorsLatest(id);
      // Reading history is now loaded via Generic Table's lazyLoad event
      this.setupSignalR(id);
    }

    // Auto-refresh timestamps every 30 seconds
    this.timestampInterval = setInterval(() => {
      this.timestampRefresh.update(v => v + 1);
    }, 30000);
  }

  ngOnDestroy(): void {
    if (this.timestampInterval) {
      clearInterval(this.timestampInterval);
    }
    // Cleanup SignalR: leave node group and unsubscribe
    if (this.currentNodeId) {
      this.signalRService.leaveNodeGroup(this.currentNodeId).catch(err =>
        console.warn('[NodeForm] Failed to leave node group:', err)
      );
    }
    this.signalRService.off('NewReading');
    // Cleanup overlays
    this.assignmentOverlayRef?.dispose();
    this.deleteOverlayRef?.dispose();
    this.deleteNodeOverlayRef?.dispose();
  }

  private async setupSignalR(nodeId: string): Promise<void> {
    // Ensure SignalR is connected before subscribing
    if (this.signalRService.connectionState() !== 'connected') {
      console.log('[NodeForm] SignalR not connected, waiting...');
      try {
        await this.signalRService.startConnection();
      } catch (error) {
        console.error('[NodeForm] Failed to connect SignalR:', error);
        return;
      }
    }

    console.log('[NodeForm] Setting up SignalR subscription for nodeId:', nodeId);

    // Store nodeId for cleanup
    this.currentNodeId = nodeId;

    // WICHTIG: Join Node Group um Events zu empfangen!
    // Das Backend sendet an Gruppen, nicht broadcast.
    try {
      await this.signalRService.joinNodeGroup(nodeId);
      console.log('[NodeForm] Joined node group:', nodeId);
    } catch (error) {
      console.error('[NodeForm] Failed to join node group:', error);
    }

    // Subscribe to new readings - update sensorsLatest directly (no HTTP call = no flicker)
    this.signalRService.onNewReading((reading: Reading) => {
      console.log('[NodeForm] Received reading:', reading.nodeId, 'current nodeId:', nodeId);
      // Case-insensitive comparison for GUIDs
      if (reading.nodeId?.toLowerCase() === nodeId?.toLowerCase()) {
        console.log('[NodeForm] Match! Updating sensorsLatest directly...');

        // Update sensorsLatest directly without HTTP call (no flicker!)
        this.updateSensorsLatestFromReading(reading);

        // Update history: If on first page, just reload from server to avoid duplicates
        // Otherwise just update total count
        if (this.lastHistoryEvent && this.lastHistoryEvent.first === 0) {
          // Reload first page from server (will include the new reading)
          this.loadReadingsLazy(this.lastHistoryEvent);
        } else {
          // Just increment total so pagination shows correct count
          this.historyTotalRecords.update(c => c + 1);
        }
      }
    });
  }

  /**
   * Update sensorsLatest signal directly from a SignalR reading.
   * This avoids HTTP calls and prevents UI flicker.
   */
  private updateSensorsLatestFromReading(reading: Reading): void {
    const current = this.sensorsLatest();
    if (!current) return;

    // Find the sensor by assignmentId
    const updatedSensors = current.sensors.map(sensor => {
      if (sensor.assignmentId === reading.assignmentId) {
        // Update the specific measurement
        const updatedMeasurements = sensor.measurements.map(m => {
          if (m.measurementType === reading.measurementType) {
            return {
              ...m,
              readingId: reading.id,
              rawValue: reading.rawValue,
              value: reading.value,
              timestamp: reading.timestamp
            };
          }
          return m;
        });

        return { ...sensor, measurements: updatedMeasurements };
      }
      return sensor;
    });

    this.sensorsLatest.set({ ...current, sensors: updatedSensors });
  }

  private initForm(): void {
    this.form = this.fb.group({
      nodeId: ['', [Validators.required, Validators.pattern(/^[a-zA-Z0-9_-]+$/)]],
      name: ['', [Validators.required, Validators.minLength(2)]],
      hubId: ['', [Validators.required]],
      protocol: [Protocol.WLAN, [Validators.required]],
      locationName: [''],
      nodeType: ['esp32'],  // Default: ESP32
      firmwareVersion: [''],
      isSimulation: [false],
      storageMode: [StorageMode.RemoteOnly]  // Sprint OS-01: Default to remote only
    });
  }

  private initAssignmentForm(): void {
    this.assignmentForm = this.fb.group({
      sensorId: ['', Validators.required],
      endpointId: [1, [Validators.required, Validators.min(1), Validators.max(254)]],
      alias: [''],
      i2cAddressOverride: [''],
      sdaPinOverride: [null],
      sclPinOverride: [null],
      oneWirePinOverride: [null],
      analogPinOverride: [null],
      digitalPinOverride: [null],
      triggerPinOverride: [null],
      echoPinOverride: [null],
      baudRateOverride: [null],
      intervalSecondsOverride: [null],
      isActive: [true]
    });
  }

  private loadHubs(): void {
    this.hubApiService.getAll().subscribe({
      next: (hubs) => {
        this.hubs.set(hubs);
        // Set default hub if in create mode and no hub is selected
        if (this.isCreateMode() && hubs.length > 0 && !this.form.get('hubId')?.value) {
          this.form.patchValue({ hubId: hubs[0].id });
        }
      },
      error: (error) => console.error('Error loading hubs:', error)
    });
  }

  private loadAvailableSensors(): void {
    this.sensorApiService.getAll().subscribe({
      next: (sensors) => this.availableSensors.set(sensors),
      error: (error) => console.error('Error loading sensors:', error)
    });
  }

  private loadAssignments(nodeId: string): void {
    this.isLoadingAssignments.set(true);
    this.assignmentApiService.getByNode(nodeId).subscribe({
      next: (assignments) => {
        this.assignments.set(assignments);
        this.isLoadingAssignments.set(false);
      },
      error: (error) => {
        console.error('Error loading assignments:', error);
        this.isLoadingAssignments.set(false);
      }
    });
  }

  private loadSensorsLatest(nodeId: string): void {
    this.isLoadingSensorsLatest.set(true);
    this.nodeApiService.getSensorsLatest(nodeId).subscribe({
      next: (data) => {
        this.sensorsLatest.set(data);
        this.isLoadingSensorsLatest.set(false);
      },
      error: (error) => {
        console.error('Error loading sensors latest:', error);
        this.isLoadingSensorsLatest.set(false);
      }
    });
  }

  /** Lazy load readings for Generic List */
  loadReadingsLazy(event: ListLazyEvent): void {
    const nodeId = this.node()?.id;
    if (!nodeId) return;

    // Store event for filter refresh
    this.lastHistoryEvent = event;

    this.isLoadingHistory.set(true);

    // Build QueryParams for backend
    const page = Math.floor(event.first / event.rows);
    const sortField = event.sortField ? (this.historyFieldMapping[event.sortField] || event.sortField) : 'Timestamp';
    const sortDir = event.sortOrder === -1 ? 'desc' : 'asc';

    // Merge nodeId with additional filters from drawer, convert to strings
    const filters: Record<string, string> = { nodeId };
    for (const [key, value] of Object.entries(this.historyFilters)) {
      if (value != null && value !== '') {
        filters[key] = String(value);
      }
    }

    const params: QueryParams = {
      page,
      size: event.rows,
      sort: `${sortField},${sortDir}`,
      search: event.globalFilter || undefined,
      filters
    };

    this.readingApiService.getPaged(params).subscribe({
      next: (result) => {
        this.readingHistory.set(result.items);
        this.historyTotalRecords.set(result.totalRecords);
        this.isLoadingHistory.set(false);
      },
      error: (error) => {
        console.error('Error loading reading history:', error);
        this.isLoadingHistory.set(false);
      }
    });
  }

  /** Handle filter changes from Generic List filter drawer */
  onHistoryFilterChange(filters: Record<string, unknown>): void {
    this.historyFilters = filters;
    // Reload with new filters - reset to first page
    if (this.lastHistoryEvent) {
      this.loadReadingsLazy({ ...this.lastHistoryEvent, first: 0 });
    }
  }

  /** Get unique sensor names from assignments for filter dropdown */
  getAssignedSensorNames(): { value: string; label: string }[] {
    return this.assignments().map(a => ({
      value: a.sensorCode,
      label: a.alias || a.sensorName
    }));
  }

  /** Get unique measurement types from available sensors for filter dropdown */
  getMeasurementTypes(): { value: string; label: string }[] {
    const types = new Map<string, string>();
    for (const sensor of this.availableSensors()) {
      for (const cap of sensor.capabilities || []) {
        if (!types.has(cap.measurementType)) {
          types.set(cap.measurementType, cap.displayName);
        }
      }
    }
    return Array.from(types.entries()).map(([value, label]) => ({ value, label }));
  }

  /** Converts a timestamp to a relative time string (e.g., "vor 19 Sekunden") */
  getRelativeTime(timestamp: string): string {
    // Reference timestampRefresh to trigger updates
    this.timestampRefresh();

    const now = new Date();
    // Backend returns UTC timestamps without 'Z' suffix - append it for correct parsing
    const utcTimestamp = timestamp.endsWith('Z') ? timestamp : timestamp + 'Z';
    const date = new Date(utcTimestamp);
    const diffMs = now.getTime() - date.getTime();
    const diffSec = Math.floor(diffMs / 1000);

    if (diffSec < 5) return 'gerade eben';
    if (diffSec < 60) return `vor ${diffSec} Sekunden`;

    const diffMin = Math.floor(diffSec / 60);
    if (diffMin < 60) return `vor ${diffMin} Minute${diffMin !== 1 ? 'n' : ''}`;

    const diffHour = Math.floor(diffMin / 60);
    if (diffHour < 24) return `vor ${diffHour} Stunde${diffHour !== 1 ? 'n' : ''}`;

    const diffDay = Math.floor(diffHour / 24);
    if (diffDay < 7) return `vor ${diffDay} Tag${diffDay !== 1 ? 'en' : ''}`;

    return date.toLocaleDateString('de-DE');
  }

  /** Formats a timestamp for table display */
  formatTimestamp(timestamp: string): string {
    const date = new Date(timestamp);
    return date.toLocaleString('de-DE', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit'
    });
  }

  private loadNode(id: string): void {
    this.isLoading.set(true);
    this.nodeApiService.getById(id).subscribe({
      next: (node) => {
        this.node.set(node);
        this.patchForm(node);
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Error loading node:', error);
        this.snackBar.open('Fehler beim Laden des Nodes', 'Schließen', { duration: 5000 });
        this.router.navigate(['/nodes']);
      }
    });
  }

  private patchForm(node: Node): void {
    this.form.patchValue({
      nodeId: node.nodeId,
      name: node.name,
      hubId: node.hubId,
      protocol: node.protocol ?? Protocol.WLAN,
      locationName: node.location?.name || '',
      firmwareVersion: node.firmwareVersion || '',
      isSimulation: node.isSimulation || false,
      // Sprint OS-01: Parse storageMode from API (can be string or number)
      storageMode: this.parseStorageMode(node.storageMode as StorageMode | string)
    });
    // Note: We use [readonly] in the template instead of disable()
    // to keep the form valid while preventing edits
  }

  /** Get label for storage mode */
  getStorageModeLabel(mode: StorageMode): string {
    const found = this.storageModes.find(m => m.value === mode);
    return found?.label ?? 'Unbekannt';
  }

  /**
   * Parse storage mode from API response (can be string or number)
   * API returns string like "LocalAndRemote", frontend uses numeric enum
   */
  private parseStorageMode(value: StorageMode | string | undefined | null): StorageMode {
    if (value === undefined || value === null) return StorageMode.RemoteOnly;

    // Already a number (enum value)
    if (typeof value === 'number') return value;

    // String from API - map to enum value
    const mapping: Record<string, StorageMode> = {
      'RemoteOnly': StorageMode.RemoteOnly,
      'LocalAndRemote': StorageMode.LocalAndRemote,
      'LocalOnly': StorageMode.LocalOnly,
      'LocalAutoSync': StorageMode.LocalAutoSync
    };
    return mapping[value] ?? StorageMode.RemoteOnly;
  }

  async onSave(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSaving.set(true);
    const formValue = this.form.getRawValue();

    if (this.isCreateMode()) {
      const dto: CreateNodeDto = {
        nodeId: formValue.nodeId,
        name: formValue.name,
        hubId: formValue.hubId,
        protocol: formValue.protocol,
        location: formValue.locationName ? { name: formValue.locationName } : undefined
      };

      this.nodeApiService.create(dto).subscribe({
        next: () => {
          this.snackBar.open('Node erstellt', 'Schließen', { duration: 3000 });
          this.router.navigate(['/nodes']);
        },
        error: (error) => {
          console.error('Error creating node:', error);
          this.snackBar.open('Fehler beim Erstellen des Nodes', 'Schließen', { duration: 5000 });
          this.isSaving.set(false);
        }
      });
    } else {
      const dto: UpdateNodeDto = {
        name: formValue.name,
        location: formValue.locationName ? { name: formValue.locationName } : undefined,
        firmwareVersion: formValue.firmwareVersion || undefined,
        isSimulation: formValue.isSimulation,
        storageMode: formValue.storageMode  // Sprint OS-01: Offline Storage Mode
      };

      // Save node and debug settings in parallel
      try {
        const nodeUpdate = firstValueFrom(this.nodeApiService.update(this.node()!.id, dto));
        const debugUpdate = this.debugControlComponent?.hasChanges()
          ? this.debugControlComponent.saveSettings()
          : Promise.resolve();

        await Promise.all([nodeUpdate, debugUpdate]);

        this.snackBar.open('Node aktualisiert', 'Schließen', { duration: 3000 });
        this.router.navigate(['/nodes']);
      } catch (error) {
        console.error('Error updating node:', error);
        this.snackBar.open('Fehler beim Aktualisieren des Nodes', 'Schließen', { duration: 5000 });
        this.isSaving.set(false);
      }
    }
  }

  /** Toggle zwischen View und Edit Mode (Lock-Button) */
  toggleEditMode(): void {
    if (this.isViewMode()) {
      this.mode.set('edit');
    } else {
      this.mode.set('view');
      // Reset form to original values when switching back to view
      const n = this.node();
      if (n) {
        this.patchForm(n);
      }
    }
  }

  onCancel(): void {
    this.router.navigate(['/nodes']);
  }

  onBack(): void {
    this.router.navigate(['/nodes']);
  }

  getHubName(hubId: string): string {
    const hub = this.hubs().find(h => h.id === hubId);
    return hub ? `${hub.name} (${hub.hubId})` : '';
  }

  getProtocolLabel(protocol: Protocol): string {
    switch (protocol) {
      case Protocol.WLAN:
        return 'WLAN';
      case Protocol.LoRaWAN:
        return 'LoRaWAN';
      default:
        return 'Unbekannt';
    }
  }

  getNodeTypeLabel(nodeType: string): string {
    const type = this.nodeTypes.find(t => t.value === nodeType);
    return type?.label ?? 'Unbekannt';
  }

  // === Sensor Assignment Methods ===

  getNextEndpointId(): number {
    const usedIds = this.assignments().map(a => a.endpointId);
    for (let i = 1; i <= 254; i++) {
      if (!usedIds.includes(i)) return i;
    }
    return 1;
  }

  getSensorIcon(sensorCode: string): string {
    const sensor = this.availableSensors().find(s => s.code === sensorCode);
    return sensor?.icon || 'sensors';
  }

  getSensorColor(sensorCode: string): string {
    const sensor = this.availableSensors().find(s => s.code === sensorCode);
    return sensor?.color || '#666';
  }

  /** Get capabilities (measurement types) for a sensor by its code */
  getSensorCapabilities(sensorCode: string): string[] {
    const sensor = this.availableSensors().find(s => s.code === sensorCode);
    return sensor?.capabilities?.map(c => c.displayName) || [];
  }

  // Max visible chips in header (overflow shows +X badge)
  readonly maxVisibleChips = 3;

  getVisibleCapabilities(sensorCode: string): string[] {
    return this.getSensorCapabilities(sensorCode).slice(0, this.maxVisibleChips);
  }

  getHiddenCapabilitiesCount(sensorCode: string): number {
    return Math.max(0, this.getSensorCapabilities(sensorCode).length - this.maxVisibleChips);
  }

  getHiddenCapabilitiesTooltip(sensorCode: string): string {
    return this.getSensorCapabilities(sensorCode).slice(this.maxVisibleChips).join(', ');
  }

  getUnassignedSensors(): Sensor[] {
    const assignedSensorIds = this.assignments().map(a => a.sensorId);
    return this.availableSensors().filter(s => !assignedSensorIds.includes(s.id));
  }

  openAssignmentForm(assignment?: NodeSensorAssignment): void {
    if (this.assignmentOverlayRef) return;

    this.editingAssignment.set(assignment || null);

    if (assignment) {
      // Edit mode
      this.assignmentForm.patchValue({
        sensorId: assignment.sensorId,
        endpointId: assignment.endpointId,
        alias: assignment.alias || '',
        i2cAddressOverride: assignment.i2cAddressOverride || '',
        sdaPinOverride: assignment.sdaPinOverride,
        sclPinOverride: assignment.sclPinOverride,
        oneWirePinOverride: assignment.oneWirePinOverride,
        analogPinOverride: assignment.analogPinOverride,
        digitalPinOverride: assignment.digitalPinOverride,
        triggerPinOverride: assignment.triggerPinOverride,
        echoPinOverride: assignment.echoPinOverride,
        baudRateOverride: assignment.baudRateOverride,
        intervalSecondsOverride: assignment.intervalSecondsOverride,
        isActive: assignment.isActive
      });
      this.assignmentForm.get('sensorId')?.disable();
    } else {
      // Create mode
      this.assignmentForm.reset({
        sensorId: '',
        endpointId: this.getNextEndpointId(),
        alias: '',
        i2cAddressOverride: '',
        sdaPinOverride: null,
        sclPinOverride: null,
        oneWirePinOverride: null,
        analogPinOverride: null,
        digitalPinOverride: null,
        triggerPinOverride: null,
        echoPinOverride: null,
        baudRateOverride: null,
        intervalSecondsOverride: null,
        isActive: true
      });
      this.assignmentForm.get('sensorId')?.enable();

      // Subscribe to sensor selection to prefill default values
      this.setupSensorSelectionListener();
    }

    // Open drawer via CDK Overlay
    this.isAssignmentDrawerOpen.set(true);

    const positionStrategy = this.overlay.position()
      .global()
      .right('0')
      .top('0');

    this.assignmentOverlayRef = this.overlay.create({
      positionStrategy,
      hasBackdrop: true,
      backdropClass: 'gt-drawer-backdrop',
      panelClass: ['gt-drawer-panel'],
      width: '420px',
      height: '100vh',
      scrollStrategy: this.overlay.scrollStrategies.block()
    });

    this.assignmentOverlayRef.backdropClick().subscribe(() => this.closeAssignmentDrawer());

    const portal = new TemplatePortal(this.assignmentDrawerTemplate, this.viewContainerRef);
    this.assignmentOverlayRef.attach(portal);

    // Trigger animation after attach
    requestAnimationFrame(() => {
      this.assignmentOverlayRef?.addPanelClass('open');
    });
  }

  private sensorSelectionSubscription: { unsubscribe: () => void } | null = null;

  private setupSensorSelectionListener(): void {
    // Use subscription to prefill default values when sensor is selected
    this.sensorSelectionSubscription = this.assignmentForm.get('sensorId')?.valueChanges.subscribe(sensorId => {
      if (sensorId) {
        this.prefillSensorDefaults(sensorId);
      }
    }) || null;
  }

  private prefillSensorDefaults(sensorId: string): void {
    const sensor = this.availableSensors().find(s => s.id === sensorId);
    if (!sensor) return;

    // Prefill the form with sensor default values (as placeholders/suggestions)
    this.assignmentForm.patchValue({
      i2cAddressOverride: sensor.i2cAddress || '',
      sdaPinOverride: sensor.sdaPin ?? null,
      sclPinOverride: sensor.sclPin ?? null,
      oneWirePinOverride: sensor.oneWirePin ?? null,
      analogPinOverride: sensor.analogPin ?? null,
      digitalPinOverride: sensor.digitalPin ?? null,
      triggerPinOverride: sensor.triggerPin ?? null,
      echoPinOverride: sensor.echoPin ?? null,
      baudRateOverride: sensor.baudRate ?? null,
      intervalSecondsOverride: sensor.intervalSeconds ?? null
    });
  }

  getSelectedSensor(): Sensor | undefined {
    const sensorId = this.assignmentForm.get('sensorId')?.value;
    return this.availableSensors().find(s => s.id === sensorId);
  }

  closeAssignmentDrawer(): void {
    if (this.assignmentOverlayRef) {
      this.assignmentOverlayRef.removePanelClass('open');
      // Wait for animation to complete before disposing
      setTimeout(() => {
        this.assignmentOverlayRef?.dispose();
        this.assignmentOverlayRef = null;
      }, 200);
    }
    this.isAssignmentDrawerOpen.set(false);
    this.editingAssignment.set(null);

    // Clean up sensor selection subscription
    if (this.sensorSelectionSubscription) {
      this.sensorSelectionSubscription.unsubscribe();
      this.sensorSelectionSubscription = null;
    }
  }

  cancelAssignmentForm(): void {
    this.closeAssignmentDrawer();
  }

  saveAssignment(): void {
    if (this.assignmentForm.invalid) {
      this.assignmentForm.markAllAsTouched();
      return;
    }

    const nodeId = this.node()?.id;
    if (!nodeId) return;

    const formValue = this.assignmentForm.getRawValue();
    const editing = this.editingAssignment();

    if (editing) {
      // Update existing assignment
      const dto: UpdateNodeSensorAssignmentDto = {
        alias: formValue.alias || undefined,
        i2cAddressOverride: formValue.i2cAddressOverride || undefined,
        sdaPinOverride: formValue.sdaPinOverride,
        sclPinOverride: formValue.sclPinOverride,
        oneWirePinOverride: formValue.oneWirePinOverride,
        analogPinOverride: formValue.analogPinOverride,
        digitalPinOverride: formValue.digitalPinOverride,
        triggerPinOverride: formValue.triggerPinOverride,
        echoPinOverride: formValue.echoPinOverride,
        baudRateOverride: formValue.baudRateOverride,
        intervalSecondsOverride: formValue.intervalSecondsOverride,
        isActive: formValue.isActive
      };

      this.assignmentApiService.update(nodeId, editing.id, dto).subscribe({
        next: () => {
          this.snackBar.open('Zuordnung aktualisiert', 'Schließen', { duration: 3000 });
          this.loadAssignments(nodeId);
          this.cancelAssignmentForm();
        },
        error: (error) => {
          console.error('Error updating assignment:', error);
          const msg = error?.error?.detail || 'Fehler beim Aktualisieren der Zuordnung';
          this.snackBar.open(msg, 'Schließen', { duration: 5000 });
        }
      });
    } else {
      // Create new assignment
      const dto: CreateNodeSensorAssignmentDto = {
        sensorId: formValue.sensorId,
        endpointId: formValue.endpointId,
        alias: formValue.alias || undefined,
        i2cAddressOverride: formValue.i2cAddressOverride || undefined,
        sdaPinOverride: formValue.sdaPinOverride,
        sclPinOverride: formValue.sclPinOverride,
        oneWirePinOverride: formValue.oneWirePinOverride,
        analogPinOverride: formValue.analogPinOverride,
        digitalPinOverride: formValue.digitalPinOverride,
        triggerPinOverride: formValue.triggerPinOverride,
        echoPinOverride: formValue.echoPinOverride,
        baudRateOverride: formValue.baudRateOverride,
        intervalSecondsOverride: formValue.intervalSecondsOverride
      };

      this.assignmentApiService.create(nodeId, dto).subscribe({
        next: () => {
          this.snackBar.open('Sensor zugeordnet', 'Schließen', { duration: 3000 });
          this.loadAssignments(nodeId);
          this.cancelAssignmentForm();
        },
        error: (error) => {
          console.error('Error creating assignment:', error);
          const msg = error?.error?.detail || 'Fehler beim Zuordnen des Sensors';
          this.snackBar.open(msg, 'Schließen', { duration: 5000 });
        }
      });
    }
  }

  deleteAssignment(assignment: NodeSensorAssignment): void {
    const nodeId = this.node()?.id;
    if (!nodeId) return;

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Zuordnung entfernen',
        message: `Möchten Sie die Zuordnung von "${assignment.sensorName}" wirklich entfernen?`,
        confirmText: 'Entfernen',
        cancelText: 'Abbrechen'
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.assignmentApiService.remove(nodeId, assignment.id).subscribe({
          next: () => {
            this.snackBar.open('Zuordnung entfernt', 'Schließen', { duration: 3000 });
            this.loadAssignments(nodeId);
          },
          error: (error) => {
            console.error('Error deleting assignment:', error);
            this.snackBar.open('Fehler beim Entfernen der Zuordnung', 'Schließen', { duration: 5000 });
          }
        });
      }
    });
  }

  toggleAssignmentActive(assignment: NodeSensorAssignment): void {
    const nodeId = this.node()?.id;
    if (!nodeId) return;

    const dto: UpdateNodeSensorAssignmentDto = {
      isActive: !assignment.isActive
    };

    this.assignmentApiService.update(nodeId, assignment.id, dto).subscribe({
      next: () => {
        const status = !assignment.isActive ? 'aktiviert' : 'deaktiviert';
        this.snackBar.open(`Sensor ${status}`, 'Schließen', { duration: 3000 });
        this.loadAssignments(nodeId);
      },
      error: (error) => {
        console.error('Error toggling assignment:', error);
        this.snackBar.open('Fehler beim Ändern des Status', 'Schließen', { duration: 5000 });
      }
    });
  }

  /** Node löschen */
  deleteNode(): void {
    this.openDeleteNodeDrawer();
  }

  // === Delete Node Drawer Methods ===

  openDeleteNodeDrawer(): void {
    if (this.deleteNodeOverlayRef) return;

    this.deleteConfirmInput = '';

    const positionStrategy = this.overlay.position()
      .global()
      .right('0')
      .top('0');

    this.deleteNodeOverlayRef = this.overlay.create({
      positionStrategy,
      hasBackdrop: true,
      backdropClass: 'gt-drawer-backdrop',
      panelClass: ['gt-drawer-panel'],
      width: '400px',
      height: '100vh',
      scrollStrategy: this.overlay.scrollStrategies.block()
    });

    this.deleteNodeOverlayRef.backdropClick().subscribe(() => this.closeDeleteNodeDrawer());

    const portal = new TemplatePortal(this.deleteNodeDrawerTemplate, this.viewContainerRef);
    this.deleteNodeOverlayRef.attach(portal);

    requestAnimationFrame(() => {
      this.deleteNodeOverlayRef?.addPanelClass('open');
    });
  }

  closeDeleteNodeDrawer(): void {
    if (this.deleteNodeOverlayRef) {
      this.deleteNodeOverlayRef.removePanelClass('open');
      setTimeout(() => {
        this.deleteNodeOverlayRef?.dispose();
        this.deleteNodeOverlayRef = null;
      }, 200);
    }
    this.deleteConfirmInput = '';
  }

  confirmDeleteNode(): void {
    const node = this.node();
    if (!node || this.deleteConfirmInput !== 'DELETE') return;

    this.isDeleting.set(true);
    this.nodeApiService.remove(node.id).subscribe({
      next: () => {
        this.closeDeleteNodeDrawer();
        this.snackBar.open('Node gelöscht', 'Schließen', { duration: 3000 });
        this.router.navigate(['/nodes']);
      },
      error: (error) => {
        console.error('Error deleting node:', error);
        this.snackBar.open('Fehler beim Löschen des Nodes', 'Schließen', { duration: 5000 });
        this.isDeleting.set(false);
      }
    });
  }

  // === Delete Readings Drawer Methods ===

  openDeleteDrawer(): void {
    if (this.deleteOverlayRef) return;

    const positionStrategy = this.overlay.position()
      .global()
      .right('0')
      .top('0');

    this.deleteOverlayRef = this.overlay.create({
      positionStrategy,
      hasBackdrop: true,
      backdropClass: 'gt-drawer-backdrop',
      panelClass: ['gt-drawer-panel'],
      width: '420px',
      height: '100vh',
      scrollStrategy: this.overlay.scrollStrategies.block()
    });

    this.deleteOverlayRef.backdropClick().subscribe(() => this.closeDeleteDrawer());

    const portal = new TemplatePortal(this.deleteReadingsDrawerTemplate, this.viewContainerRef);
    this.deleteOverlayRef.attach(portal);

    // Trigger animation after attach
    requestAnimationFrame(() => {
      this.deleteOverlayRef?.addPanelClass('open');
    });
  }

  closeDeleteDrawer(): void {
    if (this.deleteOverlayRef) {
      this.deleteOverlayRef.removePanelClass('open');
      setTimeout(() => {
        this.deleteOverlayRef?.dispose();
        this.deleteOverlayRef = null;
      }, 200);
    }
  }

  onDeleteReadingsComplete(result: { deletedCount: number }): void {
    this.closeDeleteDrawer();
    this.snackBar.open(`${result.deletedCount} Messwerte gelöscht`, 'Schließen', { duration: 3000 });

    // Refresh the readings list
    if (this.lastHistoryEvent) {
      this.loadReadingsLazy({ ...this.lastHistoryEvent, first: 0 });
    }

    // Also refresh sensors latest
    const nodeId = this.node()?.id;
    if (nodeId) {
      this.loadSensorsLatest(nodeId);
    }
  }
}
