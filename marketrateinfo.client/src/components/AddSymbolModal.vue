<template>
  <div>
    <Button label="Add currency pair" icon="pi pi-plus" severity="success" @click="openDialog" />

    <Dialog v-model:visible="visible" header="Add currency pair" modal :draggable="false" class="w-[min(32rem,calc(100vw-2rem))]">
      <form class="flex flex-col gap-5" @submit.prevent="handleAdd">
        <div class="flex flex-col gap-2">
          <label for="symbol" class="text-sm font-medium">Currency pair symbol</label>
          <InputText id="symbol" v-model="symbolInput" placeholder="EUR/USD" :invalid="!!error" class="w-full" autofocus />
          <small v-if="error" class="text-red-500">{{ error }}</small>
        </div>
        <div class="flex justify-end gap-2">
          <Button type="button" label="Cancel" icon="pi pi-times" severity="secondary" text :disabled="loading" @click="visible = false" />
          <Button type="submit" label="Add" icon="pi pi-check" severity="success" :loading="loading" />
        </div>
      </form>
    </Dialog>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import Button from 'primevue/button'
import Dialog from 'primevue/dialog'
import InputText from 'primevue/inputtext'
import { addSymbol } from '@/api/homeApi'
import { useMarketRateStore } from '@/stores/marketRate'

const marketRateStore = useMarketRateStore()
const visible = ref(false)
const symbolInput = ref('')
const loading = ref(false)
const error = ref('')

function openDialog() { symbolInput.value = ''; error.value = ''; visible.value = true }

async function handleAdd() {
  const trimmed = symbolInput.value.trim().toUpperCase()
  if (!trimmed) { error.value = 'Please enter a currency pair symbol.'; return }
  if (!/^[A-Z]{3}\/[A-Z]{3}$/.test(trimmed)) { error.value = 'Format must be BASE/QUOTE (e.g. EUR/USD).'; return }
  error.value = ''; loading.value = true
  try { await addSymbol(trimmed); visible.value = false; await marketRateStore.loadSymbols() }
  catch (e: any) { error.value = e.response?.data?.message || 'Failed to add currency pair.' }
  finally { loading.value = false }
}
</script>
