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
import { HubApiService, NodeApiService } from '@myiotgrid/shared/data-access';
import { HubProvisioningSettings, NodeConfigurationDto } from '@myiotgrid/shared/models';
import { firstValueFrom } from 'rxjs';

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
  private readonly nodeApiService = inject(NodeApiService);
  private readonly fb = inject(FormBuilder);

  readonly configState = signal<ConfigState>('idle');
  readonly showPassword = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly provisioningSettings = signal<HubProvisioningSettings | null>(null);
  readonly isLoadingDefaults = signal(false);

  form!: FormGroup;

  readonly bleDevice = this.wizardService.bleDevice;
  readonly targetConfig = this.wizardService.targetConfig;

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

        // Step 2: Send API configuration via BLE
        const targetConfig = this.targetConfig();
        const isCloudMode = targetConfig?.mode === 'cloud';

        if (isCloudMode) {
          // Cloud mode: Call provision endpoint with WiFi credentials
          console.log('[WiFiSetup] Cloud mode: Calling provision endpoint...');
          await this.configureCloudMode(credentials);
        } else {
          // Hub mode: Use existing provisioning settings
          console.log('[WiFiSetup] Hub mode: Using local provisioning settings...');
          await this.configureHubMode();
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

  /**
   * Configure for Cloud mode:
   * 1. Call /api/nodes/provision with MAC address + WiFi credentials
   * 2. Send API config (nodeId, apiKey, hubApiUrl, targetMode='cloud') to ESP32 via BLE
   * Uses the targetConfig from hub-selection for the Cloud URL
   */
  private async configureCloudMode(credentials: WifiCredentials): Promise<void> {
    const bleDevice = this.bleDevice();
    if (!bleDevice) {
      throw new Error('No BLE device connected');
    }

    const targetConfig = this.targetConfig();
    if (!targetConfig) {
      throw new Error('No target configuration set');
    }

    // Build the Cloud API URL from targetConfig (set in hub-selection)
    const cloudUrl = `${targetConfig.address}:${targetConfig.port}`;

    // Call Cloud provision endpoint with MAC address + WiFi credentials
    const provisionDto = {
      macAddress: bleDevice.macAddress,
      name: bleDevice.name,
      wifiSsid: credentials.ssid,
      wifiPassword: credentials.password
    };

    console.log('[WiFiSetup] Provisioning node in Cloud...', { macAddress: provisionDto.macAddress });
    const config: NodeConfigurationDto = await firstValueFrom(
      this.nodeApiService.provision(provisionDto)
    );

    console.log('[WiFiSetup] Cloud provision response:', {
      nodeId: config.nodeId,
      hubApiUrl: config.hubApiUrl
    });

    // Use the Cloud URL from targetConfig (from hub-selection), not from provision response
    const hubUrl = cloudUrl;
    console.log('[WiFiSetup] Sending API config via BLE (CLOUD mode)...', { hubUrl });
    const apiSuccess = await this.bleService.sendApiConfig({
      nodeId: config.nodeId,
      apiKey: config.apiKey,
      hubUrl: hubUrl,
      targetMode: 'cloud'  // Tell firmware to use Cloud mode
    });

    if (!apiSuccess) {
      console.warn('[WiFiSetup] API config send failed');
      throw new Error('Konnte API-Konfiguration nicht an Sensor senden');
    }

    console.log('[WiFiSetup] Cloud mode configuration complete. ESP32 will connect to:', hubUrl);
  }

  /**
   * Configure for Hub mode:
   * Use targetConfig from hub-selection (which contains the address from Hub properties)
   */
  private async configureHubMode(): Promise<void> {
    const targetConfig = this.targetConfig();
    const createdNode = this.wizardService.createdNode();

    if (targetConfig && createdNode) {
      // Build the full Hub URL from targetConfig (set in hub-selection from Hub properties)
      const hubUrl = `${targetConfig.address}:${targetConfig.port}`;

      console.log('[WiFiSetup] Sending API config via BLE (LOCAL mode)...', { hubUrl });
      const apiSuccess = await this.bleService.sendApiConfig({
        nodeId: createdNode.id,
        apiKey: '', // API key will be assigned by Hub on first registration
        hubUrl: hubUrl,
        targetMode: 'local'  // Tell firmware to use Local/Hub mode
      });

      if (!apiSuccess) {
        console.warn('[WiFiSetup] API config send failed, ESP32 may use fallback discovery');
      } else {
        console.log('[WiFiSetup] Hub mode configuration complete. ESP32 will connect to:', hubUrl);
      }
    } else {
      console.log('[WiFiSetup] No target config or no node created, ESP32 will use mDNS discovery.');
    }
  }

  onBack(): void {
    this.wizardService.previousStep();
  }

  onCancel(): void {
    this.wizardService.exitWizard();
  }
}
