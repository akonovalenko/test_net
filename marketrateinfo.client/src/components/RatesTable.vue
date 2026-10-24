<script setup lang="ts">
import { computed } from 'vue'
import Column from 'primevue/column'
import DataTable from 'primevue/datatable'
import Card from 'primevue/card'
import type { MarketRate } from '@/types/marketRate'

const props = defineProps<{ rates: MarketRate[]; loading: boolean }>()
const dateFormatter = new Intl.DateTimeFormat('uk-UA', { dateStyle: 'short', timeStyle: 'medium' })
const formattedRates = computed(() => props.rates)
function formatRate(value: number): string { return Number.isFinite(value) ? value.toFixed(4) : '-' }
function formatDateTime(value: string): string { const date = new Date(value); return Number.isNaN(date.getTime()) ? '-' : dateFormatter.format(date) }
</script>

<template>
  <Card class="mt-6 h-full">
    <template #title>Market rates</template>
    <template #content>
      <DataTable :value="formattedRates" :loading="loading" data-key="id" striped-rows paginator :rows="10" :rows-per-page-options="[10, 25, 50]" responsive-layout="scroll" empty-message="No market rates found for the selected period">
        <Column field="symbol" header="Currency pair" />
        <Column field="rate" header="Rate" header-class="text-right" body-class="text-right">
          <template #body="{ data }">{{ formatRate(data.rate) }}</template>
        </Column>
        <Column field="updatedAtUtc" header="Updated">
          <template #body="{ data }">{{ formatDateTime(data.updatedAtUtc) }}</template>
        </Column>
      </DataTable>
    </template>
  </Card>
</template>
