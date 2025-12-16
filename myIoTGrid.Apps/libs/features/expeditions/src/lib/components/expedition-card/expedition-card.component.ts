import { Component, computed, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatMenuModule } from '@angular/material/menu';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Expedition, ExpeditionStatus } from '@myiotgrid/shared/models';

@Component({
  selector: 'myiotgrid-expedition-card',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatChipsModule,
    MatMenuModule,
    MatTooltipModule
  ],
  templateUrl: './expedition-card.component.html',
  styleUrl: './expedition-card.component.scss'
})
export class ExpeditionCardComponent {
  expedition = input.required<Expedition>();

  // Outputs
  cardClick = output<void>();
  edit = output<void>();
  delete = output<void>();

  onCardClick(): void {
    this.cardClick.emit();
  }

  // Computed values
  statusConfig = computed(() => {
    const status = this.expedition().status;
    return this.getStatusConfig(status);
  });

  dateRange = computed(() => {
    const exp = this.expedition();
    const start = new Date(exp.startTime);
    const end = new Date(exp.endTime);

    if (this.isSameDay(start, end)) {
      return start.toLocaleDateString('de-DE', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric'
      });
    }

    return `${start.toLocaleDateString('de-DE', { day: '2-digit', month: '2-digit' })} - ${end.toLocaleDateString('de-DE', { day: '2-digit', month: '2-digit', year: 'numeric' })}`;
  });

  formattedDuration = computed(() => {
    const duration = this.expedition().duration;
    if (!duration) return '-';

    // Parse ISO duration or time string (e.g., "08:23:00")
    const parts = duration.split(':');
    if (parts.length >= 2) {
      const hours = parseInt(parts[0], 10);
      const minutes = parseInt(parts[1], 10);

      if (hours > 0) {
        return `${hours}h ${minutes}min`;
      }
      return `${minutes}min`;
    }

    return duration;
  });

  formattedDistance = computed(() => {
    const distance = this.expedition().totalDistanceKm;
    if (!distance) return '-';
    return `${distance.toFixed(1)} km`;
  });

  private getStatusConfig(status: ExpeditionStatus): { label: string; icon: string; class: string } {
    switch (status) {
      case ExpeditionStatus.Planned:
        return { label: 'Geplant', icon: 'schedule', class: 'status-planned' };
      case ExpeditionStatus.Active:
        return { label: 'Aktiv', icon: 'play_circle', class: 'status-active' };
      case ExpeditionStatus.Completed:
        return { label: 'Abgeschlossen', icon: 'check_circle', class: 'status-completed' };
      case ExpeditionStatus.Archived:
        return { label: 'Archiviert', icon: 'inventory_2', class: 'status-archived' };
      default:
        return { label: 'Unbekannt', icon: 'help', class: '' };
    }
  }

  private isSameDay(d1: Date, d2: Date): boolean {
    return d1.getDate() === d2.getDate() &&
           d1.getMonth() === d2.getMonth() &&
           d1.getFullYear() === d2.getFullYear();
  }

  onEdit(event: Event): void {
    event.stopPropagation();
    this.edit.emit();
  }

  onDelete(event: Event): void {
    event.stopPropagation();
    this.delete.emit();
  }
}
