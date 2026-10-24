<script setup lang="ts">
import { computed } from 'vue'
import Chart from 'primevue/chart'
import Card from 'primevue/card'
import type { MarketRate } from '@/types/marketRate'

const props = defineProps<{
  rates: MarketRate[]
  symbol: string | null
  loading?: boolean
}>()

/**
 * Build chart data:
 * - Use numeric timestamps for x (milliseconds)
 * - Sort ascending by x
 */
const chartData = computed(() => {
  const points = (props.rates ?? [])
    .map(rate => {
      const date = new Date(rate.updatedAtUtc)
      return {
        x: date.getTime(),
        y: Number(rate.rate)
      }
    })
    .filter(p => Number.isFinite(p.x) && Number.isFinite(p.y))
    .sort((a, b) => a.x - b.x)

  return {
    datasets: [
      {
        label: props.symbol ? `${props.symbol} Rate` : 'Market Rate',
        data: points,
        fill: false,
        tension: 0.1,
        pointRadius: 3,
        borderColor: '#22c55e',
        backgroundColor: 'rgba(34,197,94,0.12)'
      }
    ]
  }
})

const chartOptions = {
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: { display: true },
    tooltip: {
      mode: 'index',
      intersect: false,
      callbacks: {
        // format tooltip value to 4 decimals (match table)
        label: function(context: any) {
          const v = context.raw?.y ?? context.parsed?.y ?? context.parsed
          return `${context.dataset.label}: ${Number(v).toFixed(4)}`
        }
      }
    }
  },
  scales: {
    x: {
      type: 'time',
      time: {
        unit: 'day',
        tooltipFormat: 'yyyy-MM-dd HH:mm'
      },
      title: { display: true, text: 'Date' }
    },
    y: {
      beginAtZero: false,
      title: { display: true, text: 'Rate' }
    }
  }
}
</script>

<template>
  <Card class="mt-6 h-full">
    <template #title>Rate history</template>
    <template #content>
    <div v-if="loading" class="flex h-[400px] items-center justify-center text-[var(--text-color-secondary)]">
      Loading...
    </div>

    <div v-else-if="!props.rates || props.rates.length === 0" class="flex h-[400px] items-center justify-center text-[var(--text-color-secondary)]">
      No market rates found for the selected period
    </div>

    <div v-else class="h-[400px]">
      <Chart type="line" :data="chartData" :options="chartOptions" class="h-full" />
    </div>
    </template>
  </Card>
</template>
