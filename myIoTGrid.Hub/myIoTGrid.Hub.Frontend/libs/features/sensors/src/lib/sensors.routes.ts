import { Routes } from '@angular/router';
import { SensorListComponent } from './components/sensor-list/sensor-list.component';
import { SensorDetailComponent } from './components/sensor-detail/sensor-detail.component';

export const SENSORS_ROUTES: Routes = [
  {
    path: '',
    component: SensorListComponent
  },
  {
    path: ':id',
    component: SensorDetailComponent
  }
];
