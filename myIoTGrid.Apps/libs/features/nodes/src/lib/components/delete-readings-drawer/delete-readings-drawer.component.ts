import { Component, EventEmitter, Input, Output, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { provideNativeDateAdapter } from '@angular/material/core';
import { ReadingApiService, NodeSensorAssignmentApiService } from '@myiotgrid/shared/data-access';
import { Node, NodeSensorAssignment, DeleteReadingsRangeDto, DeleteReadingsResultDto } from '@myiotgrid/shared/models';

interface MeasurementTypeOption {
  value: string;
  label: string;
}

@Component({
  selector: 'myiotgrid-delete-readings-drawer',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatSelectModule,
    MatDatepickerModule,
    MatInputModule,
    MatProgressSpinnerModule
  ],
  providers: [provideNativeDateAdapter()],
  templateUrl: './delete-readings-drawer.component.html',
  styleUrl: './delete-readings-drawer.component.scss'
})
export class DeleteReadingsDrawerComponent {
  private readonly readingApiService = inject(ReadingApiService);
  private readonly assignmentApiService = inject(NodeSensorAssignmentApiService);

  @Input() set node(value: Node | null) {
    this._node.set(value);
    if (value) {
      this.loadAssignments(value.id);
    }
  }

  @Output() close = new EventEmitter<void>();
  @Output() deleted = new EventEmitter<DeleteReadingsResultDto>();

  private readonly _node = signal<Node | null>(null);
  readonly currentNode = computed(() => this._node());

  readonly assignments = signal<NodeSensorAssignment[]>([]);
  readonly isLoading = signal(false);
  readonly isDeleting = signal(false);
  readonly deleteResult = signal<DeleteReadingsResultDto | null>(null);
  readonly error = signal<string | null>(null);

  // Form values
  fromDate: Date | null = null;
  toDate: Date | null = null;
  selectedAssignmentId: string | null = null;
  selectedMeasurementType: string | null = null;

  readonly measurementTypes = signal<MeasurementTypeOption[]>([
    { value: 'temperature', label: 'Temperatur' },
    { value: 'humidity', label: 'Luftfeuchtigkeit' },
    { value: 'pressure', label: 'Luftdruck' },
    { value: 'lux', label: 'Helligkeit' },
    { value: 'co2', label: 'CO₂' },
    { value: 'pm25', label: 'Feinstaub PM2.5' },
    { value: 'pm10', label: 'Feinstaub PM10' },
    { value: 'soil_moisture', label: 'Bodenfeuchtigkeit' },
    { value: 'distance', label: 'Entfernung' },
    { value: 'battery', label: 'Batterie' }
  ]);

  readonly canDelete = computed(() => {
    return this.fromDate !== null &&
           this.toDate !== null &&
           this.fromDate <= this.toDate &&
           !this.isDeleting();
  });

  private async loadAssignments(nodeId: string): Promise<void> {
    this.isLoading.set(true);
    this.error.set(null);
    try {
      const assignments = await this.assignmentApiService.getByNode(nodeId).toPromise();
      this.assignments.set(assignments || []);
    } catch (err) {
      console.error('Failed to load assignments:', err);
      this.error.set('Fehler beim Laden der Sensoren');
    } finally {
      this.isLoading.set(false);
    }
  }

  onClose(): void {
    this.close.emit();
  }

  async onDelete(): Promise<void> {
    const node = this.currentNode();
    if (!node || !this.fromDate || !this.toDate) return;

    this.isDeleting.set(true);
    this.error.set(null);
    this.deleteResult.set(null);

    const dto: DeleteReadingsRangeDto = {
      nodeId: node.id,
      from: this.toIsoString(this.fromDate),
      to: this.toIsoString(this.toDate, true),
      assignmentId: this.selectedAssignmentId || undefined,
      measurementType: this.selectedMeasurementType || undefined
    };

    try {
      const result = await this.readingApiService.deleteRange(dto).toPromise();
      if (result) {
        this.deleteResult.set(result);
        this.deleted.emit(result);
      }
    } catch (err) {
      console.error('Failed to delete readings:', err);
      this.error.set('Fehler beim Löschen der Messwerte');
    } finally {
      this.isDeleting.set(false);
    }
  }

  onReset(): void {
    this.fromDate = null;
    this.toDate = null;
    this.selectedAssignmentId = null;
    this.selectedMeasurementType = null;
    this.deleteResult.set(null);
    this.error.set(null);
  }

  getAssignmentLabel(assignment: NodeSensorAssignment): string {
    return `${assignment.sensorName} (Endpoint ${assignment.endpointId})`;
  }

  private toIsoString(date: Date, endOfDay = false): string {
    const d = new Date(date);
    if (endOfDay) {
      d.setHours(23, 59, 59, 999);
    } else {
      d.setHours(0, 0, 0, 0);
    }
    return d.toISOString();
  }
}
