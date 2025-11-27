import {
    Endpoint,
    EndpointServer
} from "@matter/main";
import {
    BridgedNodeEndpoint,
    TemperatureSensorDevice,
    HumiditySensorDevice,
    ContactSensorDevice
} from "@matter/main/devices";
import {
    TemperatureMeasurementServer,
    RelativeHumidityMeasurementServer,
    BooleanStateServer,
    PressureMeasurementServer,
    BridgedDeviceBasicInformationServer
} from "@matter/main/behaviors";
import { Logger } from "winston";

export enum DeviceType {
    TemperatureSensor = "temperature",
    HumiditySensor = "humidity",
    PressureSensor = "pressure",
    ContactSensor = "contact"
}

export interface MatterDevice {
    endpoint: Endpoint;
    type: DeviceType;
}

export class DeviceFactory {
    private logger: Logger;

    constructor(logger: Logger) {
        this.logger = logger;
    }

    async createDevice(
        sensorId: string,
        name: string,
        type: DeviceType
    ): Promise<Endpoint> {
        switch (type) {
            case DeviceType.TemperatureSensor:
                return this.createTemperatureSensor(sensorId, name);
            case DeviceType.HumiditySensor:
                return this.createHumiditySensor(sensorId, name);
            case DeviceType.PressureSensor:
                return this.createPressureSensor(sensorId, name);
            case DeviceType.ContactSensor:
                return this.createContactSensor(sensorId, name);
            default:
                throw new Error(`Unknown device type: ${type}`);
        }
    }

    private async createTemperatureSensor(
        sensorId: string,
        name: string
    ): Promise<Endpoint> {
        const endpoint = new Endpoint(
            TemperatureSensorDevice.with(BridgedDeviceBasicInformationServer),
            {
                id: `temp-${sensorId}`,
                bridgedDeviceBasicInformation: {
                    nodeLabel: name,
                    productName: "myIoTGrid Temperature Sensor",
                    productLabel: name,
                    serialNumber: sensorId,
                    uniqueId: `temp-${sensorId}`,
                    reachable: true,
                },
                temperatureMeasurement: {
                    // Temperature in 0.01°C units (Matter standard)
                    // Initial value: 20°C = 2000
                    measuredValue: 2000,
                    minMeasuredValue: -4000, // -40°C
                    maxMeasuredValue: 12500, // 125°C
                },
            }
        );

        return endpoint;
    }

    private async createHumiditySensor(
        sensorId: string,
        name: string
    ): Promise<Endpoint> {
        const endpoint = new Endpoint(
            HumiditySensorDevice.with(BridgedDeviceBasicInformationServer),
            {
                id: `hum-${sensorId}`,
                bridgedDeviceBasicInformation: {
                    nodeLabel: name,
                    productName: "myIoTGrid Humidity Sensor",
                    productLabel: name,
                    serialNumber: sensorId,
                    uniqueId: `hum-${sensorId}`,
                    reachable: true,
                },
                relativeHumidityMeasurement: {
                    // Humidity in 0.01% units (Matter standard)
                    // Initial value: 50% = 5000
                    measuredValue: 5000,
                    minMeasuredValue: 0,
                    maxMeasuredValue: 10000, // 100%
                },
            }
        );

        return endpoint;
    }

    private async createPressureSensor(
        sensorId: string,
        name: string
    ): Promise<Endpoint> {
        // Pressure sensor uses a custom endpoint with BridgedNodeEndpoint
        const endpoint = new Endpoint(
            BridgedNodeEndpoint.with(
                BridgedDeviceBasicInformationServer,
                PressureMeasurementServer
            ),
            {
                id: `pres-${sensorId}`,
                bridgedDeviceBasicInformation: {
                    nodeLabel: name,
                    productName: "myIoTGrid Pressure Sensor",
                    productLabel: name,
                    serialNumber: sensorId,
                    uniqueId: `pres-${sensorId}`,
                    reachable: true,
                },
                pressureMeasurement: {
                    // Pressure in kPa (Matter standard)
                    // Initial value: 1013.25 hPa = 101.325 kPa = 101325 in 0.1 kPa
                    measuredValue: 10132,
                    minMeasuredValue: 8000,  // 800 hPa
                    maxMeasuredValue: 12000, // 1200 hPa
                },
            }
        );

        return endpoint;
    }

    private async createContactSensor(
        sensorId: string,
        name: string
    ): Promise<Endpoint> {
        const endpoint = new Endpoint(
            ContactSensorDevice.with(BridgedDeviceBasicInformationServer),
            {
                id: `contact-${sensorId}`,
                bridgedDeviceBasicInformation: {
                    nodeLabel: name,
                    productName: "myIoTGrid Alert Sensor",
                    productLabel: name,
                    serialNumber: sensorId,
                    uniqueId: `contact-${sensorId}`,
                    reachable: true,
                },
                booleanState: {
                    // false = closed (no alert), true = open (alert active)
                    stateValue: false,
                },
            }
        );

        return endpoint;
    }

    async updateDeviceValue(
        endpoint: Endpoint,
        deviceType: DeviceType,
        sensorType: string,
        value: number
    ): Promise<void> {
        try {
            switch (deviceType) {
                case DeviceType.TemperatureSensor:
                    if (sensorType === "temperature") {
                        // Convert °C to Matter format (0.01°C units)
                        const matterTemp = Math.round(value * 100);
                        await endpoint.set({
                            temperatureMeasurement: {
                                measuredValue: matterTemp
                            }
                        });
                    }
                    break;

                case DeviceType.HumiditySensor:
                    if (sensorType === "humidity") {
                        // Convert % to Matter format (0.01% units)
                        const matterHum = Math.round(value * 100);
                        await endpoint.set({
                            relativeHumidityMeasurement: {
                                measuredValue: matterHum
                            }
                        });
                    }
                    break;

                case DeviceType.PressureSensor:
                    if (sensorType === "pressure") {
                        // Convert hPa to Matter format (0.1 kPa units)
                        // 1 hPa = 0.1 kPa, so hPa value = kPa * 10
                        const matterPres = Math.round(value * 10);
                        await endpoint.set({
                            pressureMeasurement: {
                                measuredValue: matterPres
                            }
                        });
                    }
                    break;

                default:
                    this.logger.warn(`Unknown device type for update: ${deviceType}`);
            }
        } catch (error) {
            this.logger.error(`Error updating device value`, { error, deviceType, sensorType, value });
            throw error;
        }
    }

    async setContactState(endpoint: Endpoint, isOpen: boolean): Promise<void> {
        try {
            await endpoint.set({
                booleanState: {
                    stateValue: isOpen
                }
            });
        } catch (error) {
            this.logger.error(`Error setting contact state`, { error, isOpen });
            throw error;
        }
    }

    static mapSensorTypeToDeviceType(sensorType: string): DeviceType | null {
        const mapping: Record<string, DeviceType> = {
            "temperature": DeviceType.TemperatureSensor,
            "humidity": DeviceType.HumiditySensor,
            "pressure": DeviceType.PressureSensor,
            "contact": DeviceType.ContactSensor,
        };

        return mapping[sensorType.toLowerCase()] || null;
    }

    static getSupportedSensorTypes(): string[] {
        return ["temperature", "humidity", "pressure"];
    }
}
