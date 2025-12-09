import { Component, Input } from '@angular/core';
import { CommonModule, DatePipe, DecimalPipe } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { ChartStats, Trend } from '@myiotgrid/shared/models';

@Component({
  selector: 'myiotgrid-stats-cards',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatIconModule, DatePipe, DecimalPipe],
  templateUrl: './stats-cards.component.html',
  styleUrl: './stats-cards.component.scss'
})
export class StatsCardsComponent {
  @Input() stats: ChartStats | null = null;
  @Input() trend: Trend | null = null;
  @Input() unit = '';
  @Input() color = '#1976d2';

  getTrendIcon(): string {
    if (!this.trend) return 'remove';
    switch (this.trend.direction) {
      case 'up': return 'trending_up';
      case 'down': return 'trending_down';
      default: return 'trending_flat';
    }
  }

  getTrendColor(): string {
    if (!this.trend) return '#9e9e9e';
    switch (this.trend.direction) {
      case 'up': return '#4caf50';
      case 'down': return '#f44336';
      default: return '#9e9e9e';
    }
  }
}
