import { Routes } from '@angular/router';
import { HubListComponent } from './components/hub-list/hub-list.component';
import { HubDetailComponent } from './components/hub-detail/hub-detail.component';

export const HUBS_ROUTES: Routes = [
  {
    path: '',
    component: HubListComponent
  },
  {
    path: ':id',
    component: HubDetailComponent
  }
];
