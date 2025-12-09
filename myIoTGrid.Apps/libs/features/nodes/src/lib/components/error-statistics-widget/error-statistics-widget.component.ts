import { Component, Input, inject, signal, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';

import { NodeDebugApiService } from '@myiotgrid/shared/data-access';
import { NodeErrorStatistics } from '@myiotgrid/shared/models';

/**
 * Widget for displaying error statistics of a node.
 * Sprint 8: Remote Debug System
 */
@Component({
  selector: 'myiotgrid-error-statistics-widget',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    MatDividerModule
  ],
  templateUrl: './error-statistics-widget.component.html',
  styleUrl: './error-statistics-widget.component.scss'
})
export class ErrorStatisticsWidgetComponent implements OnInit, OnChanges {
  @Input({ required: true }) nodeId!: string;

  private readonly debugApiService = inject(NodeDebugApiService);

  readonly isLoading = signal(true);
  readonly stats = signal<NodeErrorStatistics | null>(null);

  ngOnInit(): void {
    this.loadStatistics();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['nodeId'] && !changes['nodeId'].firstChange) {
      this.loadStatistics();
    }
  }

  private loadStatistics(): void {
    if (!this.nodeId) return;

    this.isLoading.set(true);
    this.debugApiService.getErrorStatistics(this.nodeId).subscribe({
      next: (stats) => {
        this.stats.set(stats);
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Error loading statistics:', error);
        this.isLoading.set(false);
      }
    });
  }

  refresh(): void {
    this.loadStatistics();
  }

  getErrorPercentage(): number {
    const s = this.stats();
    if (!s || s.totalLogs === 0) return 0;
    return Math.round((s.errorCount / s.totalLogs) * 100);
  }

  getCategoryEntries(): [string, number][] {
    const s = this.stats();
    if (!s) return [];
    return Object.entries(s.errorsByCategory).sort((a, b) => b[1] - a[1]);
  }

  formatDate(dateStr: string | undefined): string {
    if (!dateStr) return 'Keine Fehler';
    const date = new Date(dateStr.endsWith('Z') ? dateStr : dateStr + 'Z');
    return date.toLocaleString('de-DE', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  getHealthStatus(): 'healthy' | 'warning' | 'critical' {
    const s = this.stats();
    if (!s || s.totalLogs === 0) return 'healthy';

    const errorRate = s.errorCount / s.totalLogs;
    if (errorRate > 0.2) return 'critical';
    if (errorRate > 0.05) return 'warning';
    return 'healthy';
  }

  getHealthIcon(): string {
    switch (this.getHealthStatus()) {
      case 'critical': return 'error';
      case 'warning': return 'warning';
      default: return 'check_circle';
    }
  }

  getHealthLabel(): string {
    switch (this.getHealthStatus()) {
      case 'critical': return 'Kritisch';
      case 'warning': return 'Warnung';
      default: return 'Gesund';
    }
  }
}
