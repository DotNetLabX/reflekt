import './assets/main.css'

import { createApp } from 'vue'
import { createPinia } from 'pinia'

import App from './App.vue'
import router from './router'
import { setGuestTokenSource } from '@/api/client'
import { useGuestStore } from '@/stores/guest'

const app = createApp(App)
const pinia = createPinia()

app.use(pinia)

const guestStore = useGuestStore(pinia)
setGuestTokenSource(() => guestStore.token)

app.use(router)

app.mount('#app')
