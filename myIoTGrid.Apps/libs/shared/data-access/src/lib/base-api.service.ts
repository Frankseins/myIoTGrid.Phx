import { inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_CONFIG, defaultApiConfig } from './api.config';

export abstract class BaseApiService {
  protected readonly http = inject(HttpClient);
  protected readonly config = inject(API_CONFIG, { optional: true }) ?? defaultApiConfig;

  protected get baseUrl(): string {
    return this.config.baseUrl;
  }

  protected buildParams(params: Record<string, unknown>): HttpParams {
    let httpParams = new HttpParams();

    Object.entries(params).forEach(([key, value]) => {
      if (value !== null && value !== undefined && value !== '') {
        httpParams = httpParams.set(key, String(value));
      }
    });

    return httpParams;
  }

  protected get<T>(endpoint: string, params?: Record<string, unknown>): Observable<T> {
    const options = params ? { params: this.buildParams(params) } : {};
    return this.http.get<T>(`${this.baseUrl}${endpoint}`, options);
  }

  protected post<T>(endpoint: string, body: unknown): Observable<T> {
    return this.http.post<T>(`${this.baseUrl}${endpoint}`, body);
  }

  protected put<T>(endpoint: string, body: unknown): Observable<T> {
    return this.http.put<T>(`${this.baseUrl}${endpoint}`, body);
  }

  protected delete<T>(endpoint: string): Observable<T> {
    return this.http.delete<T>(`${this.baseUrl}${endpoint}`);
  }

  protected deleteWithBody<T>(endpoint: string, body: unknown): Observable<T> {
    return this.http.delete<T>(`${this.baseUrl}${endpoint}`, { body });
  }
}
