import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
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
import { NodeApiService, HubApiService, NodeSensorAssignmentApiService, SensorApiService } from '@myiotgrid/shared/data-access';
import {
  Node, CreateNodeDto, UpdateNodeDto, Hub, Protocol,
  NodeSensorAssignment, CreateNodeSensorAssignmentDto, UpdateNodeSensorAssignmentDto,
  Sensor
} from '@myiotgrid/shared/models';
import { LoadingSpinnerComponent } from '@myiotgrid/shared/ui';
import { ConfirmDialogComponent } from '@myiotgrid/shared/ui';

type FormMode = 'view' | 'edit' | 'create';

@Component({
  selector: 'myiotgrid-node-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
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
    LoadingSpinnerComponent
  ],
  templateUrl: './node-form.component.html',
  styleUrl: './node-form.component.scss'
})
export class NodeFormComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly nodeApiService = inject(NodeApiService);
  private readonly hubApiService = inject(HubApiService);
  private readonly assignmentApiService = inject(NodeSensorAssignmentApiService);
  private readonly sensorApiService = inject(SensorApiService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialog = inject(MatDialog);

  readonly isLoading = signal(true);
  readonly isSaving = signal(false);
  readonly mode = signal<FormMode>('view');
  readonly node = signal<Node | null>(null);
  readonly hubs = signal<Hub[]>([]);

  // Sensor Assignment
  readonly assignments = signal<NodeSensorAssignment[]>([]);
  readonly availableSensors = signal<Sensor[]>([]);
  readonly isLoadingAssignments = signal(false);
  readonly showAssignmentForm = signal(false);
  readonly editingAssignment = signal<NodeSensorAssignment | null>(null);
  assignmentForm!: FormGroup;

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
    }
  }

  private initForm(): void {
    this.form = this.fb.group({
      nodeId: ['', [Validators.required, Validators.pattern(/^[a-zA-Z0-9_-]+$/)]],
      name: ['', [Validators.required, Validators.minLength(2)]],
      hubId: ['', [Validators.required]],
      protocol: [Protocol.WLAN, [Validators.required]],
      locationName: [''],
      firmwareVersion: ['']
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
      intervalSecondsOverride: [null],
      isActive: [true]
    });
  }

  private loadHubs(): void {
    this.hubApiService.getAll().subscribe({
      next: (hubs) => this.hubs.set(hubs),
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
      firmwareVersion: node.firmwareVersion || ''
    });
    // Note: We use [readonly] in the template instead of disable()
    // to keep the form valid while preventing edits
  }

  onSave(): void {
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
        firmwareVersion: formValue.firmwareVersion || undefined
      };

      this.nodeApiService.update(this.node()!.id, dto).subscribe({
        next: () => {
          this.snackBar.open('Node aktualisiert', 'Schließen', { duration: 3000 });
          this.router.navigate(['/nodes']);
        },
        error: (error) => {
          console.error('Error updating node:', error);
          this.snackBar.open('Fehler beim Aktualisieren des Nodes', 'Schließen', { duration: 5000 });
          this.isSaving.set(false);
        }
      });
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

  getProtocolIcon(): string {
    const protocol = this.form.get('protocol')?.value;
    switch (protocol) {
      case Protocol.WLAN:
        return 'wifi';
      case Protocol.LoRaWAN:
        return 'cell_tower';
      default:
        return 'router';
    }
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

  getUnassignedSensors(): Sensor[] {
    const assignedSensorIds = this.assignments().map(a => a.sensorId);
    return this.availableSensors().filter(s => !assignedSensorIds.includes(s.id));
  }

  openAssignmentForm(assignment?: NodeSensorAssignment): void {
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
        intervalSecondsOverride: null,
        isActive: true
      });
      this.assignmentForm.get('sensorId')?.enable();
    }

    this.showAssignmentForm.set(true);
  }

  cancelAssignmentForm(): void {
    this.showAssignmentForm.set(false);
    this.editingAssignment.set(null);
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
}
