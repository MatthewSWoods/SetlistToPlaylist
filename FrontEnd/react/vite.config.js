import { defineConfig } from 'vite'
import { resolve } from 'path'
import mkcert from 'vite-plugin-mkcert'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/

// eslint-disable-next-line no-undef
const root  = resolve(__dirname, 'src')
export default defineConfig({
  root,
  plugins: [react(), mkcert() ],
  build: {
    emptyOutDir: true,
    rollupOptions: {
      input: {
        main: resolve(root, 'index.html'),
      }
    }
  },
  server: { https: true }, // Not needed for Vite 5+
  preview: {
    port : 3001
  }
})
