import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from './base-api.service';
import { Hub, CreateHubDto, UpdateHubDto } from '@myiotgrid/shared/models';

@Injectable({ providedIn: 'root' })
export class HubApiService extends BaseApiService {
  private readonly endpoint = '/hubs';

  getAll(): Observable<Hub[]> {
    return this.get<Hub[]>(this.endpoint);
  }

  getById(id: string): Observable<Hub> {
    return this.get<Hub>(`${this.endpoint}/${id}`);
  }

  getByHubId(hubId: string): Observable<Hub> {
    return this.get<Hub>(`${this.endpoint}/by-hub-id/${hubId}`);
  }

  create(dto: CreateHubDto): Observable<Hub> {
    return this.post<Hub>(this.endpoint, dto);
  }

  update(id: string, dto: UpdateHubDto): Observable<Hub> {
    return this.put<Hub>(`${this.endpoint}/${id}`, dto);
  }

  remove(id: string): Observable<void> {
    return this.delete<void>(`${this.endpoint}/${id}`);
  }
}
