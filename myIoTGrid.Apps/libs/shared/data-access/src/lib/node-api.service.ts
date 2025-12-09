import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from './base-api.service';
import { Node, CreateNodeDto, UpdateNodeDto, Sensor, QueryParams, PagedResult, NodeSensorsLatest, NodeGpsStatus } from '@myiotgrid/shared/models';
import { queryParamsToObject } from './api-query.helper';

@Injectable({ providedIn: 'root' })
export class NodeApiService extends BaseApiService {
  private readonly endpoint = '/nodes';

  /**
   * Get all nodes
   * GET /api/nodes
   */
  getAll(): Observable<Node[]> {
    return this.get<Node[]>(this.endpoint);
  }

  /**
   * Get nodes with server-side paging, sorting, and filtering
   * GET /api/nodes/paged
   */
  getPaged(params: QueryParams): Observable<PagedResult<Node>> {
    return this.get<PagedResult<Node>>(`${this.endpoint}/paged`, queryParamsToObject(params));
  }

  /**
   * Get node by ID
   * GET /api/nodes/{id}
   */
  getById(id: string): Observable<Node> {
    return this.get<Node>(`${this.endpoint}/${id}`);
  }

  /**
   * Get nodes for a specific hub
   * GET /api/nodes?hubId={hubId}
   */
  getByHubId(hubId: string): Observable<Node[]> {
    return this.get<Node[]>(this.endpoint, { hubId });
  }

  /**
   * Register/auto-register a node
   * POST /api/nodes/register
   */
  register(dto: CreateNodeDto): Observable<Node> {
    return this.post<Node>(`${this.endpoint}/register`, dto);
  }

  /**
   * Create new node
   * POST /api/nodes
   */
  create(dto: CreateNodeDto): Observable<Node> {
    return this.post<Node>(this.endpoint, dto);
  }

  /**
   * Update node
   * PUT /api/nodes/{id}
   */
  update(id: string, dto: UpdateNodeDto): Observable<Node> {
    return this.put<Node>(`${this.endpoint}/${id}`, dto);
  }

  /**
   * Delete node
   * DELETE /api/nodes/{id}
   */
  remove(id: string): Observable<void> {
    return this.delete<void>(`${this.endpoint}/${id}`);
  }

  /**
   * Get sensors for a specific node
   * GET /api/nodes/{nodeId}/sensors
   */
  getSensors(nodeId: string): Observable<Sensor[]> {
    return this.get<Sensor[]>(`${this.endpoint}/${nodeId}/sensors`);
  }

  /**
   * Update node status
   * PUT /api/nodes/{id}/status
   */
  updateStatus(id: string, isOnline: boolean): Observable<void> {
    return this.put<void>(`${this.endpoint}/${id}/status`, { isOnline });
  }

  /**
   * Get the latest readings for each sensor assigned to a node.
   * Groups by sensor (not by measurement type) to show unique sensors.
   * GET /api/nodes/{id}/sensors/latest
   */
  getSensorsLatest(nodeId: string): Observable<NodeSensorsLatest> {
    return this.get<NodeSensorsLatest>(`${this.endpoint}/${nodeId}/sensors/latest`);
  }

  /**
   * Get GPS status for a node (satellites, fix type, HDOP, position).
   * GET /api/nodes/{id}/gps-status
   */
  getGpsStatus(nodeId: string): Observable<NodeGpsStatus> {
    return this.get<NodeGpsStatus>(`${this.endpoint}/${nodeId}/gps-status`);
  }
}
