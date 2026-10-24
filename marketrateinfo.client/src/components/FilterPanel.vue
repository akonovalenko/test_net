<script setup lang="ts">
import { ref, computed } from 'vue'
import Button from 'primevue/button'
import Card from 'primevue/card'
import DatePicker from 'primevue/datepicker'
import Message from 'primevue/message'
import Select from 'primevue/select'
import ConfirmModal from '@/components/common/ConfirmModal.vue'

type RequestStatus = 'idle' | 'loading' | 'success' | 'error'
const showConfirm = ref(false)
const props = defineProps<{ symbols: string[]; today: Date; status: RequestStatus; error: string | null; canManage: boolean }>()
const selectedSymbol = defineModel<string | null>('selectedSymbol', { default: null })
const dateRange = defineModel<Date[] | null>('dateRange', { default: null })
const emit = defineEmits<{ (event: 'search'): void; (event: 'refresh'): void; (event: 'deleteCurrencyPair', symbol: string): void }>()

const startDate = computed<Date | null>({
  get: () => dateRange.value?.[0] ?? null,
  set: date => {
    const endDate = dateRange.value?.[1] ?? null
    if (!date) { dateRange.value = null; return }
    dateRange.value = endDate && date > endDate ? [date] : endDate ? [date, endDate] : [date]
  }
})
const endDate = computed<Date | null>({
  get: () => dateRange.value?.[1] ?? null,
  set: date => {
    const start = dateRange.value?.[0] ?? null
    if (!date) { dateRange.value = start ? [start] : null; return }
    dateRange.value = start ? [start, date] : [date]
  }
})
const canSearch = computed(() => !!selectedSymbol.value && Array.isArray(dateRange.value) && dateRange.value.length === 2 && dateRange.value.every(date => date instanceof Date))
const canDelete = computed(() => props.canManage && !!selectedSymbol.value && props.status !== 'loading')
function onDeleteConfirm() { if (canDelete.value && selectedSymbol.value) emit('deleteCurrencyPair', selectedSymbol.value) }
</script>

<template>
  <Card class="mt-6">
    <template #content>
      <div class="grid grid-cols-1 gap-5 lg:grid-cols-[minmax(14rem,1fr)_minmax(22rem,2fr)_auto_auto] lg:items-end">
        <div class="flex flex-col gap-2">
          <label for="symbol" class="text-sm font-medium">Currency pair</label>
          <div class="flex gap-2">
            <Select id="symbol" v-model="selectedSymbol" :options="symbols" placeholder="Select currency pair" :disabled="symbols.length === 0 || status === 'loading'" class="min-w-0 flex-1" />
            <Button type="button" icon="pi pi-trash" severity="danger" outlined :disabled="!canDelete" aria-label="Delete currency pair" v-tooltip.top="'Delete currency pair'" @click="showConfirm = true" />
          </div>
        </div>

        <div class="flex flex-col gap-2">
          <label class="text-sm font-medium">Period</label>
          <div class="flex items-center gap-2">
            <DatePicker v-model="startDate" input-id="start-date" :manual-input="false" date-format="dd.mm.yy" :max-date="endDate ?? today" :disabled="status === 'loading'" show-icon icon-display="input" placeholder="Start date" class="min-w-0 flex-1" />
            <span class="text-[var(--text-color-secondary)]">–</span>
            <DatePicker v-model="endDate" input-id="end-date" :manual-input="false" date-format="dd.mm.yy" :min-date="startDate ?? undefined" :max-date="today" :disabled="status === 'loading'" show-icon icon-display="input" placeholder="End date" class="min-w-0 flex-1" />
          </div>
        </div>

        <Button type="button" label="Search" icon="pi pi-search" :loading="status === 'loading'" :disabled="!canSearch" @click="emit('search')" />
        <Button v-if="canManage" type="button" label="Update" icon="pi pi-refresh" severity="secondary" :loading="status === 'loading'" :disabled="!selectedSymbol" @click="emit('refresh')" />
      </div>
      <Message v-if="error" severity="error" :closable="false" class="mt-5">{{ error }}</Message>
    </template>
  </Card>

  <ConfirmModal v-model:visible="showConfirm" :message="`Are you sure you want to delete ${selectedSymbol}?`" @accept="onDeleteConfirm" />
</template>
