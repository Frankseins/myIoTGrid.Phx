import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatListModule } from '@angular/material/list';
import { MatDividerModule } from '@angular/material/divider';
import { HealthApiService } from '@myiotgrid/shared/data-access';
import { LoadingSpinnerComponent } from '@myiotgrid/shared/ui';

interface HealthStatus {
  status: string;
  version?: string;
  uptime?: string;
}

@Component({
  selector: 'myiotgrid-settings',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatListModule,
    MatDividerModule,
    LoadingSpinnerComponent
  ],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.scss'
})
export class SettingsComponent implements OnInit {
  private readonly healthApiService = inject(HealthApiService);

  readonly isLoading = signal(true);
  readonly healthStatus = signal<HealthStatus | null>(null);

  async ngOnInit(): Promise<void> {
    await this.loadData();
  }

  private async loadData(): Promise<void> {
    this.isLoading.set(true);
    try {
      const health = await this.healthApiService.check().toPromise();
      this.healthStatus.set(health || null);
    } catch (error) {
      console.error('Error loading settings:', error);
    } finally {
      this.isLoading.set(false);
    }
  }
}
