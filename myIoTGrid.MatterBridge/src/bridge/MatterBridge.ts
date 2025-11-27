import {
    ServerNode,
    Environment,
    StorageService,
    VendorId,
    Endpoint,
    EndpointServer
} from "@matter/main";
import {
    AggregatorEndpoint,
    BridgedNodeEndpoint,
    TemperatureSensorDevice,
    HumiditySensorDevice,
    ContactSensorDevice,
    PressureMeasurementServer
} from "@matter/main/devices";
import { StorageBackendDisk } from "@matter/nodejs";
import { Logger } from "winston";
import { DeviceFactory, DeviceType, MatterDevice } from "./DeviceFactory";

export interface BridgeConfig {
    vendorId: number;
    vendorName: string;
    productName: string;
    productId: number;
    storagePath: string;
    port: number;
    discriminator: number;
    passcode: number;
}

export interface DeviceInfo {
    sensorId: string;
    name: string;
    type: DeviceType;
    location?: string;
    endpoint?: Endpoint;
}

export class MatterBridge {
    private server?: ServerNode;
    private aggregator?: Endpoint<typeof AggregatorEndpoint>;
    private devices: Map<string, DeviceInfo> = new Map();
    private config: BridgeConfig;
    private logger: Logger;
    private isStarted: boolean = false;
    private deviceFactory: DeviceFactory;

    constructor(config: BridgeConfig, logger: Logger) {
        this.config = config;
        this.logger = logger;
        this.deviceFactory = new DeviceFactory(logger);
    }

    async start(): Promise<void> {
        if (this.isStarted) {
            this.logger.warn("Matter Bridge is already started");
            return;
        }

        this.logger.info("Starting Matter Bridge...");

        try {
            // Initialize Matter environment
            const environment = Environment.default;

            // Setup storage
            const storagePath = this.config.storagePath;
            environment.get(StorageService).factory = (namespace) =>
                new StorageBackendDisk(storagePath, namespace);

            // Create the server node (bridge)
            this.server = await ServerNode.create({
                id: "myiotgrid-bridge",
                network: {
                    port: this.config.port,
                },
                commissioning: {
                    passcode: this.config.passcode,
                    discriminator: this.config.discriminator,
                },
                productDescription: {
                    name: this.config.productName,
                    deviceType: AggregatorEndpoint.deviceType,
                },
                basicInformation: {
                    vendorId: VendorId(this.config.vendorId),
                    vendorName: this.config.vendorName,
                    productId: this.config.productId,
                    productName: this.config.productName,
                    productLabel: this.config.productName,
                    serialNumber: "MYIOTGRID-001",
                    uniqueId: "myiotgrid-bridge-001",
                },
            });

            // Create aggregator endpoint (bridge device)
            this.aggregator = new Endpoint(AggregatorEndpoint, { id: "aggregator" });
            await this.server.add(this.aggregator);

            // Start the server
            await this.server.start();

            this.isStarted = true;
            this.logger.info("Matter Bridge started successfully");
            this.logger.info(`Pairing code: ${this.config.passcode}`);
            this.logger.info(`Discriminator: ${this.config.discriminator}`);

        } catch (error) {
            this.logger.error("Failed to start Matter Bridge", { error });
            throw error;
        }
    }

    async stop(): Promise<void> {
        if (!this.isStarted || !this.server) {
            return;
        }

        this.logger.info("Stopping Matter Bridge...");

        try {
            await this.server.close();
            this.isStarted = false;
            this.devices.clear();
            this.logger.info("Matter Bridge stopped");
        } catch (error) {
            this.logger.error("Error stopping Matter Bridge", { error });
            throw error;
        }
    }

    async registerDevice(
        sensorId: string,
        name: string,
        type: DeviceType,
        location?: string
    ): Promise<void> {
        if (!this.isStarted || !this.aggregator) {
            throw new Error("Matter Bridge is not started");
        }

        if (this.devices.has(sensorId)) {
            this.logger.warn(`Device ${sensorId} already registered`);
            return;
        }

        const displayName = location ? `${location}: ${name}` : name;
        this.logger.info(`Registering device: ${displayName} (${type})`);

        try {
            const endpoint = await this.deviceFactory.createDevice(
                sensorId,
                displayName,
                type
            );

            await this.aggregator.add(endpoint);

            this.devices.set(sensorId, {
                sensorId,
                name: displayName,
                type,
                location,
                endpoint
            });

            this.logger.info(`Device ${displayName} registered successfully`);
        } catch (error) {
            this.logger.error(`Failed to register device ${sensorId}`, { error });
            throw error;
        }
    }

    async updateDeviceValue(
        sensorId: string,
        sensorType: string,
        value: number
    ): Promise<void> {
        const device = this.devices.get(sensorId);
        if (!device || !device.endpoint) {
            this.logger.debug(`Device ${sensorId} not found, skipping update`);
            return;
        }

        try {
            await this.deviceFactory.updateDeviceValue(
                device.endpoint,
                device.type,
                sensorType,
                value
            );
            this.logger.debug(`Updated ${sensorId} ${sensorType}: ${value}`);
        } catch (error) {
            this.logger.error(`Failed to update device ${sensorId}`, { error });
        }
    }

    async removeDevice(sensorId: string): Promise<void> {
        const device = this.devices.get(sensorId);
        if (!device) {
            return;
        }

        try {
            if (device.endpoint && this.aggregator) {
                // Remove endpoint from aggregator
                await device.endpoint.close();
            }
            this.devices.delete(sensorId);
            this.logger.info(`Device ${sensorId} removed`);
        } catch (error) {
            this.logger.error(`Failed to remove device ${sensorId}`, { error });
            throw error;
        }
    }

    async setContactSensorState(sensorId: string, isOpen: boolean): Promise<void> {
        const device = this.devices.get(sensorId);
        if (!device || !device.endpoint || device.type !== DeviceType.ContactSensor) {
            this.logger.debug(`Contact sensor ${sensorId} not found`);
            return;
        }

        try {
            await this.deviceFactory.setContactState(device.endpoint, isOpen);
            this.logger.debug(`Contact sensor ${sensorId} set to ${isOpen ? 'OPEN' : 'CLOSED'}`);
        } catch (error) {
            this.logger.error(`Failed to update contact sensor ${sensorId}`, { error });
        }
    }

    getStatus(): {
        isStarted: boolean;
        deviceCount: number;
        devices: Array<{ sensorId: string; name: string; type: string; location?: string }>;
        pairingCode: number;
        discriminator: number;
    } {
        return {
            isStarted: this.isStarted,
            deviceCount: this.devices.size,
            devices: Array.from(this.devices.values()).map(d => ({
                sensorId: d.sensorId,
                name: d.name,
                type: d.type,
                location: d.location
            })),
            pairingCode: this.config.passcode,
            discriminator: this.config.discriminator
        };
    }

    getQrCodeData(): string {
        // Manual pairing code format for Matter
        // Format: XXX-YY-ZZZ where XXX-YY is discriminator parts and ZZZ is passcode part
        const discriminator = this.config.discriminator;
        const passcode = this.config.passcode;

        // Generate QR code payload (simplified)
        // Full Matter QR code format: MT:Y.K90KA0648G00
        return `MT:${this.config.vendorId}.${passcode}.${discriminator}`;
    }

    getManualPairingCode(): string {
        // 11-digit manual pairing code
        const passcode = this.config.passcode;
        const discriminator = this.config.discriminator;

        // Simplified format: XXXX-XXX-XXXX
        const codeStr = `${passcode}${discriminator}`.padStart(11, '0');
        return `${codeStr.slice(0, 4)}-${codeStr.slice(4, 7)}-${codeStr.slice(7)}`;
    }

    isRunning(): boolean {
        return this.isStarted;
    }
}
