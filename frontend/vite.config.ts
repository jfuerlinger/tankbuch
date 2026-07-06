import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Backend-URL, die Aspire per Service Discovery injiziert (Ressourcenname "api").
// So funktioniert /api/* im Dev-Server genauso wie im Docker-Compose-Deployment,
// wo YARP denselben Pfad an das Backend weiterleitet (siehe AppHost.cs).
const apiTarget = process.env.API_HTTPS || process.env.API_HTTP || 'http://localhost:5080'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': {
        target: apiTarget,
        changeOrigin: true,
        secure: false,
      },
    },
  },
})
