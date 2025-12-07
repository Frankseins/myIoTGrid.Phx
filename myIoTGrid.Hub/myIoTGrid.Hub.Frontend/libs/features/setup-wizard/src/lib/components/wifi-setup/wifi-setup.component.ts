import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { MatSelectModule } from '@angular/material/select';
import { SetupWizardService, WifiCredentials } from '../../services/setup-wizard.service';
import { BleCommunicationService } from '../../services/ble-communication.service';
import { HubApiService } from '@myiotgrid/shared/data-access';
import { HubProvisioningSettings } from '@myiotgrid/shared/models';

type ConfigState = 'idle' | 'scanning' | 'configuring' | 'success' | 'error';

@Component({
  selector: 'myiotgrid-wifi-setup',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatDividerModule,
    MatSelectModule
  ],
  templateUrl: './wifi-setup.component.html',
  styleUrl: './wifi-setup.component.scss'
})
export class WifiSetupComponent implements OnInit {
  private readonly wizardService = inject(SetupWizardService);
  private readonly bleService = inject(BleCommunicationService);
  private readonly hubApiService = inject(HubApiService);
  private readonly fb = inject(FormBuilder);

  readonly configState = signal<ConfigState>('idle');
  readonly showPassword = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly provisioningSettings = signal<HubProvisioningSettings | null>(null);
  readonly isLoadingDefaults = signal(false);

  form!: FormGroup;

  readonly bleDevice = this.wizardService.bleDevice;

  ngOnInit(): void {
    this.initForm();
    this.loadProvisioningDefaults();
  }

  private loadProvisioningDefaults(): void {
    // Pre-fill if we have saved credentials
    const savedCredentials = this.wizardService.wifiCredentials();
    if (savedCredentials) {
      this.form.patchValue({
        ssid: savedCredentials.ssid,
        password: savedCredentials.password
      });
      return;
    }

    // Load provisioning defaults from Hub
    this.isLoadingDefaults.set(true);
    this.hubApiService.getProvisioningSettings().subscribe({
      next: (settings) => {
        this.provisioningSettings.set(settings);
        // Pre-fill form with defaults
        if (settings.defaultWifiSsid) {
          this.form.patchValue({
            ssid: settings.defaultWifiSsid,
            password: settings.defaultWifiPassword || ''
          });
        }
        this.isLoadingDefaults.set(false);
      },
      error: (error) => {
        console.error('[WiFiSetup] Failed to load provisioning defaults:', error);
        this.isLoadingDefaults.set(false);
      }
    });
  }

  private initForm(): void {
    this.form = this.fb.group({
      ssid: ['', [Validators.required, Validators.minLength(1), Validators.maxLength(32)]],
      password: ['', [Validators.required, Validators.minLength(8)]]
    });
  }

  togglePasswordVisibility(): void {
    this.showPassword.update(v => !v);
  }

  async onConfigure(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const credentials: WifiCredentials = {
      ssid: this.form.value.ssid,
      password: this.form.value.password
    };

    this.configState.set('configuring');
    this.errorMessage.set(null);

    try {
      // Check if BLE is connected
      const bleConnected = this.bleService.connectionState() === 'connected';

      if (bleConnected) {
        // Step 1: Send WiFi credentials via BLE
        console.log('[WiFiSetup] Sending WiFi credentials via BLE...');
        const wifiSuccess = await this.bleService.sendWifiConfig({
          ssid: credentials.ssid,
          password: credentials.password
        });

        if (!wifiSuccess) {
          this.configState.set('error');
          this.errorMessage.set(this.bleService.lastError() || 'WiFi-Konfiguration fehlgeschlagen');
          return;
        }

        // Step 2: Send API configuration via BLE (no more mDNS discovery!)
        const settings = this.provisioningSettings();
        const createdNode = this.wizardService.createdNode();

        if (settings?.apiUrl && createdNode) {
          // Build the full Hub URL with port
          // Check if URL already includes a port (after removing protocol like http://)
          const urlWithoutProtocol = settings.apiUrl.replace(/^https?:\/\//, '');
          const hasPort = urlWithoutProtocol.includes(':');
          const hubUrl = hasPort
            ? settings.apiUrl  // Already has port
            : `${settings.apiUrl}:${settings.apiPort}`;

          console.log('[WiFiSetup] Sending API config via BLE...', { hubUrl });
          const apiSuccess = await this.bleService.sendApiConfig({
            nodeId: createdNode.id,
            apiKey: '', // API key will be assigned by Hub on first registration
            hubUrl: hubUrl
          });

          if (!apiSuccess) {
            console.warn('[WiFiSetup] API config send failed, ESP32 may use fallback discovery');
          } else {
            console.log('[WiFiSetup] API config sent successfully. ESP32 will connect directly to Hub.');
          }
        } else {
          console.log('[WiFiSetup] No API URL configured or no node created, ESP32 will use mDNS discovery.');
        }

        this.wizardService.setWifiCredentials(credentials);
        this.configState.set('success');

        // Auto-advance after brief delay
        setTimeout(() => {
          this.wizardService.nextStep();
        }, 1000);
      } else {
        // Fallback: Simulate for demo/testing when BLE is not connected
        console.log('[WiFiSetup] BLE not connected, using simulated mode');
        await new Promise(resolve => setTimeout(resolve, 2000));

        this.wizardService.setWifiCredentials(credentials);
        this.configState.set('success');

        setTimeout(() => {
          this.wizardService.nextStep();
        }, 1000);
      }
    } catch (error) {
      console.error('[WiFiSetup] Configuration error:', error);
      this.configState.set('error');
      this.errorMessage.set('Konfiguration fehlgeschlagen. Bitte versuchen Sie es erneut.');
    }
  }

  onBack(): void {
    this.wizardService.previousStep();
  }

  onCancel(): void {
    this.wizardService.exitWizard();
  }
}
