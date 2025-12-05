import { Component, Input, Output, EventEmitter, signal, computed } from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Node, NodeProvisioningStatus, NodeSensorsLatest, Protocol } from '@myiotgrid/shared/models';

@Component({
  selector: 'myiotgrid-node-card',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatTooltipModule,
    DecimalPipe
  ],
  templateUrl: './node-card.component.html',
  styleUrl: './node-card.component.scss'
})
export class NodeCardComponent {
  @Input({ required: true }) node!: Node;
  @Input() sensorsLatest?: NodeSensorsLatest;
  @Input() isDeleting = false;
  @Input() showConfigureButton = true;

  @Output() cardClick = new EventEmitter<Node>();
  @Output() configureClick = new EventEmitter<Node>();

  // Expanded state per card (internal)
  private _expanded = signal(false);

  get isExpanded(): boolean {
    return this._expanded();
  }

  toggleSensorsExpanded(event: Event): void {
    event.stopPropagation();
    this._expanded.update(v => !v);
  }

  onCardClick(): void {
    this.cardClick.emit(this.node);
  }

  onConfigureClick(event: Event): void {
    event.stopPropagation();
    this.configureClick.emit(this.node);
  }

  /**
   * Checks if node needs configuration (status != Configured)
   */
  isUnconfigured(): boolean {
    return this.node.status !== NodeProvisioningStatus.Configured;
  }

  /**
   * Returns status label for unconfigured nodes
   */
  getStatusLabel(): string {
    switch (this.node.status) {
      case NodeProvisioningStatus.Unconfigured: return 'Nicht konfiguriert';
      case NodeProvisioningStatus.Pairing: return 'Pairing läuft...';
      case NodeProvisioningStatus.Error: return 'Fehler';
      default: return '';
    }
  }

  /**
   * Returns status icon for unconfigured nodes
   */
  getStatusIcon(): string {
    switch (this.node.status) {
      case NodeProvisioningStatus.Unconfigured: return 'settings';
      case NodeProvisioningStatus.Pairing: return 'bluetooth_searching';
      case NodeProvisioningStatus.Error: return 'error';
      default: return 'check_circle';
    }
  }

  getNodeIcon(): string {
    const protocol = this.node.protocol;
    switch (protocol) {
      case Protocol.WLAN: return 'wifi';
      case Protocol.LoRaWAN: return 'cell_tower';
      default: return 'router';
    }
  }

  /**
   * Get the latest timestamp from sensors or node.lastSeen
   */
  getNodeTimestamp(): string | null {
    if (this.sensorsLatest) {
      const sensorTimestamp = this.getLatestTimestamp();
      if (sensorTimestamp) {
        return sensorTimestamp;
      }
    }
    return this.node.lastSeen || null;
  }

  private getLatestTimestamp(): string | null {
    if (!this.sensorsLatest) return null;
    let latest: string | null = null;
    for (const sensor of this.sensorsLatest.sensors) {
      for (const m of sensor.measurements) {
        if (!latest || new Date(m.timestamp) > new Date(latest)) {
          latest = m.timestamp;
        }
      }
    }
    return latest;
  }

  getRelativeTime(timestamp: string | null): string {
    if (!timestamp) return '';
    const date = new Date(timestamp);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);

    if (diffMins < 1) return 'gerade eben';
    if (diffMins < 60) return `vor ${diffMins} Min.`;
    const diffHours = Math.floor(diffMins / 60);
    if (diffHours < 24) return `vor ${diffHours} Std.`;
    const diffDays = Math.floor(diffHours / 24);
    return `vor ${diffDays} Tag${diffDays > 1 ? 'en' : ''}`;
  }
}
