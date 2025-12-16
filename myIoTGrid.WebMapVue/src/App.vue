
<script setup lang="ts">
import { onMounted, onBeforeUnmount, ref, watch, computed } from 'vue'
import { getNodes, getAllPositionsPagedAsc, type NodeDto, type PositionPoint } from './api'
import MapView from './components/MapView.vue'
import TimeSeriesChart from './components/TimeSeriesChart.vue'

// Data
const nodes = ref<NodeDto[]>([])
const selectedNodeId = ref('88d655ed-1578-4b0b-b7f4-724ed27016e7')
const lat = ref<number | null>(null)
const lon = ref<number | null>(null)

// Full trail (entire history for node)
const fullTrail = ref<PositionPoint[]>([])

// Time bounds and selection (use local time for inputs)
const firstEventISO = ref<string | null>(null)
const nowISO = ref<string>(new Date().toISOString())
const selectedStartLocal = ref<string>('') // YYYY-MM-DDTHH:mm:ss
const selectedEndLocal = ref<string>('')

// Interval (seconds)
const intervalSec = ref<number>(30)

let timer: number | undefined

function stopPolling() {
  if (timer) { clearInterval(timer); timer = undefined }
}

function toLocalInputValue(iso: string): string {
  // Convert ISO (UTC) to local datetime-local string with seconds
  const d = new Date(iso)
  const pad = (n: number) => String(n).padStart(2, '0')
  const yyyy = d.getFullYear()
  const mm = pad(d.getMonth() + 1)
  const dd = pad(d.getDate())
  const hh = pad(d.getHours())
  const min = pad(d.getMinutes())
  const ss = pad(d.getSeconds())
  return `${yyyy}-${mm}-${dd}T${hh}:${min}:${ss}`
}

function fromLocalInputValue(val: string): number | null {
  // val like 'YYYY-MM-DDTHH:mm[:ss]'
  if (!val) return null
  const d = new Date(val)
  const t = d.getTime()
  return isNaN(t) ? null : t
}

function clampSelectionToBounds() {
  if (!firstEventISO.value) return
  const minMs = new Date(firstEventISO.value).getTime()
  const maxMs = new Date(nowISO.value).getTime()
  const s = fromLocalInputValue(selectedStartLocal.value)
  const e = fromLocalInputValue(selectedEndLocal.value)
  if (s != null) {
    const cs = Math.max(minMs, Math.min(s, maxMs))
    selectedStartLocal.value = toLocalInputValue(new Date(cs).toISOString())
  }
  if (e != null) {
    const ce = Math.max(minMs, Math.min(e, maxMs))
    selectedEndLocal.value = toLocalInputValue(new Date(ce).toISOString())
  }
  // Ensure start <= end
  const s2 = fromLocalInputValue(selectedStartLocal.value)
  const e2 = fromLocalInputValue(selectedEndLocal.value)
  if (s2 != null && e2 != null && s2 > e2) {
    // Swap
    const tmp = selectedStartLocal.value
    selectedStartLocal.value = selectedEndLocal.value
    selectedEndLocal.value = tmp
  }
}

async function loadAllHistory() {
  if (!selectedNodeId.value) return
  try {
    nowISO.value = new Date().toISOString()
    const positions = await getAllPositionsPagedAsc(selectedNodeId.value, 500, 2000)
    fullTrail.value = positions
    const first = positions.length > 0 ? positions[0] : undefined
    const last = positions.length > 0 ? positions[positions.length - 1] : undefined
    firstEventISO.value = first?.ts ?? null
    lat.value = last?.lat ?? null
    lon.value = last?.lon ?? null
    // Initialize selection to full range
    if (firstEventISO.value) {
      selectedStartLocal.value = toLocalInputValue(firstEventISO.value)
      selectedEndLocal.value = toLocalInputValue(nowISO.value)
    }
  } catch (e) {
    console.error('[WebMap] loadAllHistory failed', e)
  }
}

onMounted(async () => {
  try {
    const list = await getNodes()
    nodes.value = list
    if (!selectedNodeId.value && list.length > 0) {
      const first = list.find(() => true)
      if (first && first.id) selectedNodeId.value = first.id
    }
  } catch (e) { console.error(e) }
  await loadAllHistory()
})

onBeforeUnmount(() => { stopPolling() })

watch(selectedNodeId, async () => {
  fullTrail.value = []
  await loadAllHistory()
})

watch([selectedStartLocal, selectedEndLocal], () => {
  clampSelectionToBounds()
})

// Filter trail by selected time range
const filteredTrail = computed<PositionPoint[]>(() => {
  const sMs = fromLocalInputValue(selectedStartLocal.value)
  const eMs = fromLocalInputValue(selectedEndLocal.value)
  if (sMs == null || eMs == null) return fullTrail.value
  return fullTrail.value.filter(p => {
    const t = new Date(p.ts).getTime()
    return t >= sMs && t <= eMs
  })
})

// Downsample by interval
const intervalTrail = computed<PositionPoint[]>(() => {
  const sec = Math.max(1, intervalSec.value | 0)
  if (sec <= 1) return filteredTrail.value
  const intervalMs = sec * 1000
  const out: PositionPoint[] = []
  let lastBucket: number | null = null
  for (const p of filteredTrail.value) {
    const t = new Date(p.ts).getTime()
    const bucket = Math.floor(t / intervalMs)
    if (lastBucket === null || bucket !== lastBucket) {
      out.push(p)
      lastBucket = bucket
    }
  }
  return out
})

// For MapView
const polylineLatLngs = computed<[number, number][]>(() => intervalTrail.value.map(p => [p.lat, p.lon]))
const markerPoints = computed<PositionPoint[]>(() => intervalTrail.value)

const dateMinAttr = computed(() => firstEventISO.value ? toLocalInputValue(firstEventISO.value).slice(0, 19) : '')
const dateMaxAttr = computed(() => toLocalInputValue(nowISO.value).slice(0, 19))

// Slider representation (epoch seconds)
const sliderMinSec = computed(() => firstEventISO.value ? Math.floor(new Date(firstEventISO.value).getTime() / 1000) : 0)
const sliderMaxSec = computed(() => Math.floor(new Date(nowISO.value).getTime() / 1000))

// Two-way computed bindings for double slider thumbs
const startSec = computed<number>({
  get() {
    const s = fromLocalInputValue(selectedStartLocal.value)
    return s != null ? Math.floor(s / 1000) : sliderMinSec.value
  },
  set(v: number) {
    const min = sliderMinSec.value
    const max = sliderMaxSec.value
    let clamped = Math.max(min, Math.min(v, max))
    // Ensure start <= end
    clamped = Math.min(clamped, endSec.value)
    selectedStartLocal.value = toLocalInputValue(new Date(clamped * 1000).toISOString())
  }
})

const endSec = computed<number>({
  get() {
    const e = fromLocalInputValue(selectedEndLocal.value)
    return e != null ? Math.floor(e / 1000) : sliderMaxSec.value
  },
  set(v: number) {
    const min = sliderMinSec.value
    const max = sliderMaxSec.value
    let clamped = Math.max(min, Math.min(v, max))
    // Ensure start <= end
    clamped = Math.max(clamped, startSec.value)
    selectedEndLocal.value = toLocalInputValue(new Date(clamped * 1000).toISOString())
  }
})

// Current speed from the last point in the filtered view
const currentSpeed = computed(() => {
  const last = intervalTrail.value.length > 0 ? intervalTrail.value[intervalTrail.value.length - 1] : undefined
  return last?.speed ?? null
})

// Default mode: full history via /api/readings/paged (ascending), markers per second

// -----------------------------
// Right-side charts (time series)
// -----------------------------
const chartLabels = computed<string[]>(() => filteredTrail.value.map(p => new Date(p.ts).toLocaleTimeString()))

type SeriesDef = {
  key: keyof PositionPoint
  title: string
  unit?: string
  color?: string
}

// Define known measurement series in desired order; dynamic filter will hide empty ones
const seriesDefs: SeriesDef[] = [
  { key: 'speed', title: 'Geschwindigkeit', unit: 'km/h', color: '#4CAF50' },
  { key: 'altitude', title: 'Höhe', unit: 'm', color: '#8E24AA' },
  { key: 'temperature', title: 'Temperatur', unit: '°C', color: '#E53935' },
  { key: 'waterTemperature', title: 'Wassertemperatur', unit: '°C', color: '#1E88E5' },
  { key: 'humidity', title: 'Luftfeuchtigkeit', unit: '%', color: '#00ACC1' },
  { key: 'pressure', title: 'Luftdruck', unit: 'hPa', color: '#6D4C41' },
  { key: 'illuminance', title: 'Helligkeit', unit: 'lux', color: '#FDD835' },
  { key: 'hdop', title: 'HDOP', unit: '', color: '#3949AB' },
  { key: 'gpsSatellites', title: 'Satelliten', unit: '', color: '#00796B' },
  { key: 'gpsFix', title: 'Fix-Status', unit: '', color: '#546E7A' },
]

const measurementSeries = computed(() => {
  const hasValues = (key: keyof PositionPoint) => filteredTrail.value.some(p => typeof (p as any)[key] === 'number')
  return seriesDefs
    .filter(def => hasValues(def.key))
    .map(def => ({
      ...def,
      values: filteredTrail.value.map(p => {
        const v = (p as any)[def.key]
        return typeof v === 'number' ? v : null
      }) as (number | null)[],
    }))
})
</script>

<template>
  <div class="app">
    <header class="topbar">
      <div class="brand">myIoTGrid • Web Map</div>
      <div class="controls">
        <div class="control timeframe">
          <label>Timeframe</label>
          <div class="range-wrap">
            <input type="range" :min="sliderMinSec" :max="sliderMaxSec" step="1" v-model.number="startSec" />
            <input type="range" :min="sliderMinSec" :max="sliderMaxSec" step="1" v-model.number="endSec" />
            <div class="track">
              <div class="range" :style="{ left: ((startSec - sliderMinSec) / (sliderMaxSec - sliderMinSec) * 100) + '%', width: ((endSec - startSec) / (sliderMaxSec - sliderMinSec) * 100) + '%' }" />
            </div>
          </div>
          <div class="time-labels">
            <span>{{ toLocalInputValue(new Date(startSec * 1000).toISOString()).replace('T',' ') }}</span>
            <span>{{ toLocalInputValue(new Date(endSec * 1000).toISOString()).replace('T',' ') }}</span>
          </div>
        </div>
        <div class="control">
          <label>Interval</label>
          <select v-model.number="intervalSec">
            <option :value="1">1s</option>
            <option :value="5">5s</option>
            <option :value="10">10s</option>
            <option :value="30">30s</option>
            <option :value="60">1 min</option>
            <option :value="300">5 min</option>
          </select>
        </div>
        <div class="spacer" />
        <div class="meta" v-if="currentSpeed != null">Speed: {{ currentSpeed.toFixed(1) }} km/h</div>
      </div>
    </header>
    <main class="content two-col">
      <div class="map-wrap">
        <MapView :lat="lat" :lon="lon" :trail="polylineLatLngs" :points="markerPoints" />
      </div>
      <aside class="charts-panel">
        <div class="charts-inner">
          <template v-if="measurementSeries.length > 0">
            <div v-for="s in measurementSeries" :key="s.key as string" class="chart-item">
              <TimeSeriesChart :title="s.title" :unit="s.unit" :labels="chartLabels" :values="s.values" :color="s.color" />
            </div>
          </template>
          <div v-else class="no-data">No measurements in selected timeframe.</div>
        </div>
      </aside>
    </main>
  </div>
  
</template>

<style scoped>
.app { display: flex; flex-direction: column; height: 100vh; width: 100vw; }
.topbar {
  position: sticky; top: 0; z-index: 10;
  display: flex; align-items: center; gap: 16px;
  padding: 10px 16px;
  background: linear-gradient(90deg, #0D47A1 0%, #1976D2 100%);
  color: #fff;
  box-shadow: 0 2px 8px rgba(0,0,0,0.25);
}
.brand { font-weight: 600; letter-spacing: 0.3px; }
.controls { display: flex; align-items: center; gap: 12px; flex-wrap: wrap; width: 100%; }
.control { display: flex; align-items: center; gap: 6px; color: #E3F2FD; white-space: nowrap; }
.control label { font-size: 12px; opacity: 0.9; }
.control input, .control select {
  border-radius: 6px; border: 1px solid rgba(255,255,255,0.25);
  background: rgba(255,255,255,0.12); color: #fff; padding: 6px 8px;
}
.control input::-webkit-calendar-picker-indicator { filter: invert(1); }
.control.timeframe { flex-direction: column; align-items: flex-start; gap: 6px; flex: 1 1 100%; }
.range-wrap { position: relative; height: 36px; width: 100%; max-width: 720px; }
.range-wrap input[type="range"] { position: absolute; left: 0; right: 0; top: 0; width: 100%; height: 36px; -webkit-appearance: none; background: none; pointer-events: none; }
.range-wrap input[type="range"]::-webkit-slider-thumb { -webkit-appearance: none; appearance: none; height: 16px; width: 16px; border-radius: 50%; background: #fff; border: 2px solid #64B5F6; pointer-events: auto; }
.range-wrap input[type="range"]::-moz-range-thumb { height: 16px; width: 16px; border-radius: 50%; background: #fff; border: 2px solid #64B5F6; pointer-events: auto; }
.range-wrap input[type="range"]::-webkit-slider-runnable-track { height: 4px; background: transparent; }
.track { position: absolute; left: 0; right: 0; top: 50%; transform: translateY(-50%); height: 4px; background: rgba(255,255,255,0.25); border-radius: 2px; }
.track .range { position: absolute; height: 100%; background: #64B5F6; border-radius: 2px; }
.time-labels { display: flex; justify-content: space-between; width: 100%; font-size: 11px; opacity: 0.95; }
.spacer { flex: 1; }
.meta { font-size: 12px; opacity: 0.9; }
.content { flex: 1; min-height: 0; }
.two-col { display: flex; gap: 0; height: 100%; }
.map-wrap { flex: 1 1 auto; min-width: 0; position: relative; }
.map-wrap :deep(#map) { height: 100%; width: 100%; }
.charts-panel { width: 380px; max-width: 40vw; border-left: 1px solid #e5e7eb; background: #fafafa; color: #111; overflow-y: auto; }
.charts-inner { padding: 10px 12px; display: flex; flex-direction: column; gap: 12px; }
.chart-item { background: #fff; border: 1px solid #e5e7eb; border-radius: 8px; padding: 8px; box-shadow: 0 1px 2px rgba(0,0,0,0.05); }
.no-data { color: #666; font-size: 12px; padding: 8px; }
@media (max-width: 600px) {
  .controls { gap: 8px; }
  .brand { font-size: 14px; }
  .control.timeframe { flex-basis: 100%; }
  .range-wrap { width: 100%; max-width: none; }
  .two-col { flex-direction: column; }
  .charts-panel { width: 100%; max-width: none; height: 40vh; }
}
</style>
