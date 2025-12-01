// ================================
// Sensor Form Component
// Bearbeiten/Erstellen von Sensoren
// Mit SensorType-Auswahl, Kalibrierung und Vererbung
// ================================

import { Component, OnInit, inject, signal, computed, effect } from '@angular/core';
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
import { SensorApiService, SensorTypeApiService } from '@myiotgrid/shared/data-access';
import {
  Sensor,
  SensorType,
  SensorTypeCapability,
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
  private readonly sensorTypeApiService = inject(SensorTypeApiService);
  private readonly snackBar = inject(MatSnackBar);

  readonly isLoading = signal(true);
  readonly isSaving = signal(false);
  readonly mode = signal<FormMode>('view');
  readonly sensor = signal<Sensor | null>(null);
  readonly sensorTypes = signal<SensorType[]>([]);
  readonly currentSensorTypeId = signal<string>('');

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

  // Der ausgewählte SensorType (mit allen Details)
  readonly selectedSensorType = computed(() => {
    const typeId = this.currentSensorTypeId();
    if (!typeId) return null;
    return this.sensorTypes().find(t => t.id === typeId) || null;
  });

  // Verfügbare Capabilities des ausgewählten SensorTypes
  readonly availableCapabilities = computed(() => {
    const type = this.selectedSensorType();
    if (!type) return [];
    return type.capabilities?.filter(c => c.isActive) || [];
  });

  // Protokoll-Label für Anzeige
  readonly protocolLabel = computed(() => {
    const type = this.selectedSensorType();
    if (!type) return '';
    return COMMUNICATION_PROTOCOL_LABELS[type.protocol] || 'Unbekannt';
  });

  form!: FormGroup;

  // Für die Chip-Auswahl der aktiven Capabilities
  selectedCapabilityIds: string[] = [];

  constructor() {
    // Effect um auf SensorType-Änderungen zu reagieren
    effect(() => {
      const type = this.selectedSensorType();
      if (type && this.isCreateMode()) {
        // Bei neuem Sensor: Alle Capabilities standardmäßig aktivieren
        this.selectedCapabilityIds = type.capabilities
          ?.filter(c => c.isActive)
          .map(c => c.id) || [];
      }
    });
  }

  ngOnInit(): void {
    this.initForm();
    this.loadSensorTypes();

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
      // Basic Information
      sensorTypeId: ['', [Validators.required]],
      name: ['', [Validators.required, Validators.minLength(2)]],
      description: [''],
      serialNumber: [''],

      // Intervall Override (null = vom SensorType erben)
      intervalSecondsOverride: [null],

      // Pin Configuration Override (null = vom SensorType erben)
      i2cAddressOverride: [null],
      sdaPinOverride: [null],
      sclPinOverride: [null],
      oneWirePinOverride: [null],
      analogPinOverride: [null],
      digitalPinOverride: [null],
      triggerPinOverride: [null],
      echoPinOverride: [null],

      // Kalibrierung (wird vom SensorType vererbt, kann überschrieben werden)
      offsetCorrection: [0],
      gainCorrection: [1.0],
      calibrationNotes: [''],

      // Status
      isActive: [true]
    });

    // Signal updaten wenn sich sensorTypeId ändert
    this.form.get('sensorTypeId')?.valueChanges.subscribe(value => {
      this.currentSensorTypeId.set(value || '');
    });
  }

  private loadSensorTypes(): void {
    this.sensorTypeApiService.getAll().subscribe({
      next: (types) => {
        this.sensorTypes.set(types);
      },
      error: (error) => {
        console.error('Error loading sensor types:', error);
        this.snackBar.open('Fehler beim Laden der Sensortypen', 'Schließen', { duration: 5000 });
      }
    });
  }

  private loadSensor(id: string): void {
    this.isLoading.set(true);
    this.sensorApiService.getById(id).subscribe({
      next: (sensor) => {
        this.sensor.set(sensor);
        this.patchForm(sensor);
        this.selectedCapabilityIds = sensor.activeCapabilityIds || [];
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
      sensorTypeId: sensor.sensorTypeId,
      name: sensor.name,
      description: sensor.description || '',
      serialNumber: sensor.serialNumber || '',
      intervalSecondsOverride: sensor.intervalSecondsOverride ?? null,
      // Pin Configuration Override
      i2cAddressOverride: sensor.i2cAddressOverride || null,
      sdaPinOverride: sensor.sdaPinOverride ?? null,
      sclPinOverride: sensor.sclPinOverride ?? null,
      oneWirePinOverride: sensor.oneWirePinOverride ?? null,
      analogPinOverride: sensor.analogPinOverride ?? null,
      digitalPinOverride: sensor.digitalPinOverride ?? null,
      triggerPinOverride: sensor.triggerPinOverride ?? null,
      echoPinOverride: sensor.echoPinOverride ?? null,
      // Calibration
      offsetCorrection: sensor.offsetCorrection ?? 0,
      gainCorrection: sensor.gainCorrection ?? 1.0,
      calibrationNotes: sensor.calibrationNotes || '',
      isActive: sensor.isActive ?? true
    });

    // Signal für SensorType setzen
    this.currentSensorTypeId.set(sensor.sensorTypeId);
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
        sensorTypeId: formValue.sensorTypeId,
        name: formValue.name,
        description: formValue.description || undefined,
        serialNumber: formValue.serialNumber || undefined,
        intervalSecondsOverride: formValue.intervalSecondsOverride || undefined,
        // Pin Configuration Override
        i2cAddressOverride: formValue.i2cAddressOverride || undefined,
        sdaPinOverride: formValue.sdaPinOverride ?? undefined,
        sclPinOverride: formValue.sclPinOverride ?? undefined,
        oneWirePinOverride: formValue.oneWirePinOverride ?? undefined,
        analogPinOverride: formValue.analogPinOverride ?? undefined,
        digitalPinOverride: formValue.digitalPinOverride ?? undefined,
        triggerPinOverride: formValue.triggerPinOverride ?? undefined,
        echoPinOverride: formValue.echoPinOverride ?? undefined,
        activeCapabilityIds: this.selectedCapabilityIds.length > 0 ? this.selectedCapabilityIds : undefined
      };

      this.sensorApiService.create(createDto).subscribe({
        next: () => {
          this.snackBar.open('Sensor erstellt', 'Schließen', { duration: 3000 });
          this.router.navigate(['/sensors']);
        },
        error: (error) => {
          console.error('Error creating sensor:', error);
          const message = error?.error?.detail || 'Fehler beim Erstellen des Sensors';
          this.snackBar.open(message, 'Schließen', { duration: 5000 });
          this.isSaving.set(false);
        }
      });
    } else {
      const updateDto: UpdateSensorDto = {
        name: formValue.name,
        description: formValue.description || undefined,
        serialNumber: formValue.serialNumber || undefined,
        intervalSecondsOverride: formValue.intervalSecondsOverride || undefined,
        // Pin Configuration Override
        i2cAddressOverride: formValue.i2cAddressOverride || undefined,
        sdaPinOverride: formValue.sdaPinOverride ?? undefined,
        sclPinOverride: formValue.sclPinOverride ?? undefined,
        oneWirePinOverride: formValue.oneWirePinOverride ?? undefined,
        analogPinOverride: formValue.analogPinOverride ?? undefined,
        digitalPinOverride: formValue.digitalPinOverride ?? undefined,
        triggerPinOverride: formValue.triggerPinOverride ?? undefined,
        echoPinOverride: formValue.echoPinOverride ?? undefined,
        activeCapabilityIds: this.selectedCapabilityIds.length > 0 ? this.selectedCapabilityIds : undefined,
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
            const message = error?.error?.detail || 'Fehler beim Aktualisieren des Sensors';
            this.snackBar.open(message, 'Schließen', { duration: 5000 });
            this.isSaving.set(false);
          }
        });
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
      const s = this.sensor();
      if (s) {
        this.patchForm(s);
        this.selectedCapabilityIds = s.activeCapabilityIds || [];
      }
    }
  }

  onCancel(): void {
    this.router.navigate(['/sensors']);
  }

  onBack(): void {
    this.router.navigate(['/sensors']);
  }

  // ===== Capability Selection =====

  toggleCapability(capabilityId: string): void {
    if (this.isViewMode()) return;

    const index = this.selectedCapabilityIds.indexOf(capabilityId);
    if (index >= 0) {
      this.selectedCapabilityIds = this.selectedCapabilityIds.filter(id => id !== capabilityId);
    } else {
      this.selectedCapabilityIds = [...this.selectedCapabilityIds, capabilityId];
    }
  }

  isCapabilitySelected(capabilityId: string): boolean {
    return this.selectedCapabilityIds.includes(capabilityId);
  }

  // ===== Helper Methoden für SensorType-Vererbung =====

  getSensorTypeIcon(typeId: string): string {
    const type = this.sensorTypes().find(t => t.id === typeId);
    return type?.icon || 'sensors';
  }

  getSensorTypeColor(typeId: string): string {
    const type = this.sensorTypes().find(t => t.id === typeId);
    return type?.color || '#607d8b';
  }

  getSensorTypeName(typeId: string): string {
    const type = this.sensorTypes().find(t => t.id === typeId);
    return type?.name || '';
  }

  /**
   * Gibt den effektiven Wert zurück (Override oder Default vom SensorType)
   */
  getEffectiveInterval(): number {
    const override = this.form?.get('intervalSecondsOverride')?.value;
    if (override !== null && override !== undefined && override !== '') {
      return override;
    }
    const type = this.selectedSensorType();
    return type?.defaultIntervalSeconds || 60;
  }

  getEffectiveOffset(): number {
    const override = this.form?.get('offsetCorrection')?.value;
    if (override !== null && override !== undefined && override !== 0) {
      return override;
    }
    const type = this.selectedSensorType();
    return type?.defaultOffsetCorrection || 0;
  }

  getEffectiveGain(): number {
    const override = this.form?.get('gainCorrection')?.value;
    if (override !== null && override !== undefined && override !== 1.0) {
      return override;
    }
    const type = this.selectedSensorType();
    return type?.defaultGainCorrection || 1.0;
  }

  /**
   * Placeholder-Text für Override-Felder
   */
  getIntervalPlaceholder(): string {
    const type = this.selectedSensorType();
    if (!type) return 'Wähle zuerst einen Sensortyp';
    return `Standard: ${type.defaultIntervalSeconds}s`;
  }

  getOffsetPlaceholder(): string {
    const type = this.selectedSensorType();
    if (!type) return 'Wähle zuerst einen Sensortyp';
    return `Standard: ${type.defaultOffsetCorrection}`;
  }

  getGainPlaceholder(): string {
    const type = this.selectedSensorType();
    if (!type) return 'Wähle zuerst einen Sensortyp';
    return `Standard: ${type.defaultGainCorrection}`;
  }

  /**
   * Zeigt Hinweis ob Wert überschrieben oder vererbt wird
   */
  isIntervalOverridden(): boolean {
    const override = this.form?.get('intervalSecondsOverride')?.value;
    return override !== null && override !== undefined && override !== '';
  }

  isOffsetOverridden(): boolean {
    const override = this.form?.get('offsetCorrection')?.value;
    return override !== null && override !== undefined && override !== 0;
  }

  isGainOverridden(): boolean {
    const override = this.form?.get('gainCorrection')?.value;
    return override !== null && override !== undefined && override !== 1.0;
  }

  /**
   * Setzt Override-Wert zurück auf den SensorType-Default
   */
  resetInterval(): void {
    this.form.get('intervalSecondsOverride')?.setValue(null);
  }

  resetOffset(): void {
    const type = this.selectedSensorType();
    this.form.get('offsetCorrection')?.setValue(type?.defaultOffsetCorrection || 0);
  }

  resetGain(): void {
    const type = this.selectedSensorType();
    this.form.get('gainCorrection')?.setValue(type?.defaultGainCorrection || 1.0);
  }

  // ===== Pin-Konfiguration =====

  /**
   * Zeigt Pin-Konfiguration basierend auf Protokoll
   */
  showI2CPins(): boolean {
    return this.selectedSensorType()?.protocol === CommunicationProtocol.I2C;
  }

  showOneWirePin(): boolean {
    return this.selectedSensorType()?.protocol === CommunicationProtocol.OneWire;
  }

  showAnalogPin(): boolean {
    return this.selectedSensorType()?.protocol === CommunicationProtocol.Analog;
  }

  showDigitalPin(): boolean {
    return this.selectedSensorType()?.protocol === CommunicationProtocol.Digital;
  }

  showUltraSonicPins(): boolean {
    return this.selectedSensorType()?.protocol === CommunicationProtocol.UltraSonic;
  }

  /**
   * Prüft ob überhaupt Pin-Konfiguration angezeigt werden soll
   * Zeigt Sektion immer wenn ein SensorType ausgewählt ist
   */
  showPinConfiguration(): boolean {
    return this.selectedSensorType() !== null;
  }

  /**
   * Prüft ob spezifische Pin-Felder vorhanden sind
   */
  hasPinFields(): boolean {
    return this.showI2CPins() || this.showOneWirePin() || this.showAnalogPin() ||
           this.showDigitalPin() || this.showUltraSonicPins();
  }

  // ===== I2C Pin Helper =====

  getEffectiveI2CAddress(): string {
    const override = this.form?.get('i2cAddressOverride')?.value;
    if (override) return override;
    return this.selectedSensorType()?.defaultI2CAddress || '0x76';
  }

  getI2CAddressPlaceholder(): string {
    const type = this.selectedSensorType();
    if (!type) return 'Wähle zuerst einen Sensortyp';
    return `Standard: ${type.defaultI2CAddress || '0x76'}`;
  }

  isI2CAddressOverridden(): boolean {
    const override = this.form?.get('i2cAddressOverride')?.value;
    return override !== null && override !== undefined && override !== '';
  }

  resetI2CAddress(): void {
    this.form.get('i2cAddressOverride')?.setValue(null);
  }

  getEffectiveSdaPin(): number {
    const override = this.form?.get('sdaPinOverride')?.value;
    if (override !== null && override !== undefined) return override;
    return this.selectedSensorType()?.defaultSdaPin ?? 21;
  }

  getSdaPinPlaceholder(): string {
    const type = this.selectedSensorType();
    if (!type) return 'Wähle zuerst einen Sensortyp';
    return `Standard: GPIO ${type.defaultSdaPin ?? 21}`;
  }

  isSdaPinOverridden(): boolean {
    const override = this.form?.get('sdaPinOverride')?.value;
    return override !== null && override !== undefined;
  }

  resetSdaPin(): void {
    this.form.get('sdaPinOverride')?.setValue(null);
  }

  getEffectiveSclPin(): number {
    const override = this.form?.get('sclPinOverride')?.value;
    if (override !== null && override !== undefined) return override;
    return this.selectedSensorType()?.defaultSclPin ?? 22;
  }

  getSclPinPlaceholder(): string {
    const type = this.selectedSensorType();
    if (!type) return 'Wähle zuerst einen Sensortyp';
    return `Standard: GPIO ${type.defaultSclPin ?? 22}`;
  }

  isSclPinOverridden(): boolean {
    const override = this.form?.get('sclPinOverride')?.value;
    return override !== null && override !== undefined;
  }

  resetSclPin(): void {
    this.form.get('sclPinOverride')?.setValue(null);
  }

  // ===== OneWire Pin Helper =====

  getEffectiveOneWirePin(): number {
    const override = this.form?.get('oneWirePinOverride')?.value;
    if (override !== null && override !== undefined) return override;
    return this.selectedSensorType()?.defaultOneWirePin ?? 4;
  }

  getOneWirePinPlaceholder(): string {
    const type = this.selectedSensorType();
    if (!type) return 'Wähle zuerst einen Sensortyp';
    return `Standard: GPIO ${type.defaultOneWirePin ?? 4}`;
  }

  isOneWirePinOverridden(): boolean {
    const override = this.form?.get('oneWirePinOverride')?.value;
    return override !== null && override !== undefined;
  }

  resetOneWirePin(): void {
    this.form.get('oneWirePinOverride')?.setValue(null);
  }

  // ===== Analog Pin Helper =====

  getEffectiveAnalogPin(): number {
    const override = this.form?.get('analogPinOverride')?.value;
    if (override !== null && override !== undefined) return override;
    return this.selectedSensorType()?.defaultAnalogPin ?? 34;
  }

  getAnalogPinPlaceholder(): string {
    const type = this.selectedSensorType();
    if (!type) return 'Wähle zuerst einen Sensortyp';
    return `Standard: GPIO ${type.defaultAnalogPin ?? 34}`;
  }

  isAnalogPinOverridden(): boolean {
    const override = this.form?.get('analogPinOverride')?.value;
    return override !== null && override !== undefined;
  }

  resetAnalogPin(): void {
    this.form.get('analogPinOverride')?.setValue(null);
  }

  // ===== Digital Pin Helper =====

  getEffectiveDigitalPin(): number {
    const override = this.form?.get('digitalPinOverride')?.value;
    if (override !== null && override !== undefined) return override;
    return this.selectedSensorType()?.defaultDigitalPin ?? 5;
  }

  getDigitalPinPlaceholder(): string {
    const type = this.selectedSensorType();
    if (!type) return 'Wähle zuerst einen Sensortyp';
    return `Standard: GPIO ${type.defaultDigitalPin ?? 5}`;
  }

  isDigitalPinOverridden(): boolean {
    const override = this.form?.get('digitalPinOverride')?.value;
    return override !== null && override !== undefined;
  }

  resetDigitalPin(): void {
    this.form.get('digitalPinOverride')?.setValue(null);
  }

  // ===== UltraSonic Pin Helper =====

  getEffectiveTriggerPin(): number {
    const override = this.form?.get('triggerPinOverride')?.value;
    if (override !== null && override !== undefined) return override;
    return this.selectedSensorType()?.defaultTriggerPin ?? 12;
  }

  getTriggerPinPlaceholder(): string {
    const type = this.selectedSensorType();
    if (!type) return 'Wähle zuerst einen Sensortyp';
    return `Standard: GPIO ${type.defaultTriggerPin ?? 12}`;
  }

  isTriggerPinOverridden(): boolean {
    const override = this.form?.get('triggerPinOverride')?.value;
    return override !== null && override !== undefined;
  }

  resetTriggerPin(): void {
    this.form.get('triggerPinOverride')?.setValue(null);
  }

  getEffectiveEchoPin(): number {
    const override = this.form?.get('echoPinOverride')?.value;
    if (override !== null && override !== undefined) return override;
    return this.selectedSensorType()?.defaultEchoPin ?? 14;
  }

  getEchoPinPlaceholder(): string {
    const type = this.selectedSensorType();
    if (!type) return 'Wähle zuerst einen Sensortyp';
    return `Standard: GPIO ${type.defaultEchoPin ?? 14}`;
  }

  isEchoPinOverridden(): boolean {
    const override = this.form?.get('echoPinOverride')?.value;
    return override !== null && override !== undefined;
  }

  resetEchoPin(): void {
    this.form.get('echoPinOverride')?.setValue(null);
  }
}
