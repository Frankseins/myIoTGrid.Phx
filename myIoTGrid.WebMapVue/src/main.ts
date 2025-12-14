import { createApp } from 'vue'
import App from './App.vue'
import 'leaflet/dist/leaflet.css'
import L from 'leaflet'
// Fix Leaflet default icon paths for Vite/bundlers
// Import image assets and merge into Icon.Default so marker icons load (no 404s)
import iconRetinaUrl from 'leaflet/dist/images/marker-icon-2x.png'
import iconUrl from 'leaflet/dist/images/marker-icon.png'
import shadowUrl from 'leaflet/dist/images/marker-shadow.png'

L.Icon.Default.mergeOptions({
  iconRetinaUrl,
  iconUrl,
  shadowUrl,
})

createApp(App).mount('#app')
