import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatListModule } from '@angular/material/list';
import { MatDividerModule } from '@angular/material/divider';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { HealthApiService, BluetoothHubApiService } from '@myiotgrid/shared/data-access';
import { BluetoothHub } from '@myiotgrid/shared/models';
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
    FormsModule,
    RouterLink,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatListModule,
    MatDividerModule,
    MatChipsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSnackBarModule,
    LoadingSpinnerComponent
  ],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.scss'
})
export class SettingsComponent implements OnInit {
  private readonly healthApiService = inject(HealthApiService);
  private readonly bluetoothHubApiService = inject(BluetoothHubApiService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly isLoading = signal(true);
  readonly healthStatus = signal<HealthStatus | null>(null);
  readonly bluetoothHubs = signal<BluetoothHub[]>([]);
  readonly isBluetoothHubsLoading = signal(false);

  // Add BluetoothHub form state
  readonly showAddForm = signal(false);
  readonly newHubName = signal('');
  readonly newHubMacAddress = signal('');
  readonly isAddingHub = signal(false);

  async ngOnInit(): Promise<void> {
    await this.loadData();
  }

  private async loadData(): Promise<void> {
    this.isLoading.set(true);
    try {
      const [health] = await Promise.all([
        this.healthApiService.check().toPromise(),
        this.loadBluetoothHubs()
      ]);
      this.healthStatus.set(health || null);
    } catch (error) {
      console.error('Error loading settings:', error);
    } finally {
      this.isLoading.set(false);
    }
  }

  async loadBluetoothHubs(): Promise<void> {
    this.isBluetoothHubsLoading.set(true);
    try {
      const hubs = await this.bluetoothHubApiService.getAll().toPromise();
      this.bluetoothHubs.set(hubs || []);
    } catch (error) {
      console.error('Error loading BluetoothHubs:', error);
      this.bluetoothHubs.set([]);
    } finally {
      this.isBluetoothHubsLoading.set(false);
    }
  }

  toggleAddForm(): void {
    this.showAddForm.update(show => !show);
    if (!this.showAddForm()) {
      this.resetAddForm();
    }
  }

  private resetAddForm(): void {
    this.newHubName.set('');
    this.newHubMacAddress.set('');
  }

  async addBluetoothHub(): Promise<void> {
    const name = this.newHubName().trim();
    if (!name) {
      this.snackBar.open('Bitte geben Sie einen Namen ein', 'OK', { duration: 3000 });
      return;
    }

    this.isAddingHub.set(true);
    try {
      await this.bluetoothHubApiService.create({
        name,
        macAddress: this.newHubMacAddress().trim() || undefined
      }).toPromise();

      this.snackBar.open('BluetoothHub erfolgreich erstellt', 'OK', { duration: 3000 });
      this.toggleAddForm();
      await this.loadBluetoothHubs();
    } catch (error) {
      console.error('Error creating BluetoothHub:', error);
      this.snackBar.open('Fehler beim Erstellen des BluetoothHubs', 'OK', { duration: 3000 });
    } finally {
      this.isAddingHub.set(false);
    }
  }

  async deleteBluetoothHub(hub: BluetoothHub): Promise<void> {
    if (!confirm(`Möchten Sie den BluetoothHub "${hub.name}" wirklich löschen?`)) {
      return;
    }

    try {
      await this.bluetoothHubApiService.remove(hub.id).toPromise();
      this.snackBar.open('BluetoothHub erfolgreich gelöscht', 'OK', { duration: 3000 });
      await this.loadBluetoothHubs();
    } catch (error) {
      console.error('Error deleting BluetoothHub:', error);
      this.snackBar.open('Fehler beim Löschen des BluetoothHubs', 'OK', { duration: 3000 });
    }
  }

  getStatusClass(status: string): string {
    switch (status.toLowerCase()) {
      case 'active': return 'status-active';
      case 'inactive': return 'status-inactive';
      case 'error': return 'status-error';
      default: return 'status-unknown';
    }
  }

  getStatusLabel(status: string): string {
    switch (status.toLowerCase()) {
      case 'active': return 'Aktiv';
      case 'inactive': return 'Inaktiv';
      case 'error': return 'Fehler';
      default: return status;
    }
  }
}
