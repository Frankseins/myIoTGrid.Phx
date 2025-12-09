import { Routes } from '@angular/router';
import { HubSettingsComponent } from './components/hub-settings/hub-settings.component';

/**
 * Hub Routes
 * Single-Hub-Architecture: Only one Hub per installation
 * The main route shows the Hub settings page (no list needed)
 */
export const HUBS_ROUTES: Routes = [
  {
    path: '',
    component: HubSettingsComponent
  }
];
