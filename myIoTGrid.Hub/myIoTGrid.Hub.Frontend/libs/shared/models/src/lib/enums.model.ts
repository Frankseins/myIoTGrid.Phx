/**
 * Enums - corresponds to Backend Enums in myIoTGrid.Hub.Shared
 */

export enum Protocol {
  Unknown = 'Unknown',
  WLAN = 'WLAN',
  LoRaWAN = 'LoRaWAN'
}

export enum AlertLevel {
  Ok = 'Ok',
  Info = 'Info',
  Warning = 'Warning',
  Critical = 'Critical'
}

export enum AlertSource {
  Local = 'Local',
  Cloud = 'Cloud'
}

export enum NodeTransport {
  Unknown = 'Unknown',
  WLAN = 'WLAN',
  LoRaWAN = 'LoRaWAN',
  Matter = 'Matter'
}
