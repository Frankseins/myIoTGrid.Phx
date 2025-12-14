// ================================
// Reading Card Component
// Mobile-optimierte Card für einzelne Messwerte
// ================================

import { Component, Input, Output, EventEmitter, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatRippleModule } from '@angular/material/core';

import { ReadingListItem } from '@myiotgrid/shared/models';

@Component({
  selector: 'myiotgrid-reading-card',
  standalone: true,
  imports: [
    CommonModule,
    MatIconModule,
    MatRippleModule,
    DecimalPipe
  ],
  templateUrl: './reading-card.component.html',
  styleUrls: ['./reading-card.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ReadingCardComponent {
  @Input() reading!: ReadingListItem;
  @Input() measurementLabel = '';
  @Input() measurementIcon = 'sensors';
  @Input() color = '#1976d2';
  @Input() sensorName = '';
  @Input() nodeName = '';
  @Input() expanded = false;

  @Output() expandChange = new EventEmitter<boolean>();

  toggle(): void {
    this.expanded = !this.expanded;
    this.expandChange.emit(this.expanded);
  }

  getTimeDisplay(): string {
    const date = new Date(this.reading.timestamp);
    return date.toLocaleTimeString('de-DE', { hour: '2-digit', minute: '2-digit' });
  }

  getDateDisplay(): string {
    const date = new Date(this.reading.timestamp);
    const today = new Date();
    const yesterday = new Date(today);
    yesterday.setDate(yesterday.getDate() - 1);

    // Nur Datum vergleichen (ohne Uhrzeit)
    const dateOnly = new Date(date.getFullYear(), date.getMonth(), date.getDate());
    const todayOnly = new Date(today.getFullYear(), today.getMonth(), today.getDate());
    const yesterdayOnly = new Date(yesterday.getFullYear(), yesterday.getMonth(), yesterday.getDate());

    if (dateOnly.getTime() === todayOnly.getTime()) {
      return ''; // Heute - kein Datum anzeigen
    } else if (dateOnly.getTime() === yesterdayOnly.getTime()) {
      return 'gestern';
    } else {
      return date.toLocaleDateString('de-DE', { day: '2-digit', month: '2-digit' });
    }
  }

  getTrendIcon(): string {
    switch (this.reading.trendDirection) {
      case 'up': return 'trending_up';
      case 'down': return 'trending_down';
      default: return 'trending_flat';
    }
  }

  getTrendColor(): string {
    switch (this.reading.trendDirection) {
      case 'up': return '#4caf50';
      case 'down': return '#f44336';
      default: return '#9e9e9e';
    }
  }

  getFullTimestamp(): string {
    const date = new Date(this.reading.timestamp);
    return date.toLocaleString('de-DE', {
      weekday: 'long',
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit'
    });
  }
}
