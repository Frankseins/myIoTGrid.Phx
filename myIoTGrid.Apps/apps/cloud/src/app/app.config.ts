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
 * API Configuration for Cloud Frontend
 *
 * Development: Uses proxy (see proxy.conf.json) -> http://localhost:5003
 * Production:  https://api.myiotgrid.cloud
 */
const getApiBaseUrl = (): string => {
  if (typeof window === 'undefined') {
    return '';  // SSR: use relative URL
  }

  const hostname = window.location.hostname;

  // Local development - use relative URL, proxy handles routing to backend
  if (hostname === 'localhost' || hostname === '127.0.0.1') {
    return '';  // Proxy forwards /api/* to http://localhost:5003
  }

  // Production: Frontend on app.myiotgrid.cloud, API on api.myiotgrid.cloud
  return 'https://api.myiotgrid.cloud';
};

const apiBaseUrl = getApiBaseUrl();

const apiConfig: ApiConfig = {
  baseUrl: `${apiBaseUrl}/api`,
  signalRUrl: `${apiBaseUrl}/hubs/sensors`,
  sensorApiUrl: apiBaseUrl
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
