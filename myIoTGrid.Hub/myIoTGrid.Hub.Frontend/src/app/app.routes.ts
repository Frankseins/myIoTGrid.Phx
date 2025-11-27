import { Routes } from '@angular/router';

export const appRoutes: Routes = [
  {
    path: '',
    redirectTo: '/dashboard',
    pathMatch: 'full'
  },
  {
    path: 'dashboard',
    loadChildren: () => import('@myiotgrid/dashboard/feature').then(m => m.DASHBOARD_ROUTES)
  },
  {
    path: 'sensors',
    loadChildren: () => import('@myiotgrid/sensors/feature').then(m => m.SENSORS_ROUTES)
  },
  {
    path: 'hubs',
    loadChildren: () => import('@myiotgrid/hubs/feature').then(m => m.HUBS_ROUTES)
  },
  {
    path: 'alerts',
    loadChildren: () => import('@myiotgrid/alerts/feature').then(m => m.ALERTS_ROUTES)
  },
  {
    path: 'settings',
    loadChildren: () => import('@myiotgrid/settings/feature').then(m => m.SETTINGS_ROUTES)
  },
  {
    path: '**',
    redirectTo: '/dashboard'
  }
];
