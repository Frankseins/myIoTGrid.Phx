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

  readonly isViewMode = computed(() => this.mode() === 'view');
  readonly isEditMode = computed(() => this.mode() === 'edit');

  form!: FormGroup;

  ngOnInit(): void {
    this.initForm();
    this.loadHub();
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
}
