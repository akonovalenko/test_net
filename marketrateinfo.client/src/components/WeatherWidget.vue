<template>
  <Card class="w-full md:w-auto">
    <template #content>
      <div class="flex flex-wrap items-center gap-4">
        <div class="flex items-center gap-3">
          <i class="pi pi-sun text-2xl text-amber-500" />
          <div>
            <div class="font-semibold">{{ weatherStore.currentWeather?.cityName || 'City' }}</div>
            <div class="text-xs text-[var(--text-color-secondary)]">{{ weatherStore.currentWeather?.summary }}</div>
          </div>
        </div>
        <Divider layout="vertical" class="!mx-0 hidden md:flex" />
        <div class="flex items-center gap-3 text-xs text-[var(--text-color-secondary)]">
          <span><i class="pi pi-compass mr-1 text-[var(--primary-color)]" />{{ weatherStore.currentWeather?.windSpeed }} km/h</span>
          <span><i class="pi pi-cloud mr-1 text-cyan-500" />{{ weatherStore.currentWeather?.humidity }}%</span>
        </div>
        <Tag :value="`${weatherStore.currentWeather?.temperatureC ?? '-'}°C`" severity="contrast" class="ml-auto text-base" />
      </div>
    </template>
  </Card>
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import Card from 'primevue/card'
import Divider from 'primevue/divider'
import Tag from 'primevue/tag'
import { useWeatherStore } from '@/stores/weather'
const weatherStore = useWeatherStore()
onMounted(() => weatherStore.fetchWeather())
</script>
