import { Injectable, inject, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { API_CONFIG, defaultApiConfig } from './api.config';
import { Reading, Alert, Hub, Node, Sensor, NodeDebugLog, NodeDebugConfiguration } from '@myiotgrid/shared/models';

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

  // ==========================================
  // Reading Events (RENAMED from SensorData)
  // ==========================================

  /**
   * Subscribe to new readings
   * Event: NewReading (RENAMED from NewSensorData)
   */
  onNewReading(callback: (reading: Reading) => void): void {
    this.hubConnection?.on('NewReading', callback);
  }

  /**
   * Subscribe to new synced readings
   * Event: NewSyncedReading
   */
  onNewSyncedReading(callback: (reading: Reading) => void): void {
    this.hubConnection?.on('NewSyncedReading', callback);
  }

  // ==========================================
  // Node Events (RENAMED from Sensor events)
  // ==========================================

  /**
   * Subscribe to node status changes
   * Event: NodeStatusChanged (RENAMED from SensorStatusChanged)
   */
  onNodeStatusChanged(callback: (node: Node) => void): void {
    this.hubConnection?.on('NodeStatusChanged', callback);
  }

  /**
   * Subscribe to new node registrations
   * Event: NodeRegistered (RENAMED from SensorRegistered)
   */
  onNodeRegistered(callback: (node: Node) => void): void {
    this.hubConnection?.on('NodeRegistered', callback);
  }

  // ==========================================
  // Sensor Events (NEW - physical sensor chips)
  // ==========================================

  /**
   * Subscribe to sensor added events
   * Event: SensorAdded
   */
  onSensorAdded(callback: (sensor: Sensor) => void): void {
    this.hubConnection?.on('SensorAdded', callback);
  }

  /**
   * Subscribe to sensor removed events
   * Event: SensorRemoved
   */
  onSensorRemoved(callback: (sensorId: string) => void): void {
    this.hubConnection?.on('SensorRemoved', callback);
  }

  // ==========================================
  // Hub Events (unchanged)
  // ==========================================

  /**
   * Subscribe to hub status changes
   */
  onHubStatusChanged(callback: (hub: Hub) => void): void {
    this.hubConnection?.on('HubStatusChanged', callback);
  }

  // ==========================================
  // Alert Events (unchanged)
  // ==========================================

  /**
   * Subscribe to new alerts
   */
  onAlertReceived(callback: (alert: Alert) => void): void {
    this.hubConnection?.on('AlertReceived', callback);
  }

  /**
   * Subscribe to alert acknowledgements
   */
  onAlertAcknowledged(callback: (alertId: string) => void): void {
    this.hubConnection?.on('AlertAcknowledged', callback);
  }

  // ==========================================
  // Cloud Sync Events (unchanged)
  // ==========================================

  /**
   * Subscribe to cloud sync status changes
   */
  onCloudSyncStatus(callback: (isConnected: boolean) => void): void {
    this.hubConnection?.on('CloudSyncStatus', callback);
  }

  // ==========================================
  // Group Management (RENAMED)
  // ==========================================

  /**
   * Join a hub group to receive hub-specific events
   */
  async joinHubGroup(hubId: string): Promise<void> {
    await this.hubConnection?.invoke('JoinHubGroup', hubId);
  }

  /**
   * Leave a hub group
   */
  async leaveHubGroup(hubId: string): Promise<void> {
    await this.hubConnection?.invoke('LeaveHubGroup', hubId);
  }

  /**
   * Join a node group to receive node-specific events
   * (RENAMED from JoinSensorGroup)
   */
  async joinNodeGroup(nodeId: string): Promise<void> {
    await this.hubConnection?.invoke('JoinNodeGroup', nodeId);
  }

  /**
   * Leave a node group
   * (RENAMED from LeaveSensorGroup)
   */
  async leaveNodeGroup(nodeId: string): Promise<void> {
    await this.hubConnection?.invoke('LeaveNodeGroup', nodeId);
  }

  /**
   * Join an alert level group
   */
  async joinAlertGroup(alertLevel: string): Promise<void> {
    await this.hubConnection?.invoke('JoinAlertGroup', alertLevel);
  }

  /**
   * Leave an alert level group
   */
  async leaveAlertGroup(alertLevel: string): Promise<void> {
    await this.hubConnection?.invoke('LeaveAlertGroup', alertLevel);
  }

  // ==========================================
  // Debug Events (Sprint 8: Remote Debug System)
  // ==========================================

  /**
   * Subscribe to debug log received events
   * Event: DebugLogReceived
   */
  onDebugLogReceived(callback: (log: NodeDebugLog) => void): void {
    this.hubConnection?.on('DebugLogReceived', callback);
  }

  /**
   * Subscribe to debug configuration changed events
   * Event: DebugConfigChanged
   */
  onDebugConfigChanged(callback: (config: NodeDebugConfiguration) => void): void {
    this.hubConnection?.on('DebugConfigChanged', callback);
  }

  /**
   * Join a debug group to receive debug logs for a specific node
   */
  async joinDebugGroup(nodeId: string): Promise<void> {
    await this.hubConnection?.invoke('JoinDebugGroup', nodeId);
  }

  /**
   * Leave a debug group
   */
  async leaveDebugGroup(nodeId: string): Promise<void> {
    await this.hubConnection?.invoke('LeaveDebugGroup', nodeId);
  }

  // ==========================================
  // Utility Methods
  // ==========================================

  /**
   * Remove event handler
   */
  off(eventName: string): void {
    this.hubConnection?.off(eventName);
  }
}
