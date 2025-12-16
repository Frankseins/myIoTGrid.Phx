import { Injectable, inject, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { API_CONFIG, defaultApiConfig } from './api.config';
import { Reading, Alert, Hub, Node, Sensor, NodeDebugLog, NodeDebugConfiguration } from '@myiotgrid/shared/models';

export type ConnectionState = 'disconnected' | 'connecting' | 'connected' | 'reconnecting';

/**
 * SignalR Service with Signal-based event handling.
 *
 * Instead of registering callbacks per component, this service
 * maintains Signals that components can subscribe to using effects.
 *
 * This approach:
 * - Prevents "No client method found" warnings
 * - Centralizes event handling
 * - Leverages Angular's reactivity system
 */
@Injectable({ providedIn: 'root' })
export class SignalRService {
  private readonly config = inject(API_CONFIG, { optional: true }) ?? defaultApiConfig;
  private hubConnection: signalR.HubConnection | null = null;

  // ==========================================
  // Connection State
  // ==========================================
  readonly connectionState = signal<ConnectionState>('disconnected');
  readonly lastError = signal<string | null>(null);

  // ==========================================
  // Event Signals - Components subscribe to these
  // ==========================================

  /** Latest reading received via SignalR */
  readonly latestReading = signal<Reading | null>(null);

  /** Latest synced reading received via SignalR */
  readonly latestSyncedReading = signal<Reading | null>(null);

  /** Latest node status change */
  readonly nodeStatusChanged = signal<Node | null>(null);

  /** Latest registered node */
  readonly nodeRegistered = signal<Node | null>(null);

  /** Latest added sensor */
  readonly sensorAdded = signal<Sensor | null>(null);

  /** Latest removed sensor ID */
  readonly sensorRemoved = signal<string | null>(null);

  /** Latest hub status change */
  readonly hubStatusChanged = signal<Hub | null>(null);

  /** Latest alert received */
  readonly alertReceived = signal<Alert | null>(null);

  /** Latest acknowledged alert ID */
  readonly alertAcknowledged = signal<string | null>(null);

  /** Cloud sync status */
  readonly cloudSyncStatus = signal<boolean | null>(null);

  /** Latest debug log received */
  readonly debugLogReceived = signal<NodeDebugLog | null>(null);

  /** Latest debug config change */
  readonly debugConfigChanged = signal<NodeDebugConfiguration | null>(null);

  // ==========================================
  // Connection Management
  // ==========================================

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
        .configureLogging(signalR.LogLevel.Warning) // Reduced from Information to avoid noise
        .build();

      this.setupConnectionHandlers();
      this.setupEventHandlers();
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

  /**
   * Register ALL event handlers once at connection time.
   * This prevents "No client method found" warnings.
   */
  private setupEventHandlers(): void {
    if (!this.hubConnection) return;

    // Reading events
    this.hubConnection.on('NewReading', (reading: Reading) => {
      this.latestReading.set(reading);
    });

    this.hubConnection.on('NewSyncedReading', (reading: Reading) => {
      this.latestSyncedReading.set(reading);
    });

    // Node events
    this.hubConnection.on('NodeStatusChanged', (node: Node) => {
      this.nodeStatusChanged.set(node);
    });

    this.hubConnection.on('NodeRegistered', (node: Node) => {
      this.nodeRegistered.set(node);
    });

    // Sensor events
    this.hubConnection.on('SensorAdded', (sensor: Sensor) => {
      this.sensorAdded.set(sensor);
    });

    this.hubConnection.on('SensorRemoved', (sensorId: string) => {
      this.sensorRemoved.set(sensorId);
    });

    // Hub events
    this.hubConnection.on('HubStatusChanged', (hub: Hub) => {
      this.hubStatusChanged.set(hub);
    });

    // Alert events
    this.hubConnection.on('AlertReceived', (alert: Alert) => {
      this.alertReceived.set(alert);
    });

    this.hubConnection.on('AlertAcknowledged', (alertId: string) => {
      this.alertAcknowledged.set(alertId);
    });

    // Cloud sync events
    this.hubConnection.on('CloudSyncStatus', (isConnected: boolean) => {
      this.cloudSyncStatus.set(isConnected);
    });

    // Debug events
    this.hubConnection.on('DebugLogReceived', (log: NodeDebugLog) => {
      this.debugLogReceived.set(log);
    });

    this.hubConnection.on('DebugConfigChanged', (config: NodeDebugConfiguration) => {
      this.debugConfigChanged.set(config);
    });
  }

  // ==========================================
  // Group Management
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
   */
  async joinNodeGroup(nodeId: string): Promise<void> {
    await this.hubConnection?.invoke('JoinNodeGroup', nodeId);
  }

  /**
   * Leave a node group
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
  // Legacy Callback Methods (deprecated)
  // Keep for backward compatibility during migration
  // ==========================================

  /**
   * @deprecated Use latestReading signal with effect() instead
   */
  onNewReading(callback: (reading: Reading) => void): void {
    this.hubConnection?.on('NewReading', callback);
  }

  /**
   * @deprecated Use nodeStatusChanged signal with effect() instead
   */
  onNodeStatusChanged(callback: (node: Node) => void): void {
    this.hubConnection?.on('NodeStatusChanged', callback);
  }

  /**
   * @deprecated Use alertReceived signal with effect() instead
   */
  onAlertReceived(callback: (alert: Alert) => void): void {
    this.hubConnection?.on('AlertReceived', callback);
  }

  /**
   * @deprecated Use debugLogReceived signal with effect() instead
   */
  onDebugLogReceived(callback: (log: NodeDebugLog) => void): void {
    this.hubConnection?.on('DebugLogReceived', callback);
  }

  /**
   * @deprecated Use debugConfigChanged signal with effect() instead
   */
  onDebugConfigChanged(callback: (config: NodeDebugConfiguration) => void): void {
    this.hubConnection?.on('DebugConfigChanged', callback);
  }

  /**
   * Remove event handler
   * @deprecated No longer needed with signal-based approach
   */
  off(eventName: string): void {
    this.hubConnection?.off(eventName);
  }
}
