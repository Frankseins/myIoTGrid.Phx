import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from './base-api.service';
import { BluetoothHub, CreateBluetoothHubDto, UpdateBluetoothHubDto, Node } from '@myiotgrid/shared/models';

/**
 * API service for managing BluetoothHubs (Bluetooth Gateways on Raspberry Pi)
 * Sprint BT-01: Bluetooth Infrastructure
 */
@Injectable({ providedIn: 'root' })
export class BluetoothHubApiService extends BaseApiService {
  private readonly endpoint = '/bluetoothhubs';

  /**
   * Get all BluetoothHubs
   * GET /api/bluetoothhubs
   */
  getAll(): Observable<BluetoothHub[]> {
    return this.get<BluetoothHub[]>(this.endpoint);
  }

  /**
   * Get BluetoothHub by ID
   * GET /api/bluetoothhubs/{id}
   */
  getById(id: string): Observable<BluetoothHub> {
    return this.get<BluetoothHub>(`${this.endpoint}/${id}`);
  }

  /**
   * Create new BluetoothHub
   * POST /api/bluetoothhubs
   */
  create(dto: CreateBluetoothHubDto): Observable<BluetoothHub> {
    return this.post<BluetoothHub>(this.endpoint, dto);
  }

  /**
   * Update BluetoothHub
   * PUT /api/bluetoothhubs/{id}
   */
  update(id: string, dto: UpdateBluetoothHubDto): Observable<BluetoothHub> {
    return this.put<BluetoothHub>(`${this.endpoint}/${id}`, dto);
  }

  /**
   * Delete BluetoothHub
   * DELETE /api/bluetoothhubs/{id}
   */
  remove(id: string): Observable<void> {
    return this.delete<void>(`${this.endpoint}/${id}`);
  }

  /**
   * Send heartbeat from BluetoothHub worker
   * POST /api/bluetoothhubs/{id}/heartbeat
   */
  heartbeat(id: string): Observable<void> {
    return this.post<void>(`${this.endpoint}/${id}/heartbeat`, {});
  }

  /**
   * Update BluetoothHub status
   * PUT /api/bluetoothhubs/{id}/status
   */
  updateStatus(id: string, status: string): Observable<BluetoothHub> {
    return this.put<BluetoothHub>(`${this.endpoint}/${id}/status`, { status });
  }

  /**
   * Get nodes connected to a BluetoothHub
   * GET /api/bluetoothhubs/{id}/nodes
   */
  getNodes(id: string): Observable<Node[]> {
    return this.get<Node[]>(`${this.endpoint}/${id}/nodes`);
  }

  /**
   * Associate a node with a BluetoothHub
   * POST /api/bluetoothhubs/{id}/nodes/{nodeId}
   */
  associateNode(bluetoothHubId: string, nodeId: string): Observable<void> {
    return this.post<void>(`${this.endpoint}/${bluetoothHubId}/nodes/${nodeId}`, {});
  }

  /**
   * Disassociate a node from a BluetoothHub
   * DELETE /api/bluetoothhubs/{id}/nodes/{nodeId}
   */
  disassociateNode(bluetoothHubId: string, nodeId: string): Observable<void> {
    return this.delete<void>(`${this.endpoint}/${bluetoothHubId}/nodes/${nodeId}`);
  }

  /**
   * Register a BLE device that was paired via frontend (Web Bluetooth)
   * POST /api/bluetoothhubs/register-device
   */
  registerBleDevice(dto: RegisterBleDeviceDto): Observable<BleDeviceRegistrationResult> {
    return this.post<BleDeviceRegistrationResult>(`${this.endpoint}/register-device`, dto);
  }

  /**
   * Get or create the default BluetoothHub for this Hub
   * GET /api/bluetoothhubs/default
   */
  getOrCreateDefault(): Observable<BluetoothHub> {
    return this.get<BluetoothHub>(`${this.endpoint}/default`);
  }
}

/**
 * DTO for registering a BLE device paired via frontend
 */
export interface RegisterBleDeviceDto {
  nodeId: string;
  deviceName: string;
  macAddress?: string;
  bluetoothDeviceId?: string;
}

/**
 * Response from BLE device registration
 */
export interface BleDeviceRegistrationResult {
  success: boolean;
  nodeId?: string;
  bluetoothHubId?: string;
  message: string;
}
