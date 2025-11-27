import { InjectionToken } from '@angular/core';

export interface ApiConfig {
  baseUrl: string;
  signalRUrl: string;
}

export const API_CONFIG = new InjectionToken<ApiConfig>('API_CONFIG');

export const defaultApiConfig: ApiConfig = {
  baseUrl: 'http://localhost:5000/api',
  signalRUrl: 'http://localhost:5000/hubs/sensors'
};
