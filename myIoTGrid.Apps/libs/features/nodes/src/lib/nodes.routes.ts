import { Routes } from '@angular/router';
import { NodeListComponent } from './components/node-list/node-list.component';
import { NodeFormComponent } from './components/node-form/node-form.component';

export const NODES_ROUTES: Routes = [
  {
    path: '',
    component: NodeListComponent
  },
  {
    path: 'new',
    component: NodeFormComponent
  },
  {
    path: ':id',
    component: NodeFormComponent
  }
];
