import { Injectable, inject, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { API_CONFIG, defaultApiConfig } from './api.config';
import { SensorData, Alert, Hub } from '@myiotgrid/shared/models';

export type ConnectionState = 'disconnected' | 'connecting' | 'connected' | 'reconnecting';

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private readonly config = inject(API_CONFIG, { optional: true }) ?? defaultApiConfig;
  private hubConnection: signalR.HubConnection | null = null;

  readonly connectionState = signal<ConnectionState>('disconnected');
  readonly lastError = signal<string | null>(null);

  async startConnection(): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      return;
    }

    this.connectionState.set('connecting');
    this.lastError.set(null);

    try {
      this.hubConnection = new signalR.HubConnectionBuilder()
        .withUrl(this.config.signalRUrl)
        .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
        .configureLogging(signalR.LogLevel.Information)
        .build();

      this.setupConnectionHandlers();
      await this.hubConnection.start();
      this.connectionState.set('connected');
      console.log('SignalR Connected');
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Unknown error';
      this.lastError.set(message);
      this.connectionState.set('disconnected');
      console.error('SignalR Connection Error:', error);
      throw error;
    }
  }

  async stopConnection(): Promise<void> {
    if (this.hubConnection) {
      await this.hubConnection.stop();
      this.hubConnection = null;
      this.connectionState.set('disconnected');
    }
  }

  private setupConnectionHandlers(): void {
    if (!this.hubConnection) return;

    this.hubConnection.onreconnecting(() => {
      this.connectionState.set('reconnecting');
      console.log('SignalR Reconnecting...');
    });

    this.hubConnection.onreconnected(() => {
      this.connectionState.set('connected');
      console.log('SignalR Reconnected');
    });

    this.hubConnection.onclose((error) => {
      this.connectionState.set('disconnected');
      if (error) {
        this.lastError.set(error.message);
        console.error('SignalR Connection Closed with error:', error);
      }
    });
  }

  // Event Subscriptions
  onNewSensorData(callback: (data: SensorData) => void): void {
    this.hubConnection?.on('NewSensorData', callback);
  }

  onAlertReceived(callback: (alert: Alert) => void): void {
    this.hubConnection?.on('AlertReceived', callback);
  }

  onAlertAcknowledged(callback: (alertId: string) => void): void {
    this.hubConnection?.on('AlertAcknowledged', callback);
  }

  onHubStatusChanged(callback: (hub: Hub) => void): void {
    this.hubConnection?.on('HubStatusChanged', callback);
  }

  onSensorStatusChanged(callback: (sensorId: string, isOnline: boolean) => void): void {
    this.hubConnection?.on('SensorStatusChanged', callback);
  }

  onCloudSyncStatus(callback: (isConnected: boolean) => void): void {
    this.hubConnection?.on('CloudSyncStatus', callback);
  }

  // Hub Methods
  async joinHubGroup(hubId: string): Promise<void> {
    await this.hubConnection?.invoke('JoinHubGroup', hubId);
  }

  async leaveHubGroup(hubId: string): Promise<void> {
    await this.hubConnection?.invoke('LeaveHubGroup', hubId);
  }

  async joinAlertGroup(alertLevel: string): Promise<void> {
    await this.hubConnection?.invoke('JoinAlertGroup', alertLevel);
  }

  async leaveAlertGroup(alertLevel: string): Promise<void> {
    await this.hubConnection?.invoke('LeaveAlertGroup', alertLevel);
  }

  // Remove event handlers
  off(eventName: string): void {
    this.hubConnection?.off(eventName);
  }
}
