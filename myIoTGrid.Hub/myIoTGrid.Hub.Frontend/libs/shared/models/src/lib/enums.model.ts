/**
 * Enums - corresponds to Backend Enums in myIoTGrid.Hub.Domain
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
