import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from './base-api.service';

export interface NodeListItem {
  id: string;
  name?: string;
}

@Injectable({ providedIn: 'root' })
export class NodesApiService extends BaseApiService {
  private readonly endpoint = '/nodes';

  list(): Observable<NodeListItem[]> {
    return this.get<NodeListItem[]>(this.endpoint);
  }
}
