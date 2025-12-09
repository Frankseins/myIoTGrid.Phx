import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatTableModule } from '@angular/material/table';
import { HubApiService } from '@myiotgrid/shared/data-access';
import { Hub } from '@myiotgrid/shared/models';
import { LoadingSpinnerComponent, EmptyStateComponent } from '@myiotgrid/shared/ui';
import { RelativeTimePipe, ProtocolPipe } from '@myiotgrid/shared/utils';

@Component({
  selector: 'myiotgrid-hub-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatChipsModule,
    MatTableModule,
    LoadingSpinnerComponent,
    EmptyStateComponent,
    RelativeTimePipe,
    ProtocolPipe
  ],
  templateUrl: './hub-list.component.html',
  styleUrl: './hub-list.component.scss'
})
export class HubListComponent implements OnInit {
  private readonly hubApiService = inject(HubApiService);

  readonly isLoading = signal(true);
  readonly hubs = signal<Hub[]>([]);
  readonly displayedColumns = ['status', 'name', 'hubId', 'protocol', 'lastSeen', 'actions'];

  async ngOnInit(): Promise<void> {
    await this.loadHubs();
  }

  private async loadHubs(): Promise<void> {
    this.isLoading.set(true);
    try {
      const hubs = await this.hubApiService.getAll().toPromise();
      this.hubs.set(hubs || []);
    } catch (error) {
      console.error('Error loading hubs:', error);
    } finally {
      this.isLoading.set(false);
    }
  }
}
