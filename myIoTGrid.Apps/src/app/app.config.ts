import {
  ApplicationConfig,
  provideBrowserGlobalErrorListeners,
  provideZoneChangeDetection,
} from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideHttpClient, withFetch } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { appRoutes } from './app.routes';
import { API_CONFIG, ApiConfig } from '@myiotgrid/shared/data-access';

/**
 * API Configuration
 *
 * For DEVELOPMENT with ESP32 sensors:
 * - Replace YOUR_LOCAL_IP with your machine's IP address (e.g., 192.168.1.100)
 * - The sensor needs to reach the API directly, not through localhost
 *
 * For PRODUCTION:
 * - Use window.location.origin (API and frontend on same host)
 */
const apiConfig: ApiConfig = {
  baseUrl: '/api',
  signalRUrl: '/hubs/sensors',
  // DEVELOPMENT: Change to your local IP, e.g., 'https://192.168.1.100:5001'
  // PRODUCTION: Uses window.location.origin automatically
  sensorApiUrl: typeof window !== 'undefined'
    ? (window.location.hostname === 'localhost'
        ? 'https://localhost:5001'  // Change to https://YOUR_LOCAL_IP:5001 for ESP32 testing
        : window.location.origin)
    : 'https://localhost:5001'
};

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(appRoutes, withComponentInputBinding()),
    provideHttpClient(withFetch()),
    provideAnimationsAsync(),
    { provide: API_CONFIG, useValue: apiConfig }
  ],
};
