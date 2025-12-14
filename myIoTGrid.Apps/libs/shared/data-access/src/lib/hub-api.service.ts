import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { BaseApiService } from './base-api.service';
import { Hub, HubStatus, HubProvisioningSettings, HubProperties, Node, UpdateHubDto } from '@myiotgrid/shared/models';

/**
 * API Service for Hub management
 * Single-Hub-Architecture: Only one Hub per installation
 */
@Injectable({ providedIn: 'root' })
export class HubApiService extends BaseApiService {
  private readonly endpoint = '/hub';

  // === Single-Hub API (New) ===

  /**
   * Get the current Hub (Single-Hub-Architecture)
   * GET /api/hub
   */
  getCurrent(): Observable<Hub> {
    return this.get<Hub>(this.endpoint);
  }

  /**
   * Update the current Hub
   * PUT /api/hub
   */
  updateCurrent(dto: UpdateHubDto): Observable<Hub> {
    return this.put<Hub>(this.endpoint, dto);
  }

  /**
   * Get Hub status information
   * GET /api/hub/status
   */
  getStatus(): Observable<HubStatus> {
    return this.get<HubStatus>(`${this.endpoint}/status`);
  }

  /**
   * Get all Nodes for the current Hub
   * GET /api/hub/nodes
   */
  getNodes(): Observable<Node[]> {
    return this.get<Node[]>(`${this.endpoint}/nodes`);
  }

  /**
   * Get provisioning settings for BLE setup wizard
   * GET /api/hub/provisioning-settings
   */
  getProvisioningSettings(): Observable<HubProvisioningSettings> {
    return this.get<HubProvisioningSettings>(`${this.endpoint}/provisioning-settings`);
  }

  /**
   * Get Hub properties for sensor setup (Hub/Cloud selection)
   * Contains Address, Port, TenantID (GUID), TenantName and Version.
   * GET /api/hub/properties
   */
  getProperties(): Observable<HubProperties> {
    return this.get<HubProperties>(`${this.endpoint}/properties`);
  }

  // === Legacy API (for compatibility) ===

  /**
   * @deprecated Use getCurrent() instead - Single-Hub-Architecture
   */
  getAll(): Observable<Hub[]> {
    return this.getCurrent().pipe(
      map(hub => [hub])
    );
  }

  /**
   * @deprecated Use getCurrent() instead - Single-Hub-Architecture
   */
  getById(id: string): Observable<Hub> {
    // In Single-Hub-Architecture, always return the current hub
    return this.getCurrent();
  }

  /**
   * @deprecated Use updateCurrent() instead - Single-Hub-Architecture
   */
  update(id: string, dto: UpdateHubDto): Observable<Hub> {
    return this.updateCurrent(dto);
  }

  // === Backup API ===

  /**
   * Download database backup
   * GET /api/backup
   */
  downloadBackup(): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/backup`, {
      responseType: 'blob'
    });
  }

  /**
   * Upload and restore database backup
   * POST /api/backup
   */
  uploadBackup(file: File): Observable<{ message: string }> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<{ message: string }>(`${this.baseUrl}/backup`, formData);
  }

  /**
   * Get database size
   * GET /api/backup/size
   */
  getDatabaseSize(): Observable<{ sizeBytes: number; sizeFormatted: string }> {
    return this.get<{ sizeBytes: number; sizeFormatted: string }>('/backup/size');
  }
}
