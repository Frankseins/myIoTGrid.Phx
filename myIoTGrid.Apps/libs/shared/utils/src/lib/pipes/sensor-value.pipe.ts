import { Pipe, PipeTransform } from '@angular/core';

/**
 * Pipe to format sensor values based on measurement type.
 * GPS coordinates (latitude, longitude) are formatted with 4 decimal places.
 * Integer types (co2, pm25, pm10, rssi) are formatted as whole numbers.
 * All other values are formatted with 1 decimal place.
 */
@Pipe({
  name: 'sensorValue',
  standalone: true
})
export class SensorValuePipe implements PipeTransform {
  transform(value: number | null | undefined, measurementType?: string): string {
    if (value === null || value === undefined || Number.isNaN(value)) {
      return '--';
    }

    const type = (measurementType || '').toLowerCase();

    // GPS coordinates need 4 decimal places for precision
    if (type === 'latitude' || type === 'longitude') {
      return value.toFixed(4);
    }

    // Integer values (no decimals)
    if (['co2', 'pm25', 'pm10', 'rssi'].includes(type)) {
      return Math.round(value).toString();
    }

    // Default to 1 decimal for all other sensor values
    return value.toFixed(1);
  }
}
