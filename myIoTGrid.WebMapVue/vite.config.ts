import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [vue()],
  server: {
    port: 5173,
    host: true,
    proxy: {
      // Proxy API to backend to avoid CORS in browser during dev
      '/api': {
        target: 'https://localhost:5001',
        changeOrigin: true,
        secure: false, // allow self-signed certs for local dev
        ws: true,
      },
      // If using SignalR hubs
      '/hubs': {
        target: 'https://localhost:5001',
        changeOrigin: true,
        secure: false,
        ws: true,
      },
    },
  },
})
