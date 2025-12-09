import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'sensorUnit',
  standalone: true
})
export class SensorUnitPipe implements PipeTransform {
  transform(value: number | null | undefined, unit: string, decimals: number = 1): string {
    if (value === null || value === undefined) return '–';

    const formattedValue = value.toFixed(decimals);
    return `${formattedValue} ${unit}`;
  }
}
