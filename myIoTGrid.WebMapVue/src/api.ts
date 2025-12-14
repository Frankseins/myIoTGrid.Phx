export interface NodeDto {
  id: string
  name?: string
}

export interface Paged<T> {
  items: T[]
  totalRecords: number
  page: number
  size: number
  totalPages?: number
  hasNextPage?: boolean
}

export interface ReadingDto {
  id: number
  nodeId: string
  measurementType: string // e.g., "latitude" | "longitude" | ...
  value: number
  timestamp: string // ISO
}

const BASE = (import.meta as any).env?.VITE_API_BASE as string

export async function getNodes(): Promise<NodeDto[]> {
  const r = await fetch(`${BASE}/api/nodes`)
  if (!r.ok) throw new Error(`nodes: ${r.status}`)
  return r.json()
}

export async function getReadingsPaged(nodeId: string, page = 0, size = 100) {
  const params = new URLSearchParams({
    page: String(page),
    size: String(size),
    sort: 'Timestamp,desc',
    'filters[nodeId]': nodeId,
  })
  const r = await fetch(`${BASE}/api/readings/paged?${params.toString()}`)
  if (!r.ok) throw new Error(`readings: ${r.status}`)
  return r.json() as Promise<Paged<ReadingDto>>
}

// Fetch latest N readings for a specific measurement type for one node
export async function getReadingsByType(nodeId: string, measurementType: string, size = 600): Promise<ReadingDto[]> {
  const params = new URLSearchParams({
    page: '0',
    size: String(size),
    sort: 'Timestamp,desc',
    'filters[nodeId]': nodeId,
    'filters[measurementType]': measurementType,
  })
  const r = await fetch(`${BASE}/api/readings/paged?${params.toString()}`)
  if (!r.ok) throw new Error(`readingsByType: ${r.status}`)
  const data = (await r.json()) as Paged<ReadingDto>
  // Return ascending by time for charting convenience
  return data.items.slice().reverse()
}

// Paged loader: fetch multiple pages for a given type to reach desired count
export async function getReadingsByTypePagedAll(
  nodeId: string,
  measurementType: string,
  desiredCount = 1200,
  pageSize = 300,
  maxPages = 20,
): Promise<ReadingDto[]> {
  const all: ReadingDto[] = []
  let page = 0
  for (; page < maxPages; page++) {
    const params = new URLSearchParams({
      page: String(page),
      size: String(pageSize),
      sort: 'Timestamp,desc',
      'filters[nodeId]': nodeId,
      'filters[measurementType]': measurementType,
    })
    const r = await fetch(`${BASE}/api/readings/paged?${params.toString()}`)
    if (!r.ok) throw new Error(`readingsByType(page=${page}): ${r.status}`)
    const data = (await r.json()) as Paged<ReadingDto>
    all.push(...data.items)
    const noMore = data.items.length < pageSize || data.hasNextPage === false || (data.totalPages != null && page + 1 >= data.totalPages)
    if (all.length >= desiredCount || noMore) break
  }
  // Keep the newest first list but return ascending for charts
  const trimmed = all.slice(0, desiredCount)
  return trimmed.slice().reverse()
}

// -----------------------------
// Position building utilities
// -----------------------------
export interface PositionPoint {
  lat: number
  lon: number
  ts: string // ISO at second precision
  hdop?: number
  speed?: number // km/h
  temperature?: number
  humidity?: number
  altitude?: number // meters
  pressure?: number // hPa
  illuminance?: number // lux
  waterTemperature?: number // °C
  gpsSatellites?: number
  gpsFix?: number
}

function toSecondBucket(iso: string) {
  const d = new Date(iso)
  d.setMilliseconds(0)
  return d.toISOString()
}

export function buildPositionsFromReadings(items: ReadingDto[]): PositionPoint[] {
  // Group readings that belong to the same instant (second precision)
  const buckets = new Map<string, { lat?: number; lon?: number; hdop?: number; speed?: number; temperature?: number; humidity?: number; altitude?: number; pressure?: number; illuminance?: number; waterTemperature?: number; gpsSatellites?: number; gpsFix?: number }>()
  for (const r of items) {
    const key = toSecondBucket(r.timestamp)
    const b = buckets.get(key) ?? {}
    const mt = r.measurementType.toLowerCase()
    if (mt === 'latitude') b.lat = Number(r.value)
    if (mt === 'longitude') b.lon = Number(r.value)
    if (mt === 'gps_hdop') b.hdop = Number(r.value)
    if (mt === 'speed') b.speed = Number(r.value)
    if (mt === 'altitude') b.altitude = Number(r.value)
    if (mt === 'pressure') b.pressure = Number(r.value)
    if (mt === 'illuminance') b.illuminance = Number(r.value)
    if (mt === 'water_temperature') b.waterTemperature = Number(r.value)
    if (mt === 'gps_satellites') b.gpsSatellites = Number(r.value)
    if (mt === 'gps_fix') b.gpsFix = Number(r.value)
    // Common aliases for temperature/humidity
    if (mt === 'temperature' || mt === 'temp' || mt === 'temp_c' || mt === 'air_temperature') b.temperature = Number(r.value)
    if (mt === 'humidity' || mt === 'rel_humidity' || mt === 'relative_humidity' || mt === 'rh' || mt === 'hum') b.humidity = Number(r.value)
    buckets.set(key, b)
  }
  const pts: PositionPoint[] = []
  for (const [ts, b] of buckets) {
    if (b.lat != null && b.lon != null) pts.push({ lat: b.lat, lon: b.lon, ts,
      hdop: b.hdop,
      speed: b.speed,
      temperature: b.temperature,
      humidity: b.humidity,
      altitude: b.altitude,
      pressure: b.pressure,
      illuminance: b.illuminance,
      waterTemperature: b.waterTemperature,
      gpsSatellites: b.gpsSatellites,
      gpsFix: b.gpsFix,
    })
  }
  // Sort ascending by time
  pts.sort((a, b) => a.ts.localeCompare(b.ts))
  return pts
}

export async function getLatestPositions(nodeId: string, size = 300): Promise<PositionPoint[]> {
  const params = new URLSearchParams({
    page: '0',
    size: String(size),
    sort: 'Timestamp,desc',
    'filters[nodeId]': nodeId,
  })
  const r = await fetch(`${BASE}/api/readings/paged?${params.toString()}`)
  if (!r.ok) throw new Error(`readings: ${r.status}`)
  const data = (await r.json()) as Paged<ReadingDto>
  return buildPositionsFromReadings(data.items)
}

// Paged loader for latest positions: aggregates multiple pages and builds positions
export async function getLatestPositionsPaged(
  nodeId: string,
  desiredCount = 1800,
  pageSize = 300,
  maxPages = 30,
): Promise<PositionPoint[]> {
  const all: ReadingDto[] = []
  for (let page = 0; page < maxPages; page++) {
    const params = new URLSearchParams({
      page: String(page),
      size: String(pageSize),
      sort: 'Timestamp,desc',
      'filters[nodeId]': nodeId,
    })
    const r = await fetch(`${BASE}/api/readings/paged?${params.toString()}`)
    if (!r.ok) throw new Error(`readings(page=${page}): ${r.status}`)
    const data = (await r.json()) as Paged<ReadingDto>
    all.push(...data.items)
    const noMore = data.items.length < pageSize || data.hasNextPage === false || (data.totalPages != null && page + 1 >= data.totalPages)
    if (all.length >= desiredCount || noMore) break
  }
  const merged = buildPositionsFromReadings(all)
  // Build may yield fewer points than readings; return last N by time
  const limit = Math.min(desiredCount, merged.length)
  return merged.slice(-limit)
}

// -----------------------------
// Full history paging (ascending)
// -----------------------------
export async function getAllReadingsPagedAsc(
  nodeId: string,
  pageSize = 500,
  maxPages = 2000,
): Promise<ReadingDto[]> {
  const all: ReadingDto[] = []
  for (let page = 0; page < maxPages; page++) {
    const params = new URLSearchParams({
      page: String(page),
      size: String(pageSize),
      sort: 'Timestamp,asc',
      'filters[nodeId]': nodeId,
    })
    const r = await fetch(`${BASE}/api/readings/paged?${params.toString()}`)
    if (!r.ok) throw new Error(`readings(page=${page}): ${r.status}`)
    const data = (await r.json()) as Paged<ReadingDto>
    all.push(...data.items)
    const noMore = data.items.length < pageSize || data.hasNextPage === false || (data.totalPages != null && page + 1 >= data.totalPages)
    if (noMore) break
  }
  return all
}

export async function getAllPositionsPagedAsc(
  nodeId: string,
  pageSize = 500,
  maxPages = 2000,
): Promise<PositionPoint[]> {
  const readings = await getAllReadingsPagedAsc(nodeId, pageSize, maxPages)
  return buildPositionsFromReadings(readings)
}

// -----------------------------
// Aggregated "current per sensor" endpoint
// -----------------------------
export interface LatestMeasurementDto {
  readingId: number
  measurementType: string
  displayName: string
  rawValue: number
  value: number
  unit: string
  timestamp: string
}

export interface SensorLatestReadingDto {
  assignmentId: string
  sensorId: string
  displayName: string
  fullName: string
  alias?: string
  sensorCode: string
  sensorModel: string
  endpointId: number
  icon?: string
  color?: string
  isActive: boolean
  measurements: LatestMeasurementDto[]
}

export interface NodeSensorsLatestDto {
  nodeId: string
  nodeName?: string
  sensors: SensorLatestReadingDto[]
}

export async function getSensorsLatest(nodeId: string): Promise<NodeSensorsLatestDto> {
  const r = await fetch(`${BASE}/api/nodes/${nodeId}/sensors/latest`)
  if (!r.ok) throw new Error(`sensors/latest: ${r.status}`)
  return r.json()
}

// Utility to extract a single PositionPoint from NodeSensorsLatestDto
export function latestToPosition(latest: NodeSensorsLatestDto): PositionPoint | null {
  // Find latitude/longitude values across all sensors
  const find = (type: string) => {
    type = type.toLowerCase()
    for (const s of latest.sensors) {
      const m = s.measurements.find(x => x.measurementType.toLowerCase() === type)
      if (m) return m
    }
    return undefined
  }
  const latM = find('latitude')
  const lonM = find('longitude')
  if (!latM || !lonM) return null

  // Optional extras
  const hdopM = find('gps_hdop')
  const speedM = find('speed')
  const tempM = find('temperature')
  const humM = find('humidity')
  const altM = find('altitude')
  const pressM = find('pressure')
  const illumM = find('illuminance')
  const waterTempM = find('water_temperature')
  const satsM = find('gps_satellites')
  const fixM = find('gps_fix')

  // Use the most recent timestamp between lat/lon as point time
  const ts = new Date(Math.max(new Date(latM.timestamp).getTime(), new Date(lonM.timestamp).getTime())).toISOString()
  const point: PositionPoint = {
    lat: Number(latM.value),
    lon: Number(lonM.value),
    ts,
    hdop: hdopM ? Number(hdopM.value) : undefined,
    speed: speedM ? Number(speedM.value) : undefined,
    temperature: tempM ? Number(tempM.value) : undefined,
    humidity: humM ? Number(humM.value) : undefined,
    altitude: altM ? Number(altM.value) : undefined,
    pressure: pressM ? Number(pressM.value) : undefined,
    illuminance: illumM ? Number(illumM.value) : undefined,
    waterTemperature: waterTempM ? Number(waterTempM.value) : undefined,
    gpsSatellites: satsM ? Number(satsM.value) : undefined,
    gpsFix: fixM ? Number(fixM.value) : undefined,
  }
  return point
}
