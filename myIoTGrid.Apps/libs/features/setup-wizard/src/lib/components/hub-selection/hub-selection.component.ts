import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatRadioModule } from '@angular/material/radio';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDividerModule } from '@angular/material/divider';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { HubApiService } from '@myiotgrid/shared/data-access';
import { HubProperties, SensorTargetMode, SensorTargetConfig } from '@myiotgrid/shared/models';
import { SetupWizardService } from '../../services/setup-wizard.service';

@Component({
  selector: 'myiotgrid-hub-selection',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatRadioModule,
    MatFormFieldModule,
    MatInputModule,
    MatDividerModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './hub-selection.component.html',
  styleUrl: './hub-selection.component.scss'
})
export class HubSelectionComponent implements OnInit {
  private readonly wizardService = inject(SetupWizardService);
  private readonly hubApiService = inject(HubApiService);

  // State signals
  readonly isLoading = signal(true);
  readonly error = signal<string | null>(null);
  readonly hubProperties = signal<HubProperties | null>(null);

  // Selection state
  readonly selectedMode = signal<SensorTargetMode | null>(null);

  // Local mode overrides - default to HTTP port 5002
  readonly localAddress = signal('http://localhost');
  readonly localPort = signal(5002);

  // Computed values
  readonly tenantId = computed(() => this.hubProperties()?.tenantId ?? '');
  readonly tenantName = computed(() => this.hubProperties()?.tenantName ?? '');
  readonly tenantIdShort = computed(() => {
    const id = this.tenantId();
    return id ? `${id.substring(0, 8)}...` : '';
  });

  // Cloud defaults: http://api.myiotgrid.cloud:5002
  readonly cloudAddress = computed(() => this.hubProperties()?.cloudAddress ?? 'http://api.myiotgrid.cloud');
  readonly cloudPort = computed(() => this.hubProperties()?.cloudPort ?? 5002);

  readonly canContinue = computed(() => {
    const mode = this.selectedMode();
    if (!mode) return false;
    if (mode === 'local') {
      return this.localAddress().trim().length > 0 && this.localPort() > 0;
    }
    return true;
  });

  readonly targetConfig = computed<SensorTargetConfig | null>(() => {
    const mode = this.selectedMode();
    const props = this.hubProperties();
    if (!mode || !props) return null;

    if (mode === 'local') {
      const address = this.localAddress();
      const useSsl = address.startsWith('https://');
      return {
        mode: 'local',
        address: address,
        port: this.localPort(),
        tenantId: props.tenantId,
        tenantName: props.tenantName,
        useSsl
      };
    } else {
      // Cloud mode: use cloudAddress/cloudPort with correct SSL detection
      const cloudAddr = this.cloudAddress();
      const useSsl = cloudAddr.startsWith('https://');
      return {
        mode: 'cloud',
        address: cloudAddr,
        port: this.cloudPort(),
        tenantId: props.tenantId,
        tenantName: props.tenantName,
        useSsl
      };
    }
  });

  ngOnInit(): void {
    this.loadHubProperties();
  }

  private loadHubProperties(): void {
    this.isLoading.set(true);
    this.error.set(null);

    this.hubApiService.getProperties().subscribe({
      next: (properties) => {
        this.hubProperties.set(properties);
        // Pre-fill local address from hub properties, but always use HTTP and port 5002
        const address = properties.address.replace(/^https:\/\//, 'http://');
        this.localAddress.set(address.startsWith('http://') ? address : `http://${address}`);
        this.localPort.set(5002); // Always use port 5002 for local API
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load Hub properties:', err);
        this.error.set('Hub-Eigenschaften konnten nicht geladen werden. Bitte prüfen Sie die Verbindung.');
        this.isLoading.set(false);
      }
    });
  }

  selectMode(mode: SensorTargetMode): void {
    this.selectedMode.set(mode);
  }

  onAddressChange(address: string): void {
    this.localAddress.set(address);
  }

  onPortChange(port: number): void {
    this.localPort.set(port);
  }

  onContinue(): void {
    const config = this.targetConfig();
    if (config) {
      this.wizardService.setTargetConfig(config);
      this.wizardService.nextStep();
    }
  }

  onBack(): void {
    this.wizardService.previousStep();
  }

  onCancel(): void {
    this.wizardService.exitWizard();
  }

  retry(): void {
    this.loadHubProperties();
  }
}
