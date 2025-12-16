import { Component, inject, signal, OnInit, input, output, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule, provideNativeDateAdapter, MAT_DATE_LOCALE } from '@angular/material/core';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { firstValueFrom } from 'rxjs';
import { ExpeditionApiService, NodeApiService } from '@myiotgrid/shared/data-access';
import {
  Expedition,
  CreateExpeditionDto,
  UpdateExpeditionDto,
  Node,
  NodeReadingDateRange,
  ExpeditionStatus
} from '@myiotgrid/shared/models';

@Component({
  selector: 'myiotgrid-expedition-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatButtonModule,
    MatChipsModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  providers: [
    provideNativeDateAdapter(),
    { provide: MAT_DATE_LOCALE, useValue: 'de-DE' }
  ],
  templateUrl: './expedition-form.component.html',
  styleUrl: './expedition-form.component.scss'
})
export class ExpeditionFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly expeditionApiService = inject(ExpeditionApiService);
  private readonly nodeApiService = inject(NodeApiService);

  // Inputs for drawer mode
  mode = input<'create' | 'edit'>('create');
  expedition = input<Expedition | null>(null);

  // Outputs
  saved = output<boolean>();
  cancelled = output<void>();

  form!: FormGroup;
  nodes = signal<Node[]>([]);
  tags = signal<string[]>([]);
  tagInput = signal('');
  isSubmitting = signal(false);
  error = signal<string | null>(null);

  // Date constraints from node readings
  minDate = signal<Date | null>(null);
  maxDate = signal<Date | null>(null);
  dateRangeLoading = signal(false);
  selectedNodeDateRange = signal<NodeReadingDateRange | null>(null);

  // Status options for edit mode
  ExpeditionStatus = ExpeditionStatus;
  statusOptions = [
    { value: ExpeditionStatus.Planned, label: 'Geplant' },
    { value: ExpeditionStatus.Active, label: 'Aktiv' },
    { value: ExpeditionStatus.Completed, label: 'Abgeschlossen' },
    { value: ExpeditionStatus.Archived, label: 'Archiviert' }
  ];

  get isEditMode(): boolean {
    return this.mode() === 'edit';
  }

  get drawerTitle(): string {
    return this.isEditMode ? 'Messfahrt bearbeiten' : 'Neue Messfahrt';
  }

  constructor() {
    // React to node selection changes
    effect(() => {
      const nodeId = this.form?.get('nodeId')?.value;
      if (nodeId && !this.isEditMode) {
        this.loadDateRangeForNode(nodeId);
      }
    });
  }

  ngOnInit(): void {
    this.initForm();
    this.loadNodes();

    // Load existing data if editing
    if (this.isEditMode && this.expedition()) {
      this.populateForm(this.expedition()!);
    }

    // Watch for node selection changes
    this.form.get('nodeId')?.valueChanges.subscribe(nodeId => {
      if (nodeId && !this.isEditMode) {
        this.loadDateRangeForNode(nodeId);
      }
    });
  }

  private initForm(): void {
    this.form = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(200)]],
      description: [''],
      nodeId: ['', Validators.required],
      status: [ExpeditionStatus.Planned],
      startDate: [null as Date | null, Validators.required],
      startTime: ['10:00', Validators.required],
      endDate: [null as Date | null, Validators.required],
      endTime: ['18:00', Validators.required]
    });
  }

  private populateForm(expedition: Expedition): void {
    const startDate = new Date(expedition.startTime);
    const endDate = new Date(expedition.endTime);

    this.form.patchValue({
      name: expedition.name,
      description: expedition.description || '',
      nodeId: expedition.nodeId,
      status: expedition.status,
      startDate: startDate,
      startTime: this.formatTime(startDate),
      endDate: endDate,
      endTime: this.formatTime(endDate)
    });

    this.tags.set(expedition.tags || []);

    // Load date range for the node in edit mode
    if (expedition.nodeId) {
      this.loadDateRangeForNode(expedition.nodeId);
    }
  }

  private formatTime(date: Date): string {
    return `${date.getHours().toString().padStart(2, '0')}:${date.getMinutes().toString().padStart(2, '0')}`;
  }

  async loadNodes(): Promise<void> {
    try {
      const allNodes = await firstValueFrom(this.nodeApiService.getAll());
      this.nodes.set(allNodes);
    } catch (err) {
      console.error('Failed to load nodes:', err);
    }
  }

  async loadDateRangeForNode(nodeId: string): Promise<void> {
    this.dateRangeLoading.set(true);
    this.error.set(null);

    try {
      const dateRange = await firstValueFrom(this.nodeApiService.getReadingDateRange(nodeId));
      this.selectedNodeDateRange.set(dateRange);

      if (dateRange.minDate) {
        // minDate = erster Reading-Datensatz (keine Auswahl VOR ersten Daten)
        this.minDate.set(new Date(dateRange.minDate));
        // maxDate = unbegrenzt (Zukunft ist erlaubt)
        this.maxDate.set(null);

        // Auto-set dates to the range if not in edit mode
        if (!this.isEditMode) {
          this.form.patchValue({
            startDate: new Date(dateRange.minDate),
            endDate: dateRange.maxDate ? new Date(dateRange.maxDate) : new Date()
          });
        }
      } else {
        this.minDate.set(null);
        this.maxDate.set(null);
        this.error.set('Dieser Node hat keine Messdaten. Bitte wählen Sie einen anderen Node.');
      }
    } catch (err) {
      console.error('Failed to load date range:', err);
      this.minDate.set(null);
      this.maxDate.set(null);
    } finally {
      this.dateRangeLoading.set(false);
    }
  }

  addTag(): void {
    const tag = this.tagInput().trim();
    if (tag && !this.tags().includes(tag)) {
      this.tags.update(tags => [...tags, tag]);
      this.tagInput.set('');
    }
  }

  removeTag(tagToRemove: string): void {
    this.tags.update(tags => tags.filter(t => t !== tagToRemove));
  }

  onTagInputKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter') {
      event.preventDefault();
      this.addTag();
    }
  }

  async onSubmit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const formValue = this.form.value;

    // Combine date and time
    const startDateTime = this.combineDateTime(formValue.startDate, formValue.startTime);
    const endDateTime = this.combineDateTime(formValue.endDate, formValue.endTime);

    // Validate time range
    if (startDateTime >= endDateTime) {
      this.error.set('Startzeit muss vor Endzeit liegen');
      return;
    }

    this.isSubmitting.set(true);
    this.error.set(null);

    try {
      if (this.isEditMode && this.expedition()) {
        const updateDto: UpdateExpeditionDto = {
          name: formValue.name,
          description: formValue.description || undefined,
          status: formValue.status,
          startTime: startDateTime.toISOString(),
          endTime: endDateTime.toISOString(),
          tags: this.tags()
        };

        await firstValueFrom(
          this.expeditionApiService.update(this.expedition()!.id, updateDto)
        );
      } else {
        const createDto: CreateExpeditionDto = {
          name: formValue.name,
          description: formValue.description || undefined,
          nodeId: formValue.nodeId,
          startTime: startDateTime.toISOString(),
          endTime: endDateTime.toISOString(),
          tags: this.tags().length > 0 ? this.tags() : undefined
        };

        await firstValueFrom(this.expeditionApiService.create(createDto));
      }

      this.saved.emit(true);
    } catch (err: unknown) {
      console.error('Failed to save expedition:', err);
      const errorMessage = err instanceof Error ? err.message : 'Unbekannter Fehler';
      this.error.set(`Fehler beim Speichern: ${errorMessage}`);
    } finally {
      this.isSubmitting.set(false);
    }
  }

  private combineDateTime(date: Date, time: string): Date {
    const [hours, minutes] = time.split(':').map(Number);
    const result = new Date(date);
    result.setHours(hours, minutes, 0, 0);
    return result;
  }

  onCancel(): void {
    this.cancelled.emit();
  }
}
