import { Component, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { SignalRService, ConnectionState } from '@myiotgrid/shared/data-access';

@Component({
  selector: 'myiotgrid-connection-status',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatTooltipModule],
  templateUrl: './connection-status.component.html',
  styleUrl: './connection-status.component.scss'
})
export class ConnectionStatusComponent {
  private readonly signalRService = inject(SignalRService);

  readonly connectionState = this.signalRService.connectionState;
  readonly lastError = this.signalRService.lastError;

  readonly statusIcon = computed(() => {
    switch (this.connectionState()) {
      case 'connected':
        return 'cloud_done';
      case 'connecting':
      case 'reconnecting':
        return 'cloud_sync';
      case 'disconnected':
      default:
        return 'cloud_off';
    }
  });

  readonly statusClass = computed(() => {
    switch (this.connectionState()) {
      case 'connected':
        return 'status-connected';
      case 'connecting':
      case 'reconnecting':
        return 'status-connecting';
      case 'disconnected':
      default:
        return 'status-disconnected';
    }
  });

  readonly tooltip = computed(() => {
    switch (this.connectionState()) {
      case 'connected':
        return 'Live-Verbindung aktiv';
      case 'connecting':
        return 'Verbindung wird hergestellt...';
      case 'reconnecting':
        return 'Wiederverbindung...';
      case 'disconnected':
        return this.lastError() || 'Keine Verbindung';
    }
  });
}
