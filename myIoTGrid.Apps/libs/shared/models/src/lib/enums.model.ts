/**
 * Enums - corresponds to Backend Enums in myIoTGrid.Shared.Common.Enums
 * IMPORTANT: These must use numeric values to match the backend C# enums!
 */

export enum Protocol {
  Unknown = 0,
  WLAN = 1,
  LoRaWAN = 2
}

export enum AlertLevel {
  Ok = 0,
  Info = 1,
  Warning = 2,
  Critical = 3
}

export enum AlertSource {
  Local = 0,
  Cloud = 1
}

export enum NodeTransport {
  Unknown = 0,
  WLAN = 1,
  LoRaWAN = 2,
  Matter = 3
}
