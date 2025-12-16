<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import * as L from 'leaflet'

const props = defineProps<{ lat: number | null; lon: number | null; trail?: [number, number][], points?: { lat: number, lon: number, ts: string, temperature?: number, humidity?: number, altitude?: number, pressure?: number, illuminance?: number, waterTemperature?: number, hdop?: number, speed?: number, gpsSatellites?: number, gpsFix?: number }[] }>()
// Using local shim for leaflet types in build container; keep refs as any to avoid TS type dependency
const map = ref<any>(null)
const marker = ref<any>(null)
const poly = ref<any>(null)
const markersLayer = ref<any>(null)
const markerList = ref<any[]>([])
const prevPointsLen = ref<number>(0)
const prevFirstTs = ref<string | null>(null)
const prevLastTs = ref<string | null>(null)
let didFit = false

// Build popup HTML dynamically from provided measurements
function buildPopupHtml(p: any): string {
  const base: string[] = [
    `Lat: ${p.lat?.toFixed ? p.lat.toFixed(6) : p.lat}`,
    `Lon: ${p.lon?.toFixed ? p.lon.toFixed(6) : p.lon}`,
    `Time: ${new Date(p.ts).toLocaleString()}`,
  ]
  const LABELS: Record<string, string> = {
    temperature: 'Temperatur',
    humidity: 'Luftfeuchtigkeit',
    waterTemperature: 'Wassertemperatur',
    pressure: 'Luftdruck',
    illuminance: 'Helligkeit',
    altitude: 'Höhe',
    speed: 'Geschwindigkeit',
    hdop: 'HDOP',
    gpsSatellites: 'Satelliten',
    gpsFix: 'Fix',
  }
  const UNITS: Record<string, string> = {
    temperature: '°C',
    humidity: '%',
    waterTemperature: '°C',
    pressure: ' hPa',
    illuminance: ' lux',
    altitude: ' m',
    speed: ' km/h',
    hdop: '',
    gpsSatellites: '',
    gpsFix: '',
  }
  const DECIMALS: Record<string, number> = {
    temperature: 1,
    humidity: 1,
    waterTemperature: 1,
    pressure: 1,
    illuminance: 0,
    altitude: 1,
    speed: 2,
    hdop: 2,
    gpsSatellites: 0,
    gpsFix: 0,
  }
  const skip = new Set(['lat', 'lon', 'ts'])
  for (const [key, val] of Object.entries(p)) {
    if (skip.has(key)) continue
    if (val == null) continue
    if (typeof val !== 'number') continue
    const label = LABELS[key] ?? key
    const unit = UNITS[key] ?? ''
    const decimals = DECIMALS[key]
    const valueStr = Number.isFinite(val)
      ? (decimals != null ? val.toFixed(decimals) : String(val))
      : String(val)
    base.push(`${label}: ${valueStr}${unit}`)
  }
  return base.join('<br>')
}

onMounted(() => {
  map.value = L.map('map', { center: [52.52, 13.405], zoom: 12 })
  L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    attribution: '&copy; OpenStreetMap contributors'
  }).addTo(map.value!)
  markersLayer.value = L.layerGroup().addTo(map.value!)
})

watch(() => [props.lat, props.lon] as const, ([lat, lon]) => {
  if (!map.value || lat == null || lon == null) return
  const pos: [number, number] = [lat, lon]
  if (!marker.value) {
    // Smaller current position marker
    marker.value = L.circleMarker(pos, { radius: 6, color: '#1565C0', weight: 2, fillColor: '#42A5F5', fillOpacity: 0.8 })
      .addTo(map.value)
  } else {
    marker.value.setLatLng(pos)
  }
  if (!didFit) map.value.setView(pos, 14)
})

watch(() => props.trail, (tr) => {
  if (!map.value || !tr || tr.length === 0) return
  if (!poly.value) {
    poly.value = L.polyline(tr, { color: '#1976D2', weight: 3 }).addTo(map.value)
  } else {
    poly.value.setLatLngs(tr)
  }
  if (!didFit) {
    map.value.fitBounds(poly.value.getBounds(), { padding: [20, 20] })
    didFit = true
  }
}, { deep: true })

// Render clickable markers for each polled point
watch(() => props.points, (pts) => {
  if (!markersLayer.value) return
  // Handle reset/empty
  if (!pts || pts.length === 0) {
    markersLayer.value.clearLayers()
    markerList.value = []
    prevPointsLen.value = 0
    return
  }

  // Initial render: add all
  if (prevPointsLen.value === 0) {
    markerList.value = []
    for (const p of pts) {
      const m = L.circleMarker([p.lat, p.lon] as any, { radius: 4, color: '#0D47A1', weight: 1.5, fillColor: '#64B5F6', fillOpacity: 0.9 })
      const html = buildPopupHtml(p as any)
      m.bindPopup(html)
      m.on('mouseover', () => { m.openPopup() })
      m.on('mouseout', () => { m.closePopup() })
      m.addTo(markersLayer.value)
      markerList.value.push(m)
    }
    prevPointsLen.value = pts.length
    prevFirstTs.value = pts[0]?.ts ?? null
    prevLastTs.value = pts[pts.length - 1]?.ts ?? null
    return
  }

  // Incremental update: only add new points if it's a pure append at the tail; otherwise rebuild
  if (pts.length > prevPointsLen.value) {
    // Detect pure append: previous last should be at index prevPointsLen-1
    const isPureAppend = prevLastTs.value != null && pts[prevPointsLen.value - 1]?.ts === prevLastTs.value && pts[0]?.ts === prevFirstTs.value
    if (isPureAppend) {
      const newPts = pts.slice(prevPointsLen.value)
      for (const p of newPts) {
        const m = L.circleMarker([p.lat, p.lon] as any, { radius: 4, color: '#0D47A1', weight: 1.5, fillColor: '#64B5F6', fillOpacity: 0.9 })
        const html = buildPopupHtml(p as any)
        m.bindPopup(html)
        m.on('mouseover', () => { m.openPopup() })
        m.on('mouseout', () => { m.closePopup() })
        m.addTo(markersLayer.value)
        markerList.value.push(m)
      }
      prevPointsLen.value = pts.length
      prevFirstTs.value = pts[0]?.ts ?? null
      prevLastTs.value = pts[pts.length - 1]?.ts ?? null
    } else {
      // Not a pure append (e.g., timeframe expanded to earlier) — rebuild fully
      markersLayer.value.clearLayers()
      markerList.value = []
      for (const p of pts) {
        const m = L.circleMarker([p.lat, p.lon] as any, { radius: 4, color: '#0D47A1', weight: 1.5, fillColor: '#64B5F6', fillOpacity: 0.9 })
        const html = buildPopupHtml(p as any)
        m.bindPopup(html)
        m.on('mouseover', () => { m.openPopup() })
        m.on('mouseout', () => { m.closePopup() })
        m.addTo(markersLayer.value)
        markerList.value.push(m)
      }
      prevPointsLen.value = pts.length
      prevFirstTs.value = pts[0]?.ts ?? null
      prevLastTs.value = pts[pts.length - 1]?.ts ?? null
    }
  } else if (pts.length < prevPointsLen.value) {
    // Trail shrunk (node switched). Rebuild to keep in sync
    markersLayer.value.clearLayers()
    markerList.value = []
    for (const p of pts) {
      const m = L.circleMarker([p.lat, p.lon] as any, { radius: 4, color: '#0D47A1', weight: 1.5, fillColor: '#64B5F6', fillOpacity: 0.9 })
      const html = buildPopupHtml(p as any)
      m.bindPopup(html)
      m.on('mouseover', () => { m.openPopup() })
      m.on('mouseout', () => { m.closePopup() })
      m.addTo(markersLayer.value)
      markerList.value.push(m)
    }
    prevPointsLen.value = pts.length
    prevFirstTs.value = pts[0]?.ts ?? null
    prevLastTs.value = pts[pts.length - 1]?.ts ?? null
  }

  // Optional cap to avoid too many markers (keep last 500)
  const MAX = 500
  while (markerList.value.length > MAX) {
    const m = markerList.value.shift()
    if (m) markersLayer.value.removeLayer(m)
  }
})
</script>

<template>
  <div id="map" style="height:100%;width:100%" />
</template>
