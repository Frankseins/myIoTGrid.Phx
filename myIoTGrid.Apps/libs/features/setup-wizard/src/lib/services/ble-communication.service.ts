import { Injectable, signal } from '@angular/core';

/**
 * BLE Service UUIDs for myIoTGrid Sensor communication
 */
export const BLE_UUIDS = {
  SERVICE: '4fafc201-1fb5-459e-8fcc-c5c9c331914b',
  CHAR_REGISTRATION: 'beb5483e-36e1-4688-b7f5-ea07361b26a8',
  CHAR_WIFI_CONFIG: 'beb5483e-36e1-4688-b7f5-ea07361b26a9',
  CHAR_API_CONFIG: 'beb5483e-36e1-4688-b7f5-ea07361b26aa',
  CHAR_STATUS: 'beb5483e-36e1-4688-b7f5-ea07361b26ab'
};

/**
 * WiFi configuration to send to ESP32
 */
export interface WifiConfig {
  ssid: string;
  password: string;
}

/**
 * API configuration to send to ESP32
 */
export interface ApiConfig {
  nodeId: string;
  apiKey: string;
  hubUrl: string;
}

/**
 * Registration data from ESP32
 */
export interface NodeRegistration {
  nodeId: string;         // ESP32-generated unique ID (ESP32-<WiFi-MAC>)
  macAddress: string;     // WiFi MAC address
  firmwareVersion: string;
}

/**
 * BLE connection state
 */
export type BleConnectionState = 'disconnected' | 'connecting' | 'connected' | 'error';

/**
 * Device provisioning mode (Story 5 - Sprint S1.2)
 * - 'pairing': Initial pairing mode (new sensor)
 * - 're-pairing': RE_PAIRING mode (device name ends with -SETUP)
 * - 'unknown': Mode not yet determined
 */
export type DeviceProvisioningMode = 'pairing' | 're-pairing' | 'unknown';

/**
 * Service for Web Bluetooth communication with ESP32 sensors
 */
@Injectable({
  providedIn: 'root'
})
export class BleCommunicationService {
  private device: BluetoothDevice | null = null;
  private server: BluetoothRemoteGATTServer | null = null;
  private service: BluetoothRemoteGATTService | null = null;
  private wifiCharacteristic: BluetoothRemoteGATTCharacteristic | null = null;
  private apiCharacteristic: BluetoothRemoteGATTCharacteristic | null = null;
  private registrationCharacteristic: BluetoothRemoteGATTCharacteristic | null = null;
  private statusCharacteristic: BluetoothRemoteGATTCharacteristic | null = null;

  readonly connectionState = signal<BleConnectionState>('disconnected');
  readonly connectedDevice = signal<{ id: string; name: string; macAddress: string; nodeId: string } | null>(null);
  readonly lastError = signal<string | null>(null);

  /**
   * Current device provisioning mode (Story 5 - RE_PAIRING detection)
   * Determined by checking if device name ends with "-SETUP"
   */
  readonly provisioningMode = signal<DeviceProvisioningMode>('unknown');

  /**
   * Helper: Check if currently connected device is in RE_PAIRING mode
   */
  isRePairingMode(): boolean {
    return this.provisioningMode() === 're-pairing';
  }

  /**
   * Helper: Check if device name indicates RE_PAIRING mode
   * Device names with "-SETUP" suffix indicate the sensor is in RE_PAIRING state
   */
  private detectProvisioningMode(deviceName: string | undefined): DeviceProvisioningMode {
    if (!deviceName) return 'unknown';

    // RE_PAIRING mode: device name ends with "-SETUP"
    // Example: "myIoTGrid-A1B2-SETUP" or "ESP32-001122334455-SETUP"
    if (deviceName.endsWith('-SETUP')) {
      console.log('[BLE] RE_PAIRING mode detected (device name ends with -SETUP)');
      return 're-pairing';
    }

    // Normal pairing mode
    return 'pairing';
  }

  /**
   * Check if Web Bluetooth is supported
   */
  isSupported(): boolean {
    return 'bluetooth' in navigator;
  }

  /**
   * Request device selection and connect
   */
  async requestDevice(): Promise<BluetoothDevice | null> {
    if (!this.isSupported()) {
      this.lastError.set('Web Bluetooth wird nicht unterstützt');
      return null;
    }

    try {
      this.connectionState.set('connecting');
      this.lastError.set(null);

      const bluetooth = navigator.bluetooth;
      const device = await bluetooth.requestDevice({
        filters: [
          { namePrefix: 'myIoTGrid' },
          { namePrefix: 'ESP32' },
          { namePrefix: 'SIM-' }
        ],
        optionalServices: [BLE_UUIDS.SERVICE]
      });

      this.device = device;

      // Detect provisioning mode based on device name (Story 5 - RE_PAIRING)
      const mode = this.detectProvisioningMode(device.name);
      this.provisioningMode.set(mode);
      console.log('[BLE] Device provisioning mode:', mode, '(device name:', device.name, ')');

      // Listen for disconnection
      device.addEventListener('gattserverdisconnected', () => {
        this.handleDisconnection();
      });

      return device;
    } catch (error) {
      this.connectionState.set('disconnected');
      if (error instanceof DOMException) {
        if (error.name === 'NotFoundError') {
          this.lastError.set('Kein Gerät gefunden');
        } else if (error.name === 'NotAllowedError') {
          // User cancelled - not an error
          return null;
        } else {
          this.lastError.set(`Bluetooth-Fehler: ${error.message}`);
        }
      } else {
        this.lastError.set('Unbekannter Bluetooth-Fehler');
      }
      return null;
    }
  }

  /**
   * Connect to the selected device's GATT server
   */
  async connect(): Promise<boolean> {
    if (!this.device || !this.device.gatt) {
      this.lastError.set('Kein Gerät ausgewählt');
      return false;
    }

    try {
      this.connectionState.set('connecting');
      this.lastError.set(null);

      // Connect to GATT server
      this.server = await this.device.gatt.connect();
      console.log('[BLE] GATT server connected');

      // Get the service
      this.service = await this.server.getPrimaryService(BLE_UUIDS.SERVICE);
      console.log('[BLE] Service obtained');

      // Get all characteristics
      this.wifiCharacteristic = await this.service.getCharacteristic(BLE_UUIDS.CHAR_WIFI_CONFIG);
      console.log('[BLE] WiFi characteristic obtained');

      this.apiCharacteristic = await this.service.getCharacteristic(BLE_UUIDS.CHAR_API_CONFIG);
      console.log('[BLE] API characteristic obtained');

      this.registrationCharacteristic = await this.service.getCharacteristic(BLE_UUIDS.CHAR_REGISTRATION);
      console.log('[BLE] Registration characteristic obtained');

      this.statusCharacteristic = await this.service.getCharacteristic(BLE_UUIDS.CHAR_STATUS);
      console.log('[BLE] Status characteristic obtained');

      // Read registration data to get node_id and MAC address from ESP32
      // CRITICAL: We MUST get the real nodeId from ESP32, no fallback allowed!
      const registration = await this.readRegistrationWithRetry();

      if (!registration || !registration.nodeId) {
        console.error('[BLE] Failed to read registration from ESP32 - cannot proceed without real nodeId');
        this.connectionState.set('error');
        this.lastError.set('Konnte Registrierungsdaten vom Sensor nicht lesen. Bitte Sensor neu starten.');
        return false;
      }

      // Validation is already done in readRegistration()
      this.connectionState.set('connected');
      this.connectedDevice.set({
        id: this.device.id,
        name: this.device.name || 'ESP32 Sensor',
        macAddress: registration.macAddress,
        nodeId: registration.nodeId  // ESP32-generated unique ID from WiFi MAC
      });

      console.log('[BLE] ESP32 NodeId (verified):', registration.nodeId);
      console.log('[BLE] ESP32 MAC Address:', registration.macAddress);
      console.log('[BLE] Connected to device:', this.connectedDevice());
      return true;
    } catch (error) {
      console.error('[BLE] Connection error:', error);
      this.connectionState.set('error');
      if (error instanceof Error) {
        this.lastError.set(`Verbindungsfehler: ${error.message}`);
      } else {
        this.lastError.set('Verbindung fehlgeschlagen');
      }
      return false;
    }
  }

  /**
   * Disconnect from the device
   */
  disconnect(): void {
    if (this.server && this.server.connected) {
      this.server.disconnect();
    }
    this.handleDisconnection();
  }

  /**
   * Read registration data from ESP32 with retry logic
   * This is critical - we must get the real nodeId from ESP32
   */
  async readRegistrationWithRetry(maxRetries = 3, delayMs = 500): Promise<NodeRegistration | null> {
    for (let attempt = 1; attempt <= maxRetries; attempt++) {
      console.log(`[BLE] Reading registration (attempt ${attempt}/${maxRetries})...`);
      const registration = await this.readRegistration();

      if (registration && registration.nodeId && registration.macAddress) {
        console.log(`[BLE] Registration read successfully on attempt ${attempt}:`, registration);
        return registration;
      }

      if (attempt < maxRetries) {
        console.log(`[BLE] Registration read failed, retrying in ${delayMs}ms...`);
        await new Promise(resolve => setTimeout(resolve, delayMs));
      }
    }

    console.error(`[BLE] Failed to read registration after ${maxRetries} attempts`);
    return null;
  }

  /**
   * Read registration data from ESP32
   */
  async readRegistration(): Promise<NodeRegistration | null> {
    if (!this.registrationCharacteristic) {
      console.warn('[BLE] Registration characteristic not available');
      return null;
    }

    try {
      const value = await this.registrationCharacteristic.readValue();

      // Convert to string and clean up - remove null bytes and non-printable chars
      const rawBytes = new Uint8Array(value.buffer);
      console.log('[BLE] Registration raw bytes:', Array.from(rawBytes).map(b => b.toString(16).padStart(2, '0')).join(' '));

      // Decode and strip null bytes and non-printable characters
      let text = new TextDecoder().decode(value);
      text = text.replace(/\x00/g, '').replace(/[^\x20-\x7E]/g, '').trim();

      console.log('[BLE] Registration data cleaned:', text, 'length:', text.length);

      // Check if we got valid data
      if (!text || text.length === 0) {
        console.warn('[BLE] Registration characteristic returned empty data');
        return null;
      }

      // ESP32 sends just the nodeId as plain string: "ESP32-XXXXXXXXXXXX"
      // Try to extract it even if there's extra data
      const nodeIdMatch = text.match(/ESP32-[0-9A-Fa-f]{12}/);
      if (!nodeIdMatch) {
        console.error('[BLE] Could not find valid nodeId in:', text);
        return null;
      }

      const nodeId = nodeIdMatch[0];

      // Extract MAC address from nodeId (everything after "ESP32-")
      const macAddress = nodeId.substring(6).toUpperCase();

      console.log('[BLE] Parsed registration:', { nodeId, macAddress });

      return {
        nodeId: nodeId,
        macAddress: macAddress,
        firmwareVersion: ''  // Not sent via BLE anymore
      };
    } catch (error) {
      console.error('[BLE] Failed to read registration:', error);
      return null;
    }
  }

  /**
   * Send WiFi configuration to ESP32
   */
  async sendWifiConfig(config: WifiConfig): Promise<boolean> {
    if (!this.wifiCharacteristic) {
      this.lastError.set('WiFi-Charakteristik nicht verfügbar');
      return false;
    }

    try {
      const json = JSON.stringify({
        ssid: config.ssid,
        password: config.password
      });

      console.log('[BLE] Sending WiFi config:', config.ssid);
      const encoder = new TextEncoder();
      await this.wifiCharacteristic.writeValue(encoder.encode(json));
      console.log('[BLE] WiFi config sent successfully');
      return true;
    } catch (error) {
      console.error('[BLE] Failed to send WiFi config:', error);
      if (error instanceof Error) {
        this.lastError.set(`WiFi-Konfiguration fehlgeschlagen: ${error.message}`);
      }
      return false;
    }
  }

  /**
   * Send API configuration to ESP32
   */
  async sendApiConfig(config: ApiConfig): Promise<boolean> {
    if (!this.apiCharacteristic) {
      this.lastError.set('API-Charakteristik nicht verfügbar');
      return false;
    }

    try {
      // Use snake_case keys as expected by ESP32 firmware
      const json = JSON.stringify({
        node_id: config.nodeId,
        api_key: config.apiKey,
        hub_url: config.hubUrl
      });

      console.log('[BLE] Sending API config:', { node_id: config.nodeId, hub_url: config.hubUrl });
      const encoder = new TextEncoder();
      await this.apiCharacteristic.writeValue(encoder.encode(json));
      console.log('[BLE] API config sent successfully');
      return true;
    } catch (error) {
      console.error('[BLE] Failed to send API config:', error);
      if (error instanceof Error) {
        this.lastError.set(`API-Konfiguration fehlgeschlagen: ${error.message}`);
      }
      return false;
    }
  }

  /**
   * Read status from ESP32
   */
  async readStatus(): Promise<string | null> {
    if (!this.statusCharacteristic) {
      return null;
    }

    try {
      const value = await this.statusCharacteristic.readValue();
      return new TextDecoder().decode(value);
    } catch (error) {
      console.error('[BLE] Failed to read status:', error);
      return null;
    }
  }

  /**
   * Subscribe to status notifications
   */
  async subscribeToStatus(callback: (status: string) => void): Promise<boolean> {
    if (!this.statusCharacteristic) {
      return false;
    }

    try {
      await this.statusCharacteristic.startNotifications();
      this.statusCharacteristic.addEventListener('characteristicvaluechanged', (event) => {
        const target = event.target as unknown as BluetoothRemoteGATTCharacteristic;
        if (target.value) {
          const status = new TextDecoder().decode(target.value);
          callback(status);
        }
      });
      return true;
    } catch (error) {
      console.error('[BLE] Failed to subscribe to status:', error);
      return false;
    }
  }

  private handleDisconnection(): void {
    this.server = null;
    this.service = null;
    this.wifiCharacteristic = null;
    this.apiCharacteristic = null;
    this.registrationCharacteristic = null;
    this.statusCharacteristic = null;
    this.connectionState.set('disconnected');
    this.connectedDevice.set(null);
    this.provisioningMode.set('unknown');  // Reset provisioning mode (Story 5)
    console.log('[BLE] Disconnected');
  }

  private generateMacFromId(id: string): string {
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
}

// Web Bluetooth API type declarations
declare global {
  interface Navigator {
    bluetooth: Bluetooth;
  }

  interface Bluetooth {
    requestDevice(options: RequestDeviceOptions): Promise<BluetoothDevice>;
  }

  interface RequestDeviceOptions {
    filters?: BluetoothLEScanFilter[];
    optionalServices?: BluetoothServiceUUID[];
    acceptAllDevices?: boolean;
  }

  interface BluetoothLEScanFilter {
    name?: string;
    namePrefix?: string;
    services?: BluetoothServiceUUID[];
  }

  type BluetoothServiceUUID = string | number;

  interface BluetoothDevice {
    id: string;
    name?: string;
    gatt?: BluetoothRemoteGATTServer;
    addEventListener(type: 'gattserverdisconnected', listener: () => void): void;
  }

  interface BluetoothRemoteGATTServer {
    connected: boolean;
    connect(): Promise<BluetoothRemoteGATTServer>;
    disconnect(): void;
    getPrimaryService(service: BluetoothServiceUUID): Promise<BluetoothRemoteGATTService>;
  }

  interface BluetoothRemoteGATTService {
    getCharacteristic(characteristic: BluetoothServiceUUID): Promise<BluetoothRemoteGATTCharacteristic>;
  }

  interface BluetoothRemoteGATTCharacteristic {
    value: DataView | null;
    readValue(): Promise<DataView>;
    writeValue(value: BufferSource): Promise<void>;
    startNotifications(): Promise<BluetoothRemoteGATTCharacteristic>;
    addEventListener(type: 'characteristicvaluechanged', listener: (event: Event) => void): void;
  }
}

// Ensure this file is treated as a module
export {};
