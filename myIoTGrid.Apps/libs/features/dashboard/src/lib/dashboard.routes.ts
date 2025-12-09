import { Routes } from '@angular/router';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { WidgetDetailComponent } from './components/widget-detail/widget-detail.component';

export const DASHBOARD_ROUTES: Routes = [
  {
    path: '',
    component: DashboardComponent
  },
  {
    path: 'widget/:nodeId/:assignmentId/:measurementType',
    component: WidgetDetailComponent
  }
];
