// ================================
// Sensor Form Component (v3.0 Two-Tier Model)
// Erstellen/Bearbeiten von Sensoren
// Alle Eigenschaften sind direkt im Sensor (kein SensorType mehr)
// ================================

import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormGroup, FormArray, Validators, ReactiveFormsModule } from '@angular/forms';
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
  UpdateSensorCapabilityDto,
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

  // Measurement Type options for capabilities
  readonly measurementTypeOptions = [
    { value: 'temperature', label: 'Temperatur', unit: '°C', matterClusterId: 1026, matterClusterName: 'TemperatureMeasurement' },
    { value: 'humidity', label: 'Luftfeuchtigkeit', unit: '%', matterClusterId: 1029, matterClusterName: 'RelativeHumidityMeasurement' },
    { value: 'pressure', label: 'Luftdruck', unit: 'hPa', matterClusterId: 1027, matterClusterName: 'PressureMeasurement' },
    { value: 'illuminance', label: 'Helligkeit', unit: 'lux', matterClusterId: 1024, matterClusterName: 'IlluminanceMeasurement' },
    { value: 'co2', label: 'CO₂', unit: 'ppm', matterClusterId: 64516, matterClusterName: 'CarbonDioxideMeasurement' },
    { value: 'pm25', label: 'Feinstaub PM2.5', unit: 'µg/m³', matterClusterId: 64517, matterClusterName: 'PM25Measurement' },
    { value: 'pm10', label: 'Feinstaub PM10', unit: 'µg/m³', matterClusterId: 64518, matterClusterName: 'PM10Measurement' },
    { value: 'water_temperature', label: 'Wassertemperatur', unit: '°C', matterClusterId: 64513, matterClusterName: 'WaterTemperature' },
    { value: 'water_level', label: 'Wasserstand', unit: 'cm', matterClusterId: 64512, matterClusterName: 'WaterLevel' },
    { value: 'soil_moisture', label: 'Bodenfeuchtigkeit', unit: '%', matterClusterId: 64519, matterClusterName: 'SoilMoisture' },
    { value: 'uv_index', label: 'UV-Index', unit: 'index', matterClusterId: 64520, matterClusterName: 'UVIndex' },
    { value: 'wind_speed', label: 'Windgeschwindigkeit', unit: 'm/s', matterClusterId: 64521, matterClusterName: 'WindSpeed' },
    { value: 'rainfall', label: 'Niederschlag', unit: 'mm', matterClusterId: 64522, matterClusterName: 'Rainfall' },
    { value: 'battery', label: 'Batterie', unit: '%', matterClusterId: 64523, matterClusterName: 'BatteryLevel' },
    { value: 'rssi', label: 'Signalstärke', unit: 'dBm', matterClusterId: 64524, matterClusterName: 'RSSI' },
    { value: 'latitude', label: 'Breitengrad', unit: '°', matterClusterId: null, matterClusterName: null },
    { value: 'longitude', label: 'Längengrad', unit: '°', matterClusterId: null, matterClusterName: null },
    { value: 'altitude', label: 'Höhe', unit: 'm', matterClusterId: null, matterClusterName: null },
    { value: 'speed', label: 'Geschwindigkeit', unit: 'km/h', matterClusterId: null, matterClusterName: null },
    { value: 'distance', label: 'Entfernung', unit: 'cm', matterClusterId: null, matterClusterName: null },
    { value: 'ph', label: 'pH-Wert', unit: 'pH', matterClusterId: 64514, matterClusterName: 'PHValue' },
    { value: 'custom', label: 'Benutzerdefiniert', unit: '', matterClusterId: null, matterClusterName: null }
  ];

  // Unit options for capabilities
  readonly unitOptions = [
    { value: '°C', label: '°C (Celsius)' },
    { value: '°F', label: '°F (Fahrenheit)' },
    { value: 'K', label: 'K (Kelvin)' },
    { value: '%', label: '% (Prozent)' },
    { value: 'hPa', label: 'hPa (Hektopascal)' },
    { value: 'mbar', label: 'mbar (Millibar)' },
    { value: 'lux', label: 'lux (Lux)' },
    { value: 'ppm', label: 'ppm (Parts per Million)' },
    { value: 'µg/m³', label: 'µg/m³ (Mikrogramm pro m³)' },
    { value: 'cm', label: 'cm (Zentimeter)' },
    { value: 'm', label: 'm (Meter)' },
    { value: 'mm', label: 'mm (Millimeter)' },
    { value: 'm/s', label: 'm/s (Meter pro Sekunde)' },
    { value: 'km/h', label: 'km/h (Kilometer pro Stunde)' },
    { value: 'dBm', label: 'dBm (Dezibel-Milliwatt)' },
    { value: 'V', label: 'V (Volt)' },
    { value: 'mV', label: 'mV (Millivolt)' },
    { value: 'A', label: 'A (Ampere)' },
    { value: 'mA', label: 'mA (Milliampere)' },
    { value: 'W', label: 'W (Watt)' },
    { value: 'kWh', label: 'kWh (Kilowattstunde)' },
    { value: '°', label: '° (Grad)' },
    { value: 'pH', label: 'pH' },
    { value: 'index', label: 'Index' },
    { value: 'count', label: 'Anzahl' }
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

  // Capabilities FormArray getter
  get capabilitiesFormArray(): FormArray {
    return this.form.get('capabilities') as FormArray;
  }

  ngOnInit(): void {
    this.initForm();

    const id = this.route.snapshot.paramMap.get('id');
    const url = this.route.snapshot.url.map(s => s.path).join('/');
    const queryMode = this.route.snapshot.queryParamMap.get('mode');

    // Check if this is the 'new' route (no :id parameter)
    if (!id || id === 'new' || url.includes('new')) {
      this.mode.set('create');
      this.isLoading.set(false);
    } else if (id) {
      // Check if editing (route ends with /edit or query param mode=edit)
      const isEditRoute = url.endsWith('edit') || queryMode === 'edit';
      this.mode.set(isEditRoute ? 'edit' : 'view');
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

      // Hardware Configuration - UART
      baudRate: [null],

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
      isActive: [true],

      // Capabilities FormArray
      capabilities: this.fb.array([])
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
      baudRate: sensor.baudRate ?? null,

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

    // Load capabilities into FormArray
    console.log('[SensorForm] Loading capabilities:', sensor.capabilities);
    console.log('[SensorForm] Capabilities count:', sensor.capabilities?.length ?? 0);

    this.capabilitiesFormArray.clear();
    if (sensor.capabilities && sensor.capabilities.length > 0) {
      sensor.capabilities.forEach((cap, index) => {
        console.log(`[SensorForm] Adding capability ${index}:`, cap);
        this.capabilitiesFormArray.push(this.createCapabilityFormGroup(cap));
      });
    }

    console.log('[SensorForm] FormArray length after patch:', this.capabilitiesFormArray.length);
  }

  /**
   * Creates a FormGroup for a capability
   */
  private createCapabilityFormGroup(capability?: SensorCapability): FormGroup {
    return this.fb.group({
      id: [capability?.id || null],
      measurementType: [capability?.measurementType || '', [Validators.required]],
      displayName: [capability?.displayName || '', [Validators.required]],
      unit: [capability?.unit || '', [Validators.required]],
      minValue: [capability?.minValue ?? null],
      maxValue: [capability?.maxValue ?? null],
      resolution: [capability?.resolution ?? 0.01],
      accuracy: [capability?.accuracy ?? 0.5],
      matterClusterId: [capability?.matterClusterId ?? null],
      matterClusterName: [capability?.matterClusterName || ''],
      sortOrder: [capability?.sortOrder ?? 0],
      isActive: [capability?.isActive ?? true]
    });
  }

  /**
   * Adds a new capability to the FormArray
   */
  addCapability(): void {
    this.capabilitiesFormArray.push(this.createCapabilityFormGroup());
  }

  /**
   * Removes a capability from the FormArray
   */
  removeCapability(index: number): void {
    this.capabilitiesFormArray.removeAt(index);
  }

  /**
   * Called when measurement type changes - auto-fills related fields
   */
  onMeasurementTypeChange(index: number, measurementType: string): void {
    const option = this.measurementTypeOptions.find(o => o.value === measurementType);
    if (option && measurementType !== 'custom') {
      const capGroup = this.capabilitiesFormArray.at(index);
      capGroup.patchValue({
        displayName: option.label,
        unit: option.unit,
        matterClusterId: option.matterClusterId,
        matterClusterName: option.matterClusterName
      });
    }
  }

  /**
   * Gets the label for a measurement type value
   */
  getMeasurementTypeLabel(value: string): string {
    const option = this.measurementTypeOptions.find(o => o.value === value);
    return option?.label || value;
  }

  /**
   * Gets the label for a unit value
   */
  getUnitLabel(value: string): string {
    const option = this.unitOptions.find(o => o.value === value);
    return option?.label || value;
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
        baudRate: formValue.baudRate ?? undefined,

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
      // Build capabilities DTOs
      const capabilitiesDtos: UpdateSensorCapabilityDto[] = formValue.capabilities.map((cap: Record<string, unknown>) => ({
        id: cap['id'] || undefined,
        measurementType: cap['measurementType'] || undefined,
        displayName: cap['displayName'] || undefined,
        unit: cap['unit'] || undefined,
        minValue: cap['minValue'] ?? undefined,
        maxValue: cap['maxValue'] ?? undefined,
        resolution: cap['resolution'] ?? undefined,
        accuracy: cap['accuracy'] ?? undefined,
        matterClusterId: cap['matterClusterId'] ?? undefined,
        matterClusterName: cap['matterClusterName'] || undefined,
        sortOrder: cap['sortOrder'] ?? undefined,
        isActive: cap['isActive'] ?? true
      }));

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
        baudRate: formValue.baudRate ?? undefined,

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
        isActive: formValue.isActive,

        // Capabilities
        capabilities: capabilitiesDtos
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

  showUARTPins(): boolean {
    return this.selectedProtocol() === CommunicationProtocol.UART;
  }

  showPinConfiguration(): boolean {
    return this.selectedProtocol() !== null;
  }

  hasPinFields(): boolean {
    return this.showI2CPins() || this.showOneWirePin() || this.showAnalogPin() ||
           this.showDigitalPin() || this.showUltraSonicPins() || this.showUARTPins();
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
