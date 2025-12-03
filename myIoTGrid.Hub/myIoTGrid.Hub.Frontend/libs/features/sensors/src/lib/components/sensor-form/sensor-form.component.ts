// ================================
// Sensor Form Component (v3.0 Two-Tier Model)
// Erstellen/Bearbeiten von Sensoren
// Alle Eigenschaften sind direkt im Sensor (kein SensorType mehr)
// ================================

import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
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
import { MatChipsModule } from '@angular/material/chips';
import { SensorApiService } from '@myiotgrid/shared/data-access';
import {
  Sensor,
  SensorCapability,
  CreateSensorDto,
  UpdateSensorDto,
  CommunicationProtocol,
  COMMUNICATION_PROTOCOL_LABELS
} from '@myiotgrid/shared/models';
import { LoadingSpinnerComponent } from '@myiotgrid/shared/ui';

type FormMode = 'view' | 'edit' | 'create';

@Component({
  selector: 'myiotgrid-sensor-form',
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
    MatChipsModule,
    LoadingSpinnerComponent
  ],
  templateUrl: './sensor-form.component.html',
  styleUrl: './sensor-form.component.scss'
})
export class SensorFormComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly sensorApiService = inject(SensorApiService);
  private readonly snackBar = inject(MatSnackBar);

  readonly isLoading = signal(true);
  readonly isSaving = signal(false);
  readonly mode = signal<FormMode>('view');
  readonly sensor = signal<Sensor | null>(null);

  readonly isViewMode = computed(() => this.mode() === 'view');
  readonly isEditMode = computed(() => this.mode() === 'edit');
  readonly isCreateMode = computed(() => this.mode() === 'create');

  readonly pageTitle = computed(() => {
    switch (this.mode()) {
      case 'create': return 'Neuer Sensor';
      case 'edit': return 'Sensor bearbeiten';
      default: return 'Sensor Details';
    }
  });

  // Protocol options for select
  readonly protocolOptions = Object.entries(CommunicationProtocol)
    .filter(([key]) => isNaN(Number(key)))
    .map(([key, value]) => ({
      value: value as CommunicationProtocol,
      label: COMMUNICATION_PROTOCOL_LABELS[value as CommunicationProtocol] || key
    }));

  // Category options
  readonly categoryOptions = [
    { value: 'climate', label: 'Klima (Temperatur, Luftfeuchtigkeit)' },
    { value: 'water', label: 'Wasser (Füllstand, Temperatur)' },
    { value: 'air', label: 'Luft (CO2, Feinstaub)' },
    { value: 'light', label: 'Licht (Helligkeit, UV)' },
    { value: 'motion', label: 'Bewegung (PIR, Radar)' },
    { value: 'distance', label: 'Entfernung (Ultraschall)' },
    { value: 'soil', label: 'Boden (Feuchtigkeit)' },
    { value: 'custom', label: 'Benutzerdefiniert' }
  ];

  // Selected protocol for showing correct pin fields
  readonly selectedProtocol = signal<CommunicationProtocol | null>(null);

  // Protocol label for display
  readonly protocolLabel = computed(() => {
    const protocol = this.selectedProtocol();
    if (protocol === null) return '';
    return COMMUNICATION_PROTOCOL_LABELS[protocol] || 'Unbekannt';
  });

  form!: FormGroup;

  ngOnInit(): void {
    this.initForm();

    const id = this.route.snapshot.paramMap.get('id');
    const queryMode = this.route.snapshot.queryParamMap.get('mode');

    if (id === 'new') {
      this.mode.set('create');
      this.isLoading.set(false);
    } else if (id) {
      this.mode.set(queryMode === 'edit' ? 'edit' : 'view');
      this.loadSensor(id);
    }
  }

  private initForm(): void {
    this.form = this.fb.group({
      // Core Properties
      code: ['', [Validators.required, Validators.minLength(2), Validators.pattern(/^[a-z0-9-]+$/)]],
      name: ['', [Validators.required, Validators.minLength(2)]],
      protocol: [CommunicationProtocol.I2C, [Validators.required]],
      category: ['climate'],
      manufacturer: [''],
      model: [''],
      description: [''],
      serialNumber: [''],

      // Hardware Configuration - I2C
      i2cAddress: [''],
      sdaPin: [null],
      sclPin: [null],

      // Hardware Configuration - OneWire
      oneWirePin: [null],

      // Hardware Configuration - Analog
      analogPin: [null],

      // Hardware Configuration - Digital
      digitalPin: [null],

      // Hardware Configuration - UltraSonic
      triggerPin: [null],
      echoPin: [null],

      // Timing Configuration
      intervalSeconds: [60, [Validators.required, Validators.min(1)]],
      minIntervalSeconds: [1, [Validators.required, Validators.min(1)]],
      warmupTimeMs: [0],

      // Calibration
      offsetCorrection: [0],
      gainCorrection: [1.0],
      calibrationNotes: [''],

      // Metadata
      icon: ['sensors'],
      color: ['#607d8b'],
      datasheetUrl: [''],

      // Status
      isActive: [true]
    });

    // Update selectedProtocol when protocol changes
    this.form.get('protocol')?.valueChanges.subscribe(value => {
      this.selectedProtocol.set(value);
    });

    // Set initial protocol
    this.selectedProtocol.set(this.form.get('protocol')?.value);
  }

  private loadSensor(id: string): void {
    this.isLoading.set(true);
    this.sensorApiService.getById(id).subscribe({
      next: (sensor) => {
        this.sensor.set(sensor);
        this.patchForm(sensor);
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Error loading sensor:', error);
        this.snackBar.open('Fehler beim Laden des Sensors', 'Schließen', { duration: 5000 });
        this.router.navigate(['/sensors']);
      }
    });
  }

  private patchForm(sensor: Sensor): void {
    this.form.patchValue({
      code: sensor.code,
      name: sensor.name,
      protocol: sensor.protocol,
      category: sensor.category || 'climate',
      manufacturer: sensor.manufacturer || '',
      model: sensor.model || '',
      description: sensor.description || '',
      serialNumber: sensor.serialNumber || '',

      // Hardware Configuration
      i2cAddress: sensor.i2cAddress || '',
      sdaPin: sensor.sdaPin ?? null,
      sclPin: sensor.sclPin ?? null,
      oneWirePin: sensor.oneWirePin ?? null,
      analogPin: sensor.analogPin ?? null,
      digitalPin: sensor.digitalPin ?? null,
      triggerPin: sensor.triggerPin ?? null,
      echoPin: sensor.echoPin ?? null,

      // Timing
      intervalSeconds: sensor.intervalSeconds ?? 60,
      minIntervalSeconds: sensor.minIntervalSeconds ?? 1,
      warmupTimeMs: sensor.warmupTimeMs ?? 0,

      // Calibration
      offsetCorrection: sensor.offsetCorrection ?? 0,
      gainCorrection: sensor.gainCorrection ?? 1.0,
      calibrationNotes: sensor.calibrationNotes || '',

      // Metadata
      icon: sensor.icon || 'sensors',
      color: sensor.color || '#607d8b',
      datasheetUrl: sensor.datasheetUrl || '',

      // Status
      isActive: sensor.isActive ?? true
    });

    this.selectedProtocol.set(sensor.protocol);
  }

  onSave(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSaving.set(true);
    const formValue = this.form.getRawValue();

    if (this.isCreateMode()) {
      const createDto: CreateSensorDto = {
        code: formValue.code,
        name: formValue.name,
        protocol: formValue.protocol,
        category: formValue.category || undefined,
        manufacturer: formValue.manufacturer || undefined,
        model: formValue.model || undefined,
        description: formValue.description || undefined,
        serialNumber: formValue.serialNumber || undefined,

        // Hardware Configuration
        i2cAddress: formValue.i2cAddress || undefined,
        sdaPin: formValue.sdaPin ?? undefined,
        sclPin: formValue.sclPin ?? undefined,
        oneWirePin: formValue.oneWirePin ?? undefined,
        analogPin: formValue.analogPin ?? undefined,
        digitalPin: formValue.digitalPin ?? undefined,
        triggerPin: formValue.triggerPin ?? undefined,
        echoPin: formValue.echoPin ?? undefined,

        // Timing
        intervalSeconds: formValue.intervalSeconds ?? undefined,
        minIntervalSeconds: formValue.minIntervalSeconds ?? undefined,
        warmupTimeMs: formValue.warmupTimeMs ?? undefined,

        // Calibration
        offsetCorrection: formValue.offsetCorrection ?? undefined,
        gainCorrection: formValue.gainCorrection ?? undefined,

        // Metadata
        icon: formValue.icon || undefined,
        color: formValue.color || undefined,
        datasheetUrl: formValue.datasheetUrl || undefined
      };

      this.sensorApiService.create(createDto).subscribe({
        next: () => {
          this.snackBar.open('Sensor erstellt', 'Schließen', { duration: 3000 });
          this.router.navigate(['/sensors']);
        },
        error: (error) => {
          console.error('Error creating sensor:', error);
          const message = error?.error?.detail || error?.error?.title || 'Fehler beim Erstellen des Sensors';
          this.snackBar.open(message, 'Schließen', { duration: 5000 });
          this.isSaving.set(false);
        }
      });
    } else {
      const updateDto: UpdateSensorDto = {
        name: formValue.name,
        manufacturer: formValue.manufacturer || undefined,
        model: formValue.model || undefined,
        description: formValue.description || undefined,
        serialNumber: formValue.serialNumber || undefined,

        // Hardware Configuration
        i2cAddress: formValue.i2cAddress || undefined,
        sdaPin: formValue.sdaPin ?? undefined,
        sclPin: formValue.sclPin ?? undefined,
        oneWirePin: formValue.oneWirePin ?? undefined,
        analogPin: formValue.analogPin ?? undefined,
        digitalPin: formValue.digitalPin ?? undefined,
        triggerPin: formValue.triggerPin ?? undefined,
        echoPin: formValue.echoPin ?? undefined,

        // Timing
        intervalSeconds: formValue.intervalSeconds ?? undefined,
        minIntervalSeconds: formValue.minIntervalSeconds ?? undefined,
        warmupTimeMs: formValue.warmupTimeMs ?? undefined,

        // Calibration
        offsetCorrection: formValue.offsetCorrection ?? undefined,
        gainCorrection: formValue.gainCorrection ?? undefined,
        calibrationNotes: formValue.calibrationNotes || undefined,

        // Metadata
        icon: formValue.icon || undefined,
        color: formValue.color || undefined,
        datasheetUrl: formValue.datasheetUrl || undefined,

        // Status
        isActive: formValue.isActive
      };

      const currentId = this.sensor()?.id;
      if (currentId) {
        this.sensorApiService.update(currentId, updateDto).subscribe({
          next: () => {
            this.snackBar.open('Sensor aktualisiert', 'Schließen', { duration: 3000 });
            this.router.navigate(['/sensors']);
          },
          error: (error) => {
            console.error('Error updating sensor:', error);
            const message = error?.error?.detail || error?.error?.title || 'Fehler beim Aktualisieren des Sensors';
            this.snackBar.open(message, 'Schließen', { duration: 5000 });
            this.isSaving.set(false);
          }
        });
      }
    }
  }

  toggleEditMode(): void {
    if (this.isViewMode()) {
      this.mode.set('edit');
    } else {
      this.mode.set('view');
      const s = this.sensor();
      if (s) {
        this.patchForm(s);
      }
    }
  }

  onCancel(): void {
    this.router.navigate(['/sensors']);
  }

  onBack(): void {
    this.router.navigate(['/sensors']);
  }

  // ===== Pin Configuration Visibility =====

  showI2CPins(): boolean {
    return this.selectedProtocol() === CommunicationProtocol.I2C;
  }

  showOneWirePin(): boolean {
    return this.selectedProtocol() === CommunicationProtocol.OneWire;
  }

  showAnalogPin(): boolean {
    return this.selectedProtocol() === CommunicationProtocol.Analog;
  }

  showDigitalPin(): boolean {
    return this.selectedProtocol() === CommunicationProtocol.Digital;
  }

  showUltraSonicPins(): boolean {
    return this.selectedProtocol() === CommunicationProtocol.UltraSonic;
  }

  showPinConfiguration(): boolean {
    return this.selectedProtocol() !== null;
  }

  hasPinFields(): boolean {
    return this.showI2CPins() || this.showOneWirePin() || this.showAnalogPin() ||
           this.showDigitalPin() || this.showUltraSonicPins();
  }

  // ===== Helper Methods =====

  getIcon(): string {
    return this.form?.get('icon')?.value || 'sensors';
  }

  getColor(): string {
    return this.form?.get('color')?.value || '#607d8b';
  }

  getCalibrationFormula(): string {
    const gain = this.form?.get('gainCorrection')?.value ?? 1.0;
    const offset = this.form?.get('offsetCorrection')?.value ?? 0;
    return `(Rohwert × ${gain}) + ${offset}`;
  }

  // Capabilities display (read-only from sensor)
  getCapabilities(): SensorCapability[] {
    return this.sensor()?.capabilities || [];
  }
}
