import { InjectionToken } from '@angular/core';

export interface ApiConfig {
  baseUrl: string;
  signalRUrl: string;
  /**
   * The external API URL that sensors should use to connect.
   * This must be an absolute URL reachable by ESP32 sensors.
   * In development, use your machine's IP address (e.g., 'https://192.168.1.100:5001')
   * In production, this can be the same as the frontend host.
   */
  sensorApiUrl: string;
}

export const API_CONFIG = new InjectionToken<ApiConfig>('API_CONFIG');

export const defaultApiConfig: ApiConfig = {
  baseUrl: '/api',
  signalRUrl: '/hubs/sensors',
  // Default: use window.location.origin (works in production when API is on same host)
  // Override this in app.config.ts for development with your machine's IP
  sensorApiUrl: typeof window !== 'undefined' ? window.location.origin : 'http://localhost:5001'
};
