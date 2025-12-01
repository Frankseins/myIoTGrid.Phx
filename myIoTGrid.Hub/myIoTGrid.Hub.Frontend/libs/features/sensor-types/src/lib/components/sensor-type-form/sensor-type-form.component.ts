import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormArray } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDividerModule } from '@angular/material/divider';
import { MatExpansionModule } from '@angular/material/expansion';
import { SensorTypeApiService } from '@myiotgrid/shared/data-access';
import {
  SensorType,
  CreateSensorTypeDto,
  UpdateSensorTypeDto,
  CommunicationProtocol,
  COMMUNICATION_PROTOCOL_LABELS,
  CreateSensorTypeCapabilityDto
} from '@myiotgrid/shared/models';
import { LoadingSpinnerComponent } from '@myiotgrid/shared/ui';

type FormMode = 'view' | 'edit' | 'create';

@Component({
  selector: 'myiotgrid-sensor-type-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatIconModule,
    MatButtonModule,
    MatTooltipModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    MatSnackBarModule,
    MatDividerModule,
    MatExpansionModule,
    LoadingSpinnerComponent
  ],
  templateUrl: './sensor-type-form.component.html',
  styleUrl: './sensor-type-form.component.scss'
})
export class SensorTypeFormComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly sensorTypeApiService = inject(SensorTypeApiService);
  private readonly snackBar = inject(MatSnackBar);

  readonly isLoading = signal(true);
  readonly isSaving = signal(false);
  readonly mode = signal<FormMode>('view');
  readonly sensorType = signal<SensorType | null>(null);

  readonly isViewMode = computed(() => this.mode() === 'view');
  readonly isEditMode = computed(() => this.mode() === 'edit');
  readonly isCreateMode = computed(() => this.mode() === 'create');
  readonly isReadonly = computed(() => this.mode() === 'view');

  readonly pageTitle = computed(() => {
    switch (this.mode()) {
      case 'create': return 'Neuer Sensortyp';
      case 'edit': return 'Sensortyp bearbeiten';
      default: return 'Sensortyp Details';
    }
  });

  form!: FormGroup;

  readonly categories: { value: string; label: string }[] = [
    { value: 'climate', label: 'Klima' },
    { value: 'water', label: 'Wasser' },
    { value: 'location', label: 'Standort' },
    { value: 'custom', label: 'Benutzerdefiniert' }
  ];

  readonly protocols: { value: CommunicationProtocol; label: string }[] = Object.entries(COMMUNICATION_PROTOCOL_LABELS).map(
    ([key, label]) => ({ value: parseInt(key) as CommunicationProtocol, label })
  );

  readonly standardIcons: string[] = [
    'thermostat', 'water_drop', 'air', 'eco', 'wb_sunny', 'cloud',
    'waves', 'opacity', 'speed', 'battery_full', 'signal_cellular_alt',
    'sensors', 'co2', 'grass', 'bolt', 'light_mode'
  ];

  readonly presetColors: string[] = [
    '#f44336', '#e91e63', '#9c27b0', '#673ab7', '#3f51b5', '#2196f3',
    '#03a9f4', '#00bcd4', '#009688', '#4caf50', '#8bc34a', '#cddc39',
    '#ffeb3b', '#ffc107', '#ff9800', '#ff5722', '#795548', '#607d8b'
  ];

  ngOnInit(): void {
    this.initForm();

    const id = this.route.snapshot.paramMap.get('id');
    const queryMode = this.route.snapshot.queryParamMap.get('mode');

    if (id === 'new') {
      this.mode.set('create');
      this.isLoading.set(false);
    } else if (id) {
      this.mode.set(queryMode === 'edit' ? 'edit' : 'view');
      this.loadSensorType(id);
    }
  }

  private initForm(): void {
    this.form = this.fb.group({
      // Basic Information
      code: ['', [Validators.required, Validators.pattern(/^[a-z0-9_-]+$/)]],
      name: ['', [Validators.required, Validators.minLength(2)]],
      manufacturer: [''],
      datasheetUrl: [''],
      description: [''],
      category: ['custom', [Validators.required]],

      // Communication Protocol
      protocol: [CommunicationProtocol.I2C, [Validators.required]],
      defaultI2CAddress: [''],
      defaultSdaPin: [null],
      defaultSclPin: [null],
      defaultOneWirePin: [null],
      defaultAnalogPin: [null],
      defaultDigitalPin: [null],
      defaultTriggerPin: [null],
      defaultEchoPin: [null],

      // Timing & Calibration
      defaultIntervalSeconds: [60, [Validators.required, Validators.min(1)]],
      minIntervalSeconds: [1, [Validators.required, Validators.min(1)]],
      warmupTimeMs: [0, [Validators.min(0)]],
      defaultOffsetCorrection: [0],
      defaultGainCorrection: [1.0],

      // Appearance
      icon: ['sensors'],
      color: ['#607d8b'],

      // Capabilities (for creating new sensor types)
      capabilities: this.fb.array([])
    });
  }

  get capabilitiesArray(): FormArray {
    return this.form.get('capabilities') as FormArray;
  }

  addCapability(): void {
    const capabilityGroup = this.fb.group({
      measurementType: ['', Validators.required],
      displayName: ['', Validators.required],
      unit: ['', Validators.required],
      minValue: [null],
      maxValue: [null],
      resolution: [0.01],
      accuracy: [0.5],
      matterClusterId: [null],
      matterClusterName: [''],
      sortOrder: [this.capabilitiesArray.length]
    });
    this.capabilitiesArray.push(capabilityGroup);
  }

  removeCapability(index: number): void {
    this.capabilitiesArray.removeAt(index);
  }

  private loadSensorType(id: string): void {
    this.isLoading.set(true);
    this.sensorTypeApiService.getById(id).subscribe({
      next: (sensorType) => {
        this.sensorType.set(sensorType);
        this.patchForm(sensorType);
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Error loading sensor type:', error);
        this.snackBar.open('Fehler beim Laden des Sensortyps', 'Schließen', { duration: 5000 });
        this.router.navigate(['/sensor-types']);
      }
    });
  }

  private patchForm(sensorType: SensorType): void {
    this.form.patchValue({
      code: sensorType.code,
      name: sensorType.name,
      manufacturer: sensorType.manufacturer || '',
      datasheetUrl: sensorType.datasheetUrl || '',
      description: sensorType.description || '',
      category: sensorType.category || 'custom',
      protocol: sensorType.protocol ?? CommunicationProtocol.I2C,
      defaultI2CAddress: sensorType.defaultI2CAddress || '',
      defaultSdaPin: sensorType.defaultSdaPin,
      defaultSclPin: sensorType.defaultSclPin,
      defaultOneWirePin: sensorType.defaultOneWirePin,
      defaultAnalogPin: sensorType.defaultAnalogPin,
      defaultDigitalPin: sensorType.defaultDigitalPin,
      defaultTriggerPin: sensorType.defaultTriggerPin,
      defaultEchoPin: sensorType.defaultEchoPin,
      defaultIntervalSeconds: sensorType.defaultIntervalSeconds ?? 60,
      minIntervalSeconds: sensorType.minIntervalSeconds ?? 1,
      warmupTimeMs: sensorType.warmupTimeMs ?? 0,
      defaultOffsetCorrection: sensorType.defaultOffsetCorrection ?? 0,
      defaultGainCorrection: sensorType.defaultGainCorrection ?? 1.0,
      icon: sensorType.icon || 'sensors',
      color: sensorType.color || '#607d8b'
    });
  }

  onSave(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSaving.set(true);
    const formValue = this.form.getRawValue();

    if (this.isCreateMode()) {
      const capabilities: CreateSensorTypeCapabilityDto[] = formValue.capabilities.map((cap: CreateSensorTypeCapabilityDto) => ({
        measurementType: cap.measurementType,
        displayName: cap.displayName,
        unit: cap.unit,
        minValue: cap.minValue,
        maxValue: cap.maxValue,
        resolution: cap.resolution,
        accuracy: cap.accuracy,
        matterClusterId: cap.matterClusterId,
        matterClusterName: cap.matterClusterName || undefined,
        sortOrder: cap.sortOrder
      }));

      const createDto: CreateSensorTypeDto = {
        code: formValue.code,
        name: formValue.name,
        protocol: formValue.protocol,
        manufacturer: formValue.manufacturer || undefined,
        datasheetUrl: formValue.datasheetUrl || undefined,
        description: formValue.description || undefined,
        defaultI2CAddress: formValue.defaultI2CAddress || undefined,
        defaultSdaPin: formValue.defaultSdaPin,
        defaultSclPin: formValue.defaultSclPin,
        defaultOneWirePin: formValue.defaultOneWirePin,
        defaultAnalogPin: formValue.defaultAnalogPin,
        defaultDigitalPin: formValue.defaultDigitalPin,
        defaultTriggerPin: formValue.defaultTriggerPin,
        defaultEchoPin: formValue.defaultEchoPin,
        defaultIntervalSeconds: formValue.defaultIntervalSeconds,
        minIntervalSeconds: formValue.minIntervalSeconds,
        warmupTimeMs: formValue.warmupTimeMs,
        defaultOffsetCorrection: formValue.defaultOffsetCorrection,
        defaultGainCorrection: formValue.defaultGainCorrection,
        category: formValue.category,
        icon: formValue.icon || undefined,
        color: formValue.color || undefined,
        capabilities: capabilities.length > 0 ? capabilities : undefined
      };

      this.sensorTypeApiService.create(createDto).subscribe({
        next: () => {
          this.snackBar.open('Sensortyp erstellt', 'Schließen', { duration: 3000 });
          this.router.navigate(['/sensor-types']);
        },
        error: (error) => {
          console.error('Error creating sensor type:', error);
          const message = error?.error?.detail || 'Fehler beim Erstellen des Sensortyps';
          this.snackBar.open(message, 'Schließen', { duration: 5000 });
          this.isSaving.set(false);
        }
      });
    } else {
      const updateDto: UpdateSensorTypeDto = {
        name: formValue.name,
        manufacturer: formValue.manufacturer || undefined,
        datasheetUrl: formValue.datasheetUrl || undefined,
        description: formValue.description || undefined,
        defaultI2CAddress: formValue.defaultI2CAddress || undefined,
        defaultSdaPin: formValue.defaultSdaPin,
        defaultSclPin: formValue.defaultSclPin,
        defaultOneWirePin: formValue.defaultOneWirePin,
        defaultAnalogPin: formValue.defaultAnalogPin,
        defaultDigitalPin: formValue.defaultDigitalPin,
        defaultTriggerPin: formValue.defaultTriggerPin,
        defaultEchoPin: formValue.defaultEchoPin,
        defaultIntervalSeconds: formValue.defaultIntervalSeconds,
        minIntervalSeconds: formValue.minIntervalSeconds,
        warmupTimeMs: formValue.warmupTimeMs,
        defaultOffsetCorrection: formValue.defaultOffsetCorrection,
        defaultGainCorrection: formValue.defaultGainCorrection,
        category: formValue.category,
        icon: formValue.icon || undefined,
        color: formValue.color || undefined
      };

      const currentId = this.sensorType()?.id;
      if (currentId) {
        this.sensorTypeApiService.update(currentId, updateDto).subscribe({
          next: () => {
            this.snackBar.open('Sensortyp aktualisiert', 'Schließen', { duration: 3000 });
            this.router.navigate(['/sensor-types']);
          },
          error: (error) => {
            console.error('Error updating sensor type:', error);
            const message = error?.error?.detail || 'Fehler beim Aktualisieren des Sensortyps';
            this.snackBar.open(message, 'Schließen', { duration: 5000 });
            this.isSaving.set(false);
          }
        });
      }
    }
  }

  /** Toggle zwischen View und Edit Mode (Lock-Button) */
  toggleEditMode(): void {
    // TODO: isGlobal-Prüfung später wieder aktivieren wenn Cloud-Sync implementiert ist
    if (this.isViewMode()) {
      this.mode.set('edit');
    } else {
      this.mode.set('view');
      // Reset form to original values when switching back to view
      const st = this.sensorType();
      if (st) {
        this.patchForm(st);
      }
    }
  }

  onCancel(): void {
    this.router.navigate(['/sensor-types']);
  }

  onBack(): void {
    this.router.navigate(['/sensor-types']);
  }

  selectIcon(icon: string): void {
    this.form.get('icon')?.setValue(icon);
  }

  selectColor(color: string): void {
    this.form.get('color')?.setValue(color);
  }

  getProtocolLabel(protocol: CommunicationProtocol): string {
    return COMMUNICATION_PROTOCOL_LABELS[protocol] || 'Unbekannt';
  }

  getCategoryLabel(category: string): string {
    const cat = this.categories.find(c => c.value === category);
    return cat?.label || category || '';
  }

  showPinConfig(protocol: CommunicationProtocol): boolean {
    return protocol !== CommunicationProtocol.I2C;
  }
}
