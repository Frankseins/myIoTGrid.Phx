import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from './base-api.service';

export interface HealthStatus {
  status: string;
  entries?: Record<string, { status: string; description?: string }>;
}

@Injectable({ providedIn: 'root' })
export class HealthApiService extends BaseApiService {
  check(): Observable<HealthStatus> {
    return this.http.get<HealthStatus>(`${this.config.baseUrl.replace('/api', '')}/health`);
  }

  checkReady(): Observable<HealthStatus> {
    return this.http.get<HealthStatus>(`${this.config.baseUrl.replace('/api', '')}/health/ready`);
  }
}
