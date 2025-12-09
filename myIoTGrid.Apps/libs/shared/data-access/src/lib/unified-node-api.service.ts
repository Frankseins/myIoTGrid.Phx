import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from './base-api.service';
import { UnifiedNode } from '@myiotgrid/shared/models';

/**
 * API Service for UnifiedNodes (Local + Direct + Virtual + OtherHub)
 */
@Injectable({ providedIn: 'root' })
export class UnifiedNodeApiService extends BaseApiService {
  private readonly endpoint = '/unified-nodes';

  /**
   * Get all unified nodes
   * GET /api/unified-nodes
   */
  getAll(): Observable<UnifiedNode[]> {
    return this.get<UnifiedNode[]>(this.endpoint);
  }

  /**
   * Get unified node by ID
   * GET /api/unified-nodes/{id}
   */
  getById(id: string): Observable<UnifiedNode> {
    return this.get<UnifiedNode>(`${this.endpoint}/${id}`);
  }

  /**
   * Get unified nodes by source
   * GET /api/unified-nodes?source={source}
   */
  getBySource(source: string): Observable<UnifiedNode[]> {
    return this.get<UnifiedNode[]>(this.endpoint, { source });
  }

  /**
   * Get online unified nodes
   * GET /api/unified-nodes?isOnline=true
   */
  getOnline(): Observable<UnifiedNode[]> {
    return this.get<UnifiedNode[]>(this.endpoint, { isOnline: true });
  }
}
