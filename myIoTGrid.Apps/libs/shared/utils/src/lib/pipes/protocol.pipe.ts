import { Pipe, PipeTransform } from '@angular/core';
import { Protocol } from '@myiotgrid/shared/models';

@Pipe({
  name: 'protocol',
  standalone: true
})
export class ProtocolPipe implements PipeTransform {
  transform(value: Protocol): string {
    switch (value) {
      case Protocol.WLAN:
        return 'WLAN';
      case Protocol.LoRaWAN:
        return 'LoRaWAN';
      case Protocol.Unknown:
      default:
        return 'Unbekannt';
    }
  }
}
