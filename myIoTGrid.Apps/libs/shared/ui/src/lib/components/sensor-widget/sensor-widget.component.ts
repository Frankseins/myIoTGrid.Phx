import { Component, Input, Output, EventEmitter, computed, signal, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule, DecimalPipe, DatePipe } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { SensorWidget } from '@myiotgrid/shared/models';
import { SparklineChartComponent, SparklineSeries } from '../sparkline-chart/sparkline-chart.component';

@Component({
  selector: 'myiotgrid-sensor-widget',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatIconModule,
    SparklineChartComponent,
    DecimalPipe,
    DatePipe
  ],
  templateUrl: './sensor-widget.component.html',
  styleUrl: './sensor-widget.component.scss'
})
export class SensorWidgetComponent implements OnChanges {
  @Input({ required: true }) widget!: SensorWidget;
  @Input() isHero = false;
  @Output() widgetClick = new EventEmitter<SensorWidget>();

  onWidgetClick(): void {
    this.widgetClick.emit(this.widget);
  }

  private readonly _widget = signal<SensorWidget | null>(null);
  private readonly _isHero = signal(false);

  readonly sparklineSeries = computed((): SparklineSeries[] => {
    const widget = this._widget();
    if (!widget || !widget.dataPoints.length) return [];

    return [{
      data: widget.dataPoints,
      color: widget.color,
      label: widget.label
    }];
  });

  readonly sparklineHeight = computed(() => this._isHero() ? 60 : 40);

  readonly formattedValue = computed(() => {
    const widget = this._widget();
    if (!widget) return '0';

    // Format based on measurement type
    const value = widget.currentValue;
    if (Number.isNaN(value)) return 'NaN';

    // Use appropriate decimal places based on measurement type
    const oneDecimalTypes = [
      'temperature', 'water_temperature', 'humidity', 'pressure',
      'soil_moisture', 'battery', 'light', 'uv'
    ];
    if (oneDecimalTypes.includes(widget.measurementType.toLowerCase())) {
      return value.toFixed(1);
    }
    return value.toFixed(1); // Default to 1 decimal for all sensor values
  });

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['widget']) {
      this._widget.set(this.widget);
    }
    if (changes['isHero']) {
      this._isHero.set(this.isHero);
    }
  }

  getLocationIcon(): string {
    const widget = this._widget();
    return widget?.measurementType ? this.getMeasurementIcon(widget.measurementType) : 'sensors';
  }

  getMeasurementLabel(): string {
    const widget = this._widget();
    if (!widget?.measurementType) return '';
    return this.getMeasurementTypeLabel(widget.measurementType);
  }

  private getMeasurementTypeLabel(type: string): string {
    const labels: Record<string, string> = {
      temperature: 'Temperatur',
      water_temperature: 'Wassertemperatur',
      humidity: 'Luftfeuchtigkeit',
      pressure: 'Luftdruck',
      co2: 'CO2',
      pm25: 'Feinstaub PM2.5',
      pm10: 'Feinstaub PM10',
      soil_moisture: 'Bodenfeuchtigkeit',
      light: 'Helligkeit',
      illuminance: 'Helligkeit',
      uv: 'UV-Index',
      wind_speed: 'Windgeschwindigkeit',
      rainfall: 'Niederschlag',
      water_level: 'Wasserstand',
      battery: 'Batterie',
      rssi: 'Signalstärke',
      speed: 'Geschwindigkeit',
      latitude: 'Breitengrad',
      longitude: 'Längengrad',
      altitude: 'Höhe'
    };
    return labels[type.toLowerCase()] || type;
  }

  private getMeasurementIcon(type: string): string {
    const iconMap: Record<string, string> = {
      temperature: 'thermostat',
      water_temperature: 'thermostat',
      humidity: 'water_drop',
      pressure: 'speed',
      co2: 'air',
      pm25: 'blur_on',
      pm10: 'blur_on',
      soil_moisture: 'grass',
      light: 'light_mode',
      illuminance: 'light_mode',
      uv: 'wb_sunny',
      wind_speed: 'air',
      rainfall: 'water',
      water_level: 'waves',
      battery: 'battery_full',
      rssi: 'signal_cellular_alt',
      speed: 'speed',
      latitude: 'location_on',
      longitude: 'location_on',
      altitude: 'terrain'
    };
    return iconMap[type.toLowerCase()] || 'sensors';
  }
}
