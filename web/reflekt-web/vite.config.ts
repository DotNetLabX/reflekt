import { fileURLToPath, URL } from 'node:url'

import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import vueDevTools from 'vite-plugin-vue-devtools'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    vue(),
    vueDevTools(),
    tailwindcss(),
  ],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    },
  },
  server: {
    proxy: {
      '/api': 'http://localhost:4001',
      '/auth': 'http://localhost:4001',
      '/signin-google': 'http://localhost:4001',
      '/hubs': {
        target: 'http://localhost:4001',
        ws: true,
      },
    },
  },
  build: {
    outDir: '../../src/Services/Reflekt/Reflekt.API/wwwroot',
    emptyOutDir: true,
  },
})
