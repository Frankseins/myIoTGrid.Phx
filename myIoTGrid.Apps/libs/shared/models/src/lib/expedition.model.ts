/**
 * Expedition status enum
 */
export enum ExpeditionStatus {
  /** Expedition not yet started */
  Planned = 'Planned',
  /** Expedition currently active */
  Active = 'Active',
  /** Expedition completed */
  Completed = 'Completed',
  /** Expedition archived */
  Archived = 'Archived'
}

/**
 * Expedition model - corresponds to Backend ExpeditionDto
 * Represents a GPS tracking session
 */
export interface Expedition {
  id: string;
  name: string;
  description?: string;
  nodeId: string;
  nodeName: string;
  startTime: string;
  endTime: string;
  status: ExpeditionStatus;
  totalDistanceKm?: number;
  totalReadings?: number;
  averageSpeedKmh?: number;
  maxSpeedKmh?: number;
  /** Duration as ISO 8601 duration string (e.g., "08:23:00") */
  duration: string;
  createdAt: string;
  updatedAt?: string;
  createdBy?: string;
  tags: string[];
  coverImageUrl?: string;
}

/**
 * DTO for creating a new Expedition
 */
export interface CreateExpeditionDto {
  name: string;
  description?: string;
  nodeId: string;
  startTime: string;
  endTime: string;
  tags?: string[];
}

/**
 * DTO for updating an Expedition
 */
export interface UpdateExpeditionDto {
  name?: string;
  description?: string;
  startTime?: string;
  endTime?: string;
  status?: ExpeditionStatus;
  tags?: string[];
  coverImageUrl?: string;
}

/**
 * Expedition statistics
 */
export interface ExpeditionStats {
  expeditionId: string;
  expeditionName: string;
  totalDistanceKm: number;
  totalReadings: number;
  duration: string;
  averageSpeedKmh: number;
  maxSpeedKmh: number;
  startLatitude?: number;
  startLongitude?: number;
  endLatitude?: number;
  endLongitude?: number;
  firstReadingTime?: string;
  lastReadingTime?: string;
}

/**
 * Filter parameters for expedition list queries
 */
export interface ExpeditionFilter {
  status?: ExpeditionStatus;
  nodeId?: string;
  tags?: string;
  fromDate?: string;
  toDate?: string;
}

/**
 * GPS point with measurement data
 */
export interface ExpeditionGpsPoint {
  latitude: number;
  longitude: number;
  timestamp: string;
  speed?: number;
  altitude?: number;
  temperature?: number;
  humidity?: number;
  pressure?: number;
  waterTemperature?: number;
  illuminance?: number;
  gpsSatellites?: number;
  gpsFix?: number;
  hdop?: number;
}

/**
 * GPS data for an expedition (route and measurements)
 */
export interface ExpeditionGpsData {
  expeditionId: string;
  expeditionName: string;
  startTime: string;
  endTime: string;
  points: ExpeditionGpsPoint[];
  trail: number[][];
}
