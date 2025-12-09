import { Component, Input, inject, signal, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';

import { NodeDebugApiService } from '@myiotgrid/shared/data-access';
import { NodeHardwareStatus, DetectedDevice } from '@myiotgrid/shared/models';

/**
 * Widget for displaying hardware status of a node.
 * Shows compact summary with error/warning counts, expandable for details.
 * Sprint 8: Remote Debug System
 */
@Component({
  selector: 'myiotgrid-hardware-status-widget',
  standalone: true,
  imports: [
    CommonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    MatExpansionModule,
    MatChipsModule,
    MatDividerModule
  ],
  templateUrl: './hardware-status-widget.component.html',
  styleUrl: './hardware-status-widget.component.scss'
})
export class HardwareStatusWidgetComponent implements OnInit, OnChanges {
  @Input({ required: true }) nodeId!: string;

  private readonly debugApiService = inject(NodeDebugApiService);

  readonly isLoading = signal(true);
  readonly status = signal<NodeHardwareStatus | null>(null);
  readonly expanded = signal(false);

  ngOnInit(): void {
    this.loadStatus();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['nodeId'] && !changes['nodeId'].firstChange) {
      this.loadStatus();
    }
  }

  private loadStatus(): void {
    if (!this.nodeId) return;

    this.isLoading.set(true);
    this.debugApiService.getHardwareStatus(this.nodeId).subscribe({
      next: (status) => {
        this.status.set(status);
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Error loading hardware status:', error);
        this.isLoading.set(false);
      }
    });
  }

  refresh(): void {
    this.loadStatus();
  }

  getOverallStatus(): 'ok' | 'warning' | 'error' | 'unknown' {
    const s = this.status();
    if (!s) return 'unknown';
    switch (s.summary.overallStatus) {
      case 'OK': return 'ok';
      case 'Warning': return 'warning';
      case 'Error': return 'error';
      default: return 'unknown';
    }
  }

  getStatusIcon(): string {
    switch (this.getOverallStatus()) {
      case 'ok': return 'check_circle';
      case 'warning': return 'warning';
      case 'error': return 'error';
      default: return 'help_outline';
    }
  }

  getStatusLabel(): string {
    const s = this.status();
    if (!s) return 'Unbekannt';

    const errors = s.summary.sensorsError;
    const warnings = this.getNotConfiguredCount();

    if (errors > 0) return `${errors} Fehler`;
    if (warnings > 0) return `${warnings} nicht konfiguriert`;
    return 'OK';
  }

  getNotConfiguredCount(): number {
    const s = this.status();
    if (!s) return 0;
    return s.detectedDevices.filter(d => d.status === 'NotConfigured').length;
  }

  getDeviceStatusClass(device: DetectedDevice): string {
    switch (device.status) {
      case 'OK': return 'device-ok';
      case 'Error': return 'device-error';
      case 'NotConfigured': return 'device-warning';
      default: return '';
    }
  }

  getDeviceStatusIcon(device: DetectedDevice): string {
    switch (device.status) {
      case 'OK': return 'check_circle';
      case 'Error': return 'error';
      case 'NotConfigured': return 'help_outline';
      default: return 'device_unknown';
    }
  }

  formatDate(dateStr: string | undefined): string {
    if (!dateStr) return 'Nie';
    const date = new Date(dateStr.endsWith('Z') ? dateStr : dateStr + 'Z');
    return date.toLocaleString('de-DE', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  formatBytes(bytes: number): string {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
  }

  getStoragePercent(): number {
    const s = this.status();
    if (!s || s.storage.totalBytes === 0) return 0;
    return Math.round((s.storage.usedBytes / s.storage.totalBytes) * 100);
  }

  toggleExpanded(): void {
    this.expanded.set(!this.expanded());
  }
}
