import { Component, OnInit, inject, signal, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatTabsModule } from '@angular/material/tabs';
import { HubApiService, NodeApiService } from '@myiotgrid/shared/data-access';
import { Hub, Node } from '@myiotgrid/shared/models';
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
  private readonly nodeApiService = inject(NodeApiService);
  readonly router = inject(Router);

  id = input.required<string>();

  readonly isLoading = signal(true);
  readonly hub = signal<Hub | null>(null);
  readonly nodes = signal<Node[]>([]);

  async ngOnInit(): Promise<void> {
    await this.loadHub();
  }

  private async loadHub(): Promise<void> {
    this.isLoading.set(true);
    try {
      const [hub, nodes] = await Promise.all([
        this.hubApiService.getById(this.id()).toPromise(),
        this.nodeApiService.getByHubId(this.id()).toPromise()
      ]);
      this.hub.set(hub || null);
      this.nodes.set(nodes || []);
    } catch (error) {
      console.error('Error loading hub:', error);
    } finally {
      this.isLoading.set(false);
    }
  }
}
