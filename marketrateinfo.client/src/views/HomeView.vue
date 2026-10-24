<template>
  <main class="page-shell">
    <div class="page-container">
      <div class="mb-6 flex flex-col gap-5 lg:flex-row lg:items-start lg:justify-between">
        <div>
          <div class="mb-2 flex items-center gap-3">
            <i class="pi pi-chart-line text-2xl text-[var(--primary-color)]" />
            <h1 class="text-2xl font-semibold tracking-tight md:text-3xl">{{ applicationName }}</h1>
          </div>
          <p v-if="lastRefresh" class="text-sm text-[var(--text-color-secondary)]">
            Updated from cloud provider: {{ new Date(lastRefresh.refreshedAtUtc).toLocaleString() }}
            · saved {{ lastRefresh.ratesSaved }} rate(s)
            · providers tried {{ lastRefresh.providersUsed }}
          </p>
        </div>
        <WeatherWidget />
      </div>

      <div v-if="auth.isAdmin" class="mb-5">
        <AddSymbolModal />
      </div>

      <FilterPanel
        v-model:selectedSymbol="selectedSymbol"
        v-model:dateRange="dateRange"
        :symbols="symbols"
        :today="today"
        :status="status"
        :error="error"
        :can-manage="auth.isAdmin"
        @search="search"
        @refresh="onRefresh"
        @delete-currency-pair="onRemoveCurrencyPair"
      />

      <div class="mt-2 grid grid-cols-1 gap-2 lg:grid-cols-3 lg:gap-6">
        <div class="lg:col-span-2">
          <RateChart :rates="rates" :symbol="selectedSymbol" :loading="status === 'loading'" />
        </div>
        <div class="lg:col-span-1">
          <RatesTable :rates="rates" :loading="status === 'loading'" />
        </div>
      </div>
    </div>
  </main>
</template>

<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { storeToRefs } from 'pinia'
import { useAuthStore } from '@/stores/auth'
import FilterPanel from '@/components/FilterPanel.vue'
import RateChart from '@/components/RateChart.vue'
import RatesTable from '@/components/RatesTable.vue'
import AddSymbolModal from '@/components/AddSymbolModal.vue'
import WeatherWidget from '@/components/WeatherWidget.vue'
import { useMarketRateStore } from '@/stores/marketRate'

const marketRateStore = useMarketRateStore()
const auth = useAuthStore()
const { applicationName, status, rates, symbols, error, lastRefresh } = storeToRefs(marketRateStore)
const today = new Date()
const selectedSymbol = ref<string | null>(null)
const dateRange = ref<Date[] | null>(null)

function getSearchRange(): [Date, Date] | null {
  const symbol = selectedSymbol.value
  const range = dateRange.value
  if (!symbol || !range || range.length !== 2) return null
  const [from, to] = range
  if (!(from instanceof Date) || !(to instanceof Date)) return null
  return [from, to]
}

async function search(): Promise<void> {
  const range = getSearchRange()
  if (!range || !selectedSymbol.value) return
  await marketRateStore.loadRates(selectedSymbol.value, range[0], range[1])
}

async function onRefresh(): Promise<void> {
  const range = getSearchRange()
  try {
    await marketRateStore.refreshSingleRate(selectedSymbol.value ?? undefined, range?.[0], range?.[1])
  } catch {
    // Error is stored by the market-rate store.
  }
}

async function onRemoveCurrencyPair(symbol: string): Promise<void> {
  try {
    const removed = await marketRateStore.RemoveCurrencyPair(symbol)
    if (!removed) return
    await marketRateStore.loadSymbols()
    if (selectedSymbol.value === symbol) selectedSymbol.value = symbols.value[0] ?? null
  } catch {
    // Error is stored by the market-rate store.
  }
}

watch([selectedSymbol, dateRange], async () => {
  if (status.value === 'loading') return
  await search()
}, { deep: true })

onMounted(async () => {
  await marketRateStore.loadSymbols()
  if (symbols.value.length === 0) return
  selectedSymbol.value = symbols.value[0]
  const from = new Date(today)
  from.setDate(from.getDate() - 7)
  dateRange.value = [from, new Date(today)]
})
</script>
