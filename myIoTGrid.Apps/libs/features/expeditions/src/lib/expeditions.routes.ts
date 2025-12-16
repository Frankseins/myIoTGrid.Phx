import { Routes } from '@angular/router';
import { ExpeditionsListComponent } from './components/expeditions-list/expeditions-list.component';
import { ExpeditionDetailComponent } from './components/expedition-detail/expedition-detail.component';

export const EXPEDITIONS_ROUTES: Routes = [
  {
    path: '',
    component: ExpeditionsListComponent
  },
  {
    path: ':id',
    component: ExpeditionDetailComponent
  }
];
