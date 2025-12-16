<script setup lang="ts">
import { onMounted, onBeforeUnmount, ref, watch } from 'vue'
import {
  Chart,
  LineController,
  LineElement,
  PointElement,
  LinearScale,
  TimeSeriesScale,
  TimeScale,
  Title,
  Tooltip,
  Legend,
  Filler,
  CategoryScale,
} from 'chart.js'

Chart.register(
  LineController,
  LineElement,
  PointElement,
  LinearScale,
  TimeScale,
  TimeSeriesScale,
  Title,
  Tooltip,
  Legend,
  Filler,
  CategoryScale,
)

const props = defineProps<{ 
  title: string
  unit?: string
  labels: string[]
  values: (number | null)[]
  color?: string
}>()

const canvasRef = ref<HTMLCanvasElement | null>(null)
let chart: Chart | null = null

onMounted(() => {
  if (!canvasRef.value) return
  chart = new Chart(canvasRef.value.getContext('2d')!, {
    type: 'line',
    data: {
      labels: props.labels,
      datasets: [
        {
          label: props.unit ? `${props.title} (${props.unit})` : props.title,
          data: props.values,
          borderColor: props.color ?? '#1976D2',
          backgroundColor: (props.color ?? '#1976D2') + '33',
          fill: true,
          borderWidth: 2,
          pointRadius: 0,
          spanGaps: true,
        },
      ],
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      interaction: { mode: 'index', intersect: false },
      scales: {
        x: { display: true, ticks: { maxRotation: 0 }, grid: { display: false } },
        y: { display: true, grid: { color: '#eee' } },
      },
      plugins: {
        legend: { display: false },
        title: { display: true, text: props.title },
      },
    },
  })
})

onBeforeUnmount(() => {
  if (chart) {
    chart.destroy()
    chart = null
  }
})

watch(() => [props.labels, props.values], () => {
  if (!chart) return
  chart.data.labels = props.labels as any
  if (!chart.data.datasets || chart.data.datasets.length === 0) {
    (chart.data as any).datasets = [
      {
        label: props.unit ? `${props.title} (${props.unit})` : props.title,
        data: props.values,
        borderColor: props.color ?? '#1976D2',
        backgroundColor: (props.color ?? '#1976D2') + '33',
        fill: true,
        borderWidth: 2,
        pointRadius: 0,
        spanGaps: true,
      },
    ]
  } else {
    (chart.data.datasets[0] as any).data = props.values
  }
  chart.update('none')
})
</script>

<template>
  <div style="height:220px; width:100%">
    <canvas ref="canvasRef"></canvas>
  </div>
  <div style="font-size:12px; color:#777; margin-top:4px">Unit: {{ props.unit ?? '—' }}</div>
  
</template>
