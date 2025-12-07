import { Component, inject, signal, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatListModule } from '@angular/material/list';
import { MatDividerModule } from '@angular/material/divider';
import { MatChipsModule } from '@angular/material/chips';
import { SetupWizardService, BleDevice } from '../../services/setup-wizard.service';
import { BleCommunicationService } from '../../services/ble-communication.service';

type ScanState = 'idle' | 'scanning' | 'connecting' | 'connected' | 'error';

@Component({
  selector: 'myiotgrid-ble-pairing',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatListModule,
    MatDividerModule,
    MatChipsModule
  ],
  templateUrl: './ble-pairing.component.html',
  styleUrl: './ble-pairing.component.scss'
})
export class BlePairingComponent implements OnInit, OnDestroy {
  private readonly wizardService = inject(SetupWizardService);
  private readonly bleService = inject(BleCommunicationService);

  readonly scanState = signal<ScanState>('idle');
  readonly devices = signal<BleDevice[]>([]);
  readonly selectedDevice = signal<BleDevice | null>(null);
  readonly errorMessage = signal<string | null>(null);
  readonly isBluetoothSupported = signal(true);

  private scanInterval: ReturnType<typeof setInterval> | null = null;
  private selectedBluetoothDevice: BluetoothDevice | null = null;

  ngOnInit(): void {
    // Check if Web Bluetooth is supported
    if (!('bluetooth' in navigator)) {
      this.isBluetoothSupported.set(false);
      this.errorMessage.set('Web Bluetooth wird von diesem Browser nicht unterstützt. Bitte verwenden Sie Chrome, Edge oder Opera.');
    }
  }

  ngOnDestroy(): void {
    this.stopScanSimulation();
  }

  async startScan(): Promise<void> {
    this.scanState.set('scanning');
    this.devices.set([]);
    this.errorMessage.set(null);

    // Check if Web Bluetooth is supported
    if (this.bleService.isSupported()) {
      try {
        // Use BleCommunicationService to request device - this stores the device for later connection
        const device = await this.bleService.requestDevice();

        if (device) {
          const mac = this.generateMacFromId(device.id);
          const bleDevice: BleDevice = {
            id: device.id,
            name: device.name || 'Unbekanntes Gerät',
            rssi: -50, // Web Bluetooth doesn't expose RSSI directly
            macAddress: mac,
            nodeId: `ESP32-${mac.replace(/:/g, '').toUpperCase()}` // Temporary, updated on connect
          };

          this.devices.set([bleDevice]);
          // Auto-select the device since we only get one from Web Bluetooth picker
          this.selectedDevice.set(bleDevice);
          this.scanState.set('idle');
        } else {
          // User cancelled or error occurred
          const error = this.bleService.lastError();
          if (error) {
            this.errorMessage.set(error);
            this.scanState.set('error');
          } else {
            // User cancelled - just go back to idle
            this.scanState.set('idle');
          }
        }
      } catch (error: unknown) {
        console.error('BLE scan error:', error);
        // Fallback to simulation
        this.startScanSimulation();
      }
    } else {
      // Fallback: Simulate scanning for demo purposes
      this.startScanSimulation();
    }
  }

  private startScanSimulation(): void {
    // Simulate device discovery
    const simulatedDevices: BleDevice[] = [];
    let scanCount = 0;

    this.scanInterval = setInterval(() => {
      scanCount++;

      // Add simulated devices progressively
      if (scanCount === 2) {
        simulatedDevices.push({
          id: 'sim-001',
          name: 'myIoTGrid Sensor A',
          rssi: -45,
          macAddress: 'AA:BB:CC:DD:EE:01',
          nodeId: 'ESP32-AABBCCDDEE01'
        });
        this.devices.set([...simulatedDevices]);
      }

      if (scanCount === 4) {
        simulatedDevices.push({
          id: 'sim-002',
          name: 'ESP32-Wohnzimmer',
          rssi: -62,
          macAddress: 'AA:BB:CC:DD:EE:02',
          nodeId: 'ESP32-AABBCCDDEE02'
        });
        this.devices.set([...simulatedDevices]);
      }

      if (scanCount === 6) {
        simulatedDevices.push({
          id: 'sim-003',
          name: 'SIM-Sensor-Test',
          rssi: -78,
          macAddress: 'AA:BB:CC:DD:EE:03',
          nodeId: 'ESP32-AABBCCDDEE03'
        });
        this.devices.set([...simulatedDevices]);
      }

      if (scanCount >= 8) {
        this.stopScanSimulation();
        this.scanState.set('idle');
      }
    }, 500);
  }

  private stopScanSimulation(): void {
    if (this.scanInterval) {
      clearInterval(this.scanInterval);
      this.scanInterval = null;
    }
  }

  stopScan(): void {
    this.stopScanSimulation();
    this.scanState.set('idle');
  }

  selectDevice(device: BleDevice): void {
    this.selectedDevice.set(device);
  }

  async connectToDevice(): Promise<void> {
    const device = this.selectedDevice();
    if (!device) return;

    this.scanState.set('connecting');
    this.errorMessage.set(null);

    try {
      // Use the BleCommunicationService to establish real GATT connection
      const connected = await this.bleService.connect();

      if (connected) {
        // Get the connected device info from the service
        const connectedDevice = this.bleService.connectedDevice();
        if (connectedDevice) {
          // Update the device with real MAC address and nodeId from ESP32
          const updatedDevice: BleDevice = {
            ...device,
            macAddress: connectedDevice.macAddress,
            nodeId: connectedDevice.nodeId
          };
          this.wizardService.setBleDevice(updatedDevice);
        } else {
          // Fallback: generate nodeId from device name/id if no connectedDevice
          const fallbackDevice: BleDevice = {
            ...device,
            nodeId: `ESP32-${device.macAddress.replace(/:/g, '').toUpperCase()}`
          };
          this.wizardService.setBleDevice(fallbackDevice);
        }

        this.scanState.set('connected');

        // Auto-advance after brief delay
        setTimeout(() => {
          this.wizardService.nextStep();
        }, 800);
      } else {
        this.scanState.set('error');
        this.errorMessage.set(this.bleService.lastError() || 'Verbindung fehlgeschlagen');
      }
    } catch (error) {
      console.error('BLE connection error:', error);
      this.scanState.set('error');
      this.errorMessage.set('Verbindung zum Gerät fehlgeschlagen');
    }
  }

  getSignalStrengthIcon(rssi: number): string {
    if (rssi > -50) return 'signal_cellular_4_bar';
    if (rssi > -60) return 'signal_cellular_3_bar';
    if (rssi > -70) return 'signal_cellular_2_bar';
    return 'signal_cellular_1_bar';
  }

  getSignalStrengthClass(rssi: number): string {
    if (rssi > -50) return 'signal-excellent';
    if (rssi > -60) return 'signal-good';
    if (rssi > -70) return 'signal-fair';
    return 'signal-weak';
  }

  private generateMacFromId(id: string): string {
    // Generate a pseudo-MAC from device ID
    const hash = id.split('').reduce((a, b) => {
      a = ((a << 5) - a) + b.charCodeAt(0);
      return a & a;
    }, 0);

    const bytes = [
      (hash >> 24) & 0xff,
      (hash >> 16) & 0xff,
      (hash >> 8) & 0xff,
      hash & 0xff,
      (hash >> 12) & 0xff,
      (hash >> 4) & 0xff
    ];

    return bytes.map(b => b.toString(16).padStart(2, '0').toUpperCase()).join(':');
  }

  onBack(): void {
    this.wizardService.previousStep();
  }

  onCancel(): void {
    this.wizardService.exitWizard();
  }
}
