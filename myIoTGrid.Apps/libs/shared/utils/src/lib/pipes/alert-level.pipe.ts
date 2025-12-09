import { Pipe, PipeTransform } from '@angular/core';
import { AlertLevel } from '@myiotgrid/shared/models';

@Pipe({
  name: 'alertLevel',
  standalone: true
})
export class AlertLevelPipe implements PipeTransform {
  transform(value: AlertLevel): string {
    switch (value) {
      case AlertLevel.Ok:
        return 'OK';
      case AlertLevel.Info:
        return 'Info';
      case AlertLevel.Warning:
        return 'Warnung';
      case AlertLevel.Critical:
        return 'Kritisch';
      default:
        return 'Unbekannt';
    }
  }
}
