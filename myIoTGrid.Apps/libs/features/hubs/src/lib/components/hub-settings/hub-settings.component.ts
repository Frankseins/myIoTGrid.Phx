import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDividerModule } from '@angular/material/divider';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatListModule } from '@angular/material/list';
import { HubApiService } from '@myiotgrid/shared/data-access';
import { Hub, HubStatus, UpdateHubDto, Node } from '@myiotgrid/shared/models';
import { LoadingSpinnerComponent } from '@myiotgrid/shared/ui';
import { RelativeTimePipe } from '@myiotgrid/shared/utils';

type FormMode = 'view' | 'edit';

/**
 * Hub Settings Component
 * Single-Hub-Architecture: Only one Hub per installation
 */
@Component({
  selector: 'myiotgrid-hub-settings',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSnackBarModule,
    MatDividerModule,
    MatChipsModule,
    MatTooltipModule,
    MatListModule,
    LoadingSpinnerComponent,
    RelativeTimePipe
  ],
  templateUrl: './hub-settings.component.html',
  styleUrl: './hub-settings.component.scss'
})
export class HubSettingsComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly hubApiService = inject(HubApiService);
  private readonly snackBar = inject(MatSnackBar);

  readonly isLoading = signal(true);
  readonly isSaving = signal(false);
  readonly mode = signal<FormMode>('view');
  readonly hub = signal<Hub | null>(null);
  readonly status = signal<HubStatus | null>(null);
  readonly nodes = signal<Node[]>([]);

  // Backup state
  readonly isBackingUp = signal(false);
  readonly isRestoring = signal(false);
  readonly databaseSize = signal<string>('');

  readonly isViewMode = computed(() => this.mode() === 'view');
  readonly isEditMode = computed(() => this.mode() === 'edit');

  form!: FormGroup;

  ngOnInit(): void {
    this.initForm();
    this.loadHub();
    this.loadDatabaseSize();
  }

  private initForm(): void {
    this.form = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(200)]],
      description: ['', [Validators.maxLength(500)]],
      // Node provisioning defaults
      defaultWifiSsid: ['', [Validators.maxLength(32)]],
      defaultWifiPassword: ['', [Validators.maxLength(64)]],
      apiUrl: ['', [Validators.maxLength(200)]],
      apiPort: [5002, [Validators.min(1), Validators.max(65535)]]
    });
  }

  private loadHub(): void {
    this.isLoading.set(true);

    // Load hub, status and nodes in parallel
    Promise.all([
      this.hubApiService.getCurrent().toPromise(),
      this.hubApiService.getStatus().toPromise(),
      this.hubApiService.getNodes().toPromise()
    ]).then(([hub, status, nodes]) => {
      this.hub.set(hub || null);
      this.status.set(status || null);
      this.nodes.set(nodes || []);

      if (hub) {
        this.patchForm(hub);
      }

      this.isLoading.set(false);
    }).catch(error => {
      console.error('Error loading hub:', error);
      this.snackBar.open('Fehler beim Laden der Hub-Einstellungen', 'Schließen', { duration: 5000 });
      this.isLoading.set(false);
    });
  }

  private patchForm(hub: Hub): void {
    this.form.patchValue({
      name: hub.name,
      description: hub.description || '',
      defaultWifiSsid: hub.defaultWifiSsid || '',
      defaultWifiPassword: hub.defaultWifiPassword || '',
      apiUrl: hub.apiUrl || '',
      apiPort: hub.apiPort || 5002
    });
  }

  toggleEditMode(): void {
    if (this.isViewMode()) {
      this.mode.set('edit');
    } else {
      this.mode.set('view');
      // Reset form to original values
      const h = this.hub();
      if (h) {
        this.patchForm(h);
      }
    }
  }

  onSave(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSaving.set(true);
    const formValue = this.form.getRawValue();

    const dto: UpdateHubDto = {
      name: formValue.name,
      description: formValue.description || undefined,
      defaultWifiSsid: formValue.defaultWifiSsid || undefined,
      defaultWifiPassword: formValue.defaultWifiPassword || undefined,
      apiUrl: formValue.apiUrl || undefined,
      apiPort: formValue.apiPort || undefined
    };

    this.hubApiService.updateCurrent(dto).subscribe({
      next: (updatedHub) => {
        this.hub.set(updatedHub);
        this.patchForm(updatedHub);
        this.mode.set('view');
        this.snackBar.open('Hub-Einstellungen gespeichert', 'Schließen', { duration: 3000 });
        this.isSaving.set(false);
      },
      error: (error) => {
        console.error('Error updating hub:', error);
        this.snackBar.open('Fehler beim Speichern der Einstellungen', 'Schließen', { duration: 5000 });
        this.isSaving.set(false);
      }
    });
  }

  onCancel(): void {
    this.mode.set('view');
    const h = this.hub();
    if (h) {
      this.patchForm(h);
    }
  }

  navigateToNodes(): void {
    this.router.navigate(['/nodes']);
  }

  getStatusClass(): string {
    return this.hub()?.isOnline ? 'online' : 'offline';
  }

  getStatusIcon(): string {
    return this.hub()?.isOnline ? 'check_circle' : 'cancel';
  }

  getStatusText(): string {
    return this.hub()?.isOnline ? 'Online' : 'Offline';
  }

  // Reconnecting state after restore
  readonly isReconnecting = signal(false);

  // === Backup Methods ===

  loadDatabaseSize(): void {
    this.hubApiService.getDatabaseSize().subscribe({
      next: (result) => {
        this.databaseSize.set(result.sizeFormatted);
      },
      error: (error) => {
        console.error('Error loading database size:', error);
      }
    });
  }

  downloadBackup(): void {
    this.isBackingUp.set(true);

    this.hubApiService.downloadBackup().subscribe({
      next: (blob) => {
        // Create download link
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `hub_backup_${new Date().toISOString().replace(/[:.]/g, '-')}.db`;
        link.click();
        window.URL.revokeObjectURL(url);

        this.snackBar.open('Backup erfolgreich heruntergeladen', 'Schließen', { duration: 3000 });
        this.isBackingUp.set(false);
      },
      error: (error) => {
        console.error('Error downloading backup:', error);
        this.snackBar.open('Fehler beim Erstellen des Backups', 'Schließen', { duration: 5000 });
        this.isBackingUp.set(false);
      }
    });
  }

  onRestoreFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];

    if (!file) {
      return;
    }

    if (!file.name.endsWith('.db')) {
      this.snackBar.open('Bitte wählen Sie eine .db Datei aus', 'Schließen', { duration: 5000 });
      return;
    }

    // Confirm restore
    const confirmed = window.confirm(
      'WARNUNG: Das Wiederherstellen ersetzt die aktuelle Datenbank!\n\n' +
      'Alle aktuellen Daten gehen verloren.\n' +
      'Das Backend wird automatisch neu gestartet.\n\n' +
      'Sind Sie sicher, dass Sie fortfahren möchten?'
    );

    if (!confirmed) {
      input.value = '';
      return;
    }

    this.isRestoring.set(true);

    this.hubApiService.uploadBackup(file).subscribe({
      next: () => {
        this.isRestoring.set(false);
        this.isReconnecting.set(true);
        input.value = '';

        this.snackBar.open(
          'Backup wiederhergestellt! Backend wird neu gestartet...',
          'OK',
          { duration: 5000 }
        );

        // Wait for backend to restart and reconnect
        this.waitForBackendRestart();
      },
      error: (error) => {
        console.error('Error restoring backup:', error);
        const message = error.error?.message || error.error?.error || 'Fehler beim Wiederherstellen des Backups';
        this.snackBar.open(message, 'Schließen', { duration: 5000 });
        this.isRestoring.set(false);
        input.value = '';
      }
    });
  }

  private waitForBackendRestart(): void {
    const maxAttempts = 30; // 30 seconds max
    let attempts = 0;

    const checkBackend = () => {
      attempts++;

      this.hubApiService.getStatus().subscribe({
        next: () => {
          // Backend is back online
          this.isReconnecting.set(false);
          this.snackBar.open('Backend erfolgreich neu gestartet!', 'OK', { duration: 3000 });

          // Reload all data
          this.loadHub();
          this.loadDatabaseSize();
        },
        error: () => {
          if (attempts < maxAttempts) {
            // Retry after 1 second
            setTimeout(checkBackend, 1000);
          } else {
            this.isReconnecting.set(false);
            this.snackBar.open(
              'Backend-Neustart dauert länger als erwartet. Bitte Seite manuell neu laden.',
              'OK',
              { duration: 0 }
            );
          }
        }
      });
    };

    // Start checking after 2 seconds (give backend time to shutdown)
    setTimeout(checkBackend, 2000);
  }
}
