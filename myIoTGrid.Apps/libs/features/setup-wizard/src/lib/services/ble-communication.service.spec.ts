import { TestBed } from '@angular/core/testing';
import {
  BleCommunicationService,
  BLE_UUIDS,
  DeviceProvisioningMode
} from './ble-communication.service';

describe('BleCommunicationService', () => {
  let service: BleCommunicationService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [BleCommunicationService]
    });
    service = TestBed.inject(BleCommunicationService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('BLE_UUIDS', () => {
    it('should have correct SERVICE UUID', () => {
      expect(BLE_UUIDS.SERVICE).toBe('4fafc201-1fb5-459e-8fcc-c5c9c331914b');
    });

    it('should have correct CHAR_REGISTRATION UUID', () => {
      expect(BLE_UUIDS.CHAR_REGISTRATION).toBe('beb5483e-36e1-4688-b7f5-ea07361b26a8');
    });

    it('should have correct CHAR_WIFI_CONFIG UUID', () => {
      expect(BLE_UUIDS.CHAR_WIFI_CONFIG).toBe('beb5483e-36e1-4688-b7f5-ea07361b26a9');
    });

    it('should have correct CHAR_API_CONFIG UUID', () => {
      expect(BLE_UUIDS.CHAR_API_CONFIG).toBe('beb5483e-36e1-4688-b7f5-ea07361b26aa');
    });

    it('should have correct CHAR_STATUS UUID', () => {
      expect(BLE_UUIDS.CHAR_STATUS).toBe('beb5483e-36e1-4688-b7f5-ea07361b26ab');
    });
  });

  describe('Initial State', () => {
    it('should have disconnected connection state initially', () => {
      expect(service.connectionState()).toBe('disconnected');
    });

    it('should have null connected device initially', () => {
      expect(service.connectedDevice()).toBeNull();
    });

    it('should have no error initially', () => {
      expect(service.lastError()).toBeNull();
    });

    it('should have unknown provisioning mode initially', () => {
      expect(service.provisioningMode()).toBe('unknown');
    });
  });

  describe('isRePairingMode()', () => {
    it('should return false when provisioning mode is unknown', () => {
      expect(service.isRePairingMode()).toBe(false);
    });

    it('should return false when provisioning mode is pairing', () => {
      // Access private method via type assertion for testing
      (service as any).provisioningMode.set('pairing');
      expect(service.isRePairingMode()).toBe(false);
    });

    it('should return true when provisioning mode is re-pairing', () => {
      // Access private method via type assertion for testing
      (service as any).provisioningMode.set('re-pairing');
      expect(service.isRePairingMode()).toBe(true);
    });
  });

  describe('detectProvisioningMode() - Story 5: RE_PAIRING Erkennung', () => {
    // Access private method via type assertion for testing
    const callDetectProvisioningMode = (service: BleCommunicationService, deviceName: string | undefined): DeviceProvisioningMode => {
      return (service as any).detectProvisioningMode(deviceName);
    };

    it('should return unknown for undefined device name', () => {
      const mode = callDetectProvisioningMode(service, undefined);
      expect(mode).toBe('unknown');
    });

    it('should return unknown for empty device name', () => {
      const mode = callDetectProvisioningMode(service, '');
      expect(mode).toBe('unknown');
    });

    it('should return pairing for normal myIoTGrid device name', () => {
      const mode = callDetectProvisioningMode(service, 'myIoTGrid-A1B2C3');
      expect(mode).toBe('pairing');
    });

    it('should return pairing for normal ESP32 device name', () => {
      const mode = callDetectProvisioningMode(service, 'ESP32-001122334455');
      expect(mode).toBe('pairing');
    });

    it('should return re-pairing for device name ending with -SETUP (myIoTGrid)', () => {
      const mode = callDetectProvisioningMode(service, 'myIoTGrid-A1B2C3-SETUP');
      expect(mode).toBe('re-pairing');
    });

    it('should return re-pairing for device name ending with -SETUP (ESP32)', () => {
      const mode = callDetectProvisioningMode(service, 'ESP32-001122334455-SETUP');
      expect(mode).toBe('re-pairing');
    });

    it('should return re-pairing for SIM device name ending with -SETUP', () => {
      const mode = callDetectProvisioningMode(service, 'SIM-001122-SETUP');
      expect(mode).toBe('re-pairing');
    });

    it('should return pairing when -SETUP is in middle of name', () => {
      const mode = callDetectProvisioningMode(service, 'myIoTGrid-SETUP-A1B2C3');
      expect(mode).toBe('pairing');
    });

    it('should be case-sensitive for -SETUP suffix', () => {
      // Only exact "-SETUP" suffix triggers re-pairing mode
      const mode1 = callDetectProvisioningMode(service, 'myIoTGrid-A1B2C3-setup');
      expect(mode1).toBe('pairing');

      const mode2 = callDetectProvisioningMode(service, 'myIoTGrid-A1B2C3-Setup');
      expect(mode2).toBe('pairing');
    });
  });

  describe('isSupported()', () => {
    it('should return false when Web Bluetooth is not available', () => {
      // Mock navigator.bluetooth to be undefined
      const originalBluetooth = (navigator as any).bluetooth;
      delete (navigator as any).bluetooth;

      expect(service.isSupported()).toBe(false);

      // Restore
      if (originalBluetooth) {
        (navigator as any).bluetooth = originalBluetooth;
      }
    });

    it('should return true when Web Bluetooth is available', () => {
      // Mock navigator.bluetooth
      (navigator as any).bluetooth = {};

      expect(service.isSupported()).toBe(true);

      // Cleanup
      delete (navigator as any).bluetooth;
    });
  });

  describe('disconnect()', () => {
    it('should reset all signals on disconnect', () => {
      // Set some state
      (service as any).provisioningMode.set('re-pairing');
      (service as any).connectionState.set('connected');

      // Call disconnect through handleDisconnection
      (service as any).handleDisconnection();

      expect(service.connectionState()).toBe('disconnected');
      expect(service.connectedDevice()).toBeNull();
      expect(service.provisioningMode()).toBe('unknown');
    });
  });
});
