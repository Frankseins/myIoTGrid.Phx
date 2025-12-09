import { Injectable, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { Node, CreateNodeDto, CreateSensorDto, Protocol } from '@myiotgrid/shared/models';

/**
 * Wizard step identifiers
 */
export type WizardStep = 'welcome' | 'ble-pairing' | 'wifi-setup' | 'node-info' | 'first-sensor' | 'success';

/**
 * BLE device info from scanning
 */
export interface BleDevice {
  id: string;
  name: string;
  rssi: number;
  macAddress: string;
  nodeId: string;  // ESP32-generated unique ID (format: ESP32-<WiFi-MAC>)
}

/**
 * WiFi network info
 */
export interface WifiNetwork {
  ssid: string;
  rssi: number;
  secured: boolean;
}

/**
 * WiFi credentials
 */
export interface WifiCredentials {
  ssid: string;
  password: string;
}

/**
 * Node info from wizard
 */
export interface NodeInfo {
  name: string;
  location: string;
  icon: string;
}

/**
 * Complete wizard state
 */
export interface WizardState {
  currentStep: WizardStep;
  bleDevice: BleDevice | null;
  wifiCredentials: WifiCredentials | null;
  nodeInfo: NodeInfo | null;
  sensor: CreateSensorDto | null;
  createdNode: Node | null;
  isComplete: boolean;
  error: string | null;
}

const INITIAL_STATE: WizardState = {
  currentStep: 'welcome',
  bleDevice: null,
  wifiCredentials: null,
  nodeInfo: null,
  sensor: null,
  createdNode: null,
  isComplete: false,
  error: null
};

const STEP_ORDER: WizardStep[] = [
  'welcome',
  'ble-pairing',
  'node-info',      // Node info BEFORE wifi-setup so node can be created first
  'wifi-setup',
  'first-sensor',
  'success'
];

@Injectable({
  providedIn: 'root'
})
export class SetupWizardService {
  private readonly router: Router;

  // State signals
  private readonly _state = signal<WizardState>({ ...INITIAL_STATE });

  // Public selectors
  readonly state = this._state.asReadonly();
  readonly currentStep = computed(() => this._state().currentStep);
  readonly bleDevice = computed(() => this._state().bleDevice);
  readonly wifiCredentials = computed(() => this._state().wifiCredentials);
  readonly nodeInfo = computed(() => this._state().nodeInfo);
  readonly sensor = computed(() => this._state().sensor);
  readonly createdNode = computed(() => this._state().createdNode);
  readonly isComplete = computed(() => this._state().isComplete);
  readonly error = computed(() => this._state().error);

  readonly currentStepIndex = computed(() => STEP_ORDER.indexOf(this._state().currentStep));
  readonly totalSteps = STEP_ORDER.length;
  readonly progress = computed(() => ((this.currentStepIndex() + 1) / this.totalSteps) * 100);

  readonly canGoBack = computed(() => this.currentStepIndex() > 0 && this._state().currentStep !== 'success');
  readonly canGoNext = computed(() => this.currentStepIndex() < this.totalSteps - 1);

  constructor(router: Router) {
    this.router = router;
  }

  /**
   * Reset wizard to initial state
   */
  reset(): void {
    this._state.set({ ...INITIAL_STATE });
  }

  /**
   * Navigate to specific step
   */
  goToStep(step: WizardStep): void {
    this._state.update(state => ({
      ...state,
      currentStep: step,
      error: null
    }));
  }

  /**
   * Go to next step
   */
  nextStep(): void {
    const currentIndex = this.currentStepIndex();
    if (currentIndex < STEP_ORDER.length - 1) {
      this.goToStep(STEP_ORDER[currentIndex + 1]);
    }
  }

  /**
   * Go to previous step
   */
  previousStep(): void {
    const currentIndex = this.currentStepIndex();
    if (currentIndex > 0) {
      this.goToStep(STEP_ORDER[currentIndex - 1]);
    }
  }

  /**
   * Save BLE device selection
   */
  setBleDevice(device: BleDevice): void {
    this._state.update(state => ({
      ...state,
      bleDevice: device,
      error: null
    }));
  }

  /**
   * Save WiFi credentials
   */
  setWifiCredentials(credentials: WifiCredentials): void {
    this._state.update(state => ({
      ...state,
      wifiCredentials: credentials,
      error: null
    }));
  }

  /**
   * Save node info
   */
  setNodeInfo(info: NodeInfo): void {
    this._state.update(state => ({
      ...state,
      nodeInfo: info,
      error: null
    }));
  }

  /**
   * Save sensor configuration
   */
  setSensor(sensor: CreateSensorDto | null): void {
    this._state.update(state => ({
      ...state,
      sensor: sensor,
      error: null
    }));
  }

  /**
   * Store a reference to the created node (before wizard is complete)
   * Used when node is created early in the flow (after node-info step)
   */
  setCreatedNode(node: Node): void {
    this._state.update(state => ({
      ...state,
      createdNode: node
    }));
  }

  /**
   * Mark wizard as complete with created node
   */
  complete(node: Node): void {
    this._state.update(state => ({
      ...state,
      createdNode: node,
      isComplete: true,
      currentStep: 'success'
    }));
  }

  /**
   * Mark wizard as complete (node was already created)
   */
  completeWithExistingNode(): void {
    this._state.update(state => ({
      ...state,
      isComplete: true,
      currentStep: 'success'
    }));
  }

  /**
   * Set error message
   */
  setError(error: string): void {
    this._state.update(state => ({
      ...state,
      error
    }));
  }

  /**
   * Build CreateNodeDto from wizard state
   */
  buildCreateNodeDto(): CreateNodeDto | null {
    const state = this._state();
    if (!state.bleDevice || !state.nodeInfo) {
      return null;
    }

    return {
      nodeId: state.bleDevice.nodeId,  // Use ESP32-generated nodeId (format: ESP32-<WiFi-MAC>)
      name: state.nodeInfo.name,
      hubIdentifier: 'my-iot-hub', // Default hub identifier
      protocol: Protocol.WLAN,
      location: state.nodeInfo.location ? { name: state.nodeInfo.location } : undefined
    };
  }

  /**
   * Skip sensor configuration step - completes the wizard since node is already created
   */
  skipSensorStep(): void {
    this.setSensor(null);
    this.completeWithExistingNode();
  }

  /**
   * Exit wizard and navigate to nodes list
   */
  exitWizard(): void {
    this.reset();
    this.router.navigate(['/nodes']);
  }

  /**
   * Exit wizard and navigate to created node
   */
  goToCreatedNode(): void {
    const node = this._state().createdNode;
    this.reset();
    if (node) {
      this.router.navigate(['/nodes', node.id]);
    } else {
      this.router.navigate(['/nodes']);
    }
  }
}
