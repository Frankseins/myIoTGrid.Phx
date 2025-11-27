import { Component, OnInit, inject, signal, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatTabsModule } from '@angular/material/tabs';
import { HubApiService, SensorApiService } from '@myiotgrid/shared/data-access';
import { Hub, Sensor } from '@myiotgrid/shared/models';
import { LoadingSpinnerComponent, EmptyStateComponent } from '@myiotgrid/shared/ui';
import { RelativeTimePipe, ProtocolPipe } from '@myiotgrid/shared/utils';

@Component({
  selector: 'myiotgrid-hub-detail',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatChipsModule,
    MatTabsModule,
    LoadingSpinnerComponent,
    EmptyStateComponent,
    RelativeTimePipe,
    ProtocolPipe
  ],
  templateUrl: './hub-detail.component.html',
  styleUrl: './hub-detail.component.scss'
})
export class HubDetailComponent implements OnInit {
  private readonly hubApiService = inject(HubApiService);
  private readonly sensorApiService = inject(SensorApiService);

  id = input.required<string>();

  readonly isLoading = signal(true);
  readonly hub = signal<Hub | null>(null);
  readonly sensors = signal<Sensor[]>([]);

  async ngOnInit(): Promise<void> {
    await this.loadHub();
  }

  private async loadHub(): Promise<void> {
    this.isLoading.set(true);
    try {
      const [hub, sensors] = await Promise.all([
        this.hubApiService.getById(this.id()).toPromise(),
        this.sensorApiService.getByHub(this.id()).toPromise()
      ]);
      this.hub.set(hub || null);
      this.sensors.set(sensors || []);
    } catch (error) {
      console.error('Error loading hub:', error);
    } finally {
      this.isLoading.set(false);
    }
  }
}
