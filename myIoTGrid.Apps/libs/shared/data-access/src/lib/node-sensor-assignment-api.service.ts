import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from './base-api.service';
import {
  NodeSensorAssignment,
  CreateNodeSensorAssignmentDto,
  UpdateNodeSensorAssignmentDto
} from '@myiotgrid/shared/models';

@Injectable({ providedIn: 'root' })
export class NodeSensorAssignmentApiService extends BaseApiService {

  /**
   * Get all assignments for a node
   * GET /api/nodes/{nodeId}/assignments
   */
  getByNode(nodeId: string): Observable<NodeSensorAssignment[]> {
    return this.get<NodeSensorAssignment[]>(`/nodes/${nodeId}/assignments`);
  }

  /**
   * Get assignment by ID
   * GET /api/nodes/{nodeId}/assignments/{id}
   */
  getById(nodeId: string, id: string): Observable<NodeSensorAssignment> {
    return this.get<NodeSensorAssignment>(`/nodes/${nodeId}/assignments/${id}`);
  }

  /**
   * Get assignment by EndpointId
   * GET /api/nodes/{nodeId}/assignments/endpoint/{endpointId}
   */
  getByEndpoint(nodeId: string, endpointId: number): Observable<NodeSensorAssignment> {
    return this.get<NodeSensorAssignment>(`/nodes/${nodeId}/assignments/endpoint/${endpointId}`);
  }

  /**
   * Get all assignments for a sensor
   * GET /api/sensors/{sensorId}/assignments
   */
  getBySensor(sensorId: string): Observable<NodeSensorAssignment[]> {
    return this.get<NodeSensorAssignment[]>(`/sensors/${sensorId}/assignments`);
  }

  /**
   * Create new assignment (bind sensor to node)
   * POST /api/nodes/{nodeId}/assignments
   */
  create(nodeId: string, dto: CreateNodeSensorAssignmentDto): Observable<NodeSensorAssignment> {
    return this.post<NodeSensorAssignment>(`/nodes/${nodeId}/assignments`, dto);
  }

  /**
   * Update assignment (pin overrides, interval, etc.)
   * PUT /api/nodes/{nodeId}/assignments/{id}
   */
  update(nodeId: string, id: string, dto: UpdateNodeSensorAssignmentDto): Observable<NodeSensorAssignment> {
    return this.put<NodeSensorAssignment>(`/nodes/${nodeId}/assignments/${id}`, dto);
  }

  /**
   * Delete assignment (unbind sensor from node)
   * DELETE /api/nodes/{nodeId}/assignments/{id}
   */
  remove(nodeId: string, id: string): Observable<void> {
    return this.delete<void>(`/nodes/${nodeId}/assignments/${id}`);
  }
}
