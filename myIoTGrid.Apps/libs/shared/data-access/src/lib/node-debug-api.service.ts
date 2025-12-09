import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from './base-api.service';
import {
  NodeDebugConfiguration,
  SetNodeDebugLevelDto,
  NodeDebugLog,
  PaginatedResult,
  DebugLogFilter,
  NodeErrorStatistics,
  DebugLogCleanupResult,
  DebugLevel,
  LogCategory,
  NodeHardwareStatus
} from '@myiotgrid/shared/models';

/**
 * API service for Node Debug System (Sprint 8: Remote Debug)
 */
@Injectable({ providedIn: 'root' })
export class NodeDebugApiService extends BaseApiService {
  // Use /nodes for debug config (NodesController) and /node-debug for logs/stats (NodeDebugController)
  private readonly nodesEndpoint = '/nodes';
  private readonly debugEndpoint = '/node-debug';

  // === Debug Configuration ===
  // These are on NodesController at /api/nodes/{id}/debug

  /**
   * Get debug configuration for a node
   * GET /api/nodes/{nodeId}/debug
   */
  getDebugConfiguration(nodeId: string): Observable<NodeDebugConfiguration> {
    return this.get<NodeDebugConfiguration>(`${this.nodesEndpoint}/${nodeId}/debug`);
  }

  /**
   * Set debug level and remote logging for a node
   * PUT /api/nodes/{nodeId}/debug
   */
  setDebugLevel(nodeId: string, dto: SetNodeDebugLevelDto): Observable<NodeDebugConfiguration> {
    return this.put<NodeDebugConfiguration>(`${this.nodesEndpoint}/${nodeId}/debug`, dto);
  }

  // === Debug Logs ===
  // These are on NodeDebugController at /api/node-debug/{nodeId}/debug/logs

  /**
   * Get debug logs with filtering and paging
   * GET /api/node-debug/{nodeId}/debug/logs
   */
  getLogs(nodeId: string, filter?: DebugLogFilter): Observable<PaginatedResult<NodeDebugLog>> {
    const params: Record<string, string> = {};

    if (filter) {
      if (filter.minLevel) params['minLevel'] = filter.minLevel;
      if (filter.category) params['category'] = filter.category;
      if (filter.fromDate) params['fromDate'] = filter.fromDate;
      if (filter.toDate) params['toDate'] = filter.toDate;
      if (filter.pageNumber) params['pageNumber'] = filter.pageNumber.toString();
      if (filter.pageSize) params['pageSize'] = filter.pageSize.toString();
    }

    return this.get<PaginatedResult<NodeDebugLog>>(
      `${this.debugEndpoint}/${nodeId}/debug/logs`,
      params
    );
  }

  /**
   * Get recent logs for live view
   * GET /api/node-debug/{nodeId}/debug/logs/recent
   */
  getRecentLogs(nodeId: string, count: number = 50): Observable<NodeDebugLog[]> {
    return this.get<NodeDebugLog[]>(
      `${this.debugEndpoint}/${nodeId}/debug/logs/recent`,
      { count: count.toString() }
    );
  }

  /**
   * Clear all debug logs for a node
   * DELETE /api/node-debug/{nodeId}/debug/logs
   */
  clearLogs(nodeId: string): Observable<{ deleted: number }> {
    return this.delete<{ deleted: number }>(`${this.debugEndpoint}/${nodeId}/debug/logs`);
  }

  // === Error Statistics ===
  // These are on NodeDebugController at /api/node-debug/{nodeId}/debug/statistics

  /**
   * Get error statistics for a node
   * GET /api/node-debug/{nodeId}/debug/statistics
   */
  getErrorStatistics(nodeId: string): Observable<NodeErrorStatistics> {
    return this.get<NodeErrorStatistics>(`${this.debugEndpoint}/${nodeId}/debug/statistics`);
  }

  /**
   * Get error statistics for all nodes
   * GET /api/node-debug/debug/statistics
   */
  getAllErrorStatistics(): Observable<NodeErrorStatistics[]> {
    return this.get<NodeErrorStatistics[]>(`${this.debugEndpoint}/debug/statistics`);
  }

  // === Cleanup ===

  /**
   * Cleanup old debug logs
   * POST /api/node-debug/debug/cleanup?days=7
   */
  cleanupLogs(days: number = 7): Observable<DebugLogCleanupResult> {
    return this.http.post<DebugLogCleanupResult>(
      `${this.baseUrl}${this.debugEndpoint}/debug/cleanup`,
      null,
      { params: { days: days.toString() } }
    );
  }

  // === Hardware Status ===
  // This is on NodeDebugController at /api/node-debug/{nodeId}/hardware-status

  /**
   * Get hardware status for a node
   * GET /api/node-debug/{nodeId}/hardware-status
   */
  getHardwareStatus(nodeId: string): Observable<NodeHardwareStatus> {
    return this.get<NodeHardwareStatus>(`${this.debugEndpoint}/${nodeId}/hardware-status`);
  }
}
