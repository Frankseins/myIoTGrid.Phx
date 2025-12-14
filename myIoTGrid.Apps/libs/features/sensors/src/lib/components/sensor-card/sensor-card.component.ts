// ================================
// Sensor Card Component
// Expandierbare Karte für Mobile-Ansicht
// ================================

import { Component, Input, Output, EventEmitter, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatButtonModule } from '@angular/material/button';
import { MatRippleModule } from '@angular/material/core';

import { Sensor, COMMUNICATION_PROTOCOL_LABELS, CommunicationProtocol } from '@myiotgrid/shared/models';

@Component({
  selector: 'myiotgrid-sensor-card',
  standalone: true,
  imports: [
    CommonModule,
    MatIconModule,
    MatChipsModule,
    MatButtonModule,
    MatRippleModule
  ],
  templateUrl: './sensor-card.component.html',
  styleUrls: ['./sensor-card.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SensorCardComponent {
  @Input() sensor!: Sensor;
  @Input() expanded = false;

  @Output() edit = new EventEmitter<Sensor>();
  @Output() delete = new EventEmitter<Sensor>();
  @Output() expandChange = new EventEmitter<boolean>();

  toggle(): void {
    this.expanded = !this.expanded;
    this.expandChange.emit(this.expanded);
  }

  onEdit(event: Event): void {
    event.stopPropagation();
    this.edit.emit(this.sensor);
  }

  onDelete(event: Event): void {
    event.stopPropagation();
    this.delete.emit(this.sensor);
  }

  getSensorIcon(): string {
    return this.sensor.icon || 'sensors';
  }

  getSensorColor(): string {
    return this.sensor.color || '#607d8b';
  }

  getCategoryLabel(): string {
    const labels: Record<string, string> = {
      climate: 'Klima',
      water: 'Wasser',
      location: 'Standort',
      custom: 'Benutzerdefiniert'
    };
    return labels[this.sensor.category] || this.sensor.category || 'Unbekannt';
  }

  getCategoryColor(): string {
    const colors: Record<string, string> = {
      climate: '#FFC107',
      water: '#2196F3',
      location: '#4CAF50',
      custom: '#9E9E9E'
    };
    return colors[this.sensor.category] || '#9E9E9E';
  }

  getProtocolLabel(): string {
    return COMMUNICATION_PROTOCOL_LABELS[this.sensor.protocol as CommunicationProtocol] || 'Unbekannt';
  }

  getCapabilitiesCount(): number {
    return this.sensor.capabilities?.length || 0;
  }
}
