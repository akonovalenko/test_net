import { createApp } from 'vue'
import { createPinia } from 'pinia'
import PrimeVue from 'primevue/config'
import Aura from '@primeuix/themes/aura'
import Tooltip from 'primevue/tooltip'
import 'primeicons/primeicons.css'
import { initializeSpaRouter } from './router'

import App from './App.vue'
import './assets/main.css'
import './config/chart'

const app = createApp(App)

app.use(createPinia())

app.use(PrimeVue, {
  theme: {
    preset: Aura
  }
})

app.directive('tooltip', Tooltip)

await initializeSpaRouter()

app.mount('#app')
