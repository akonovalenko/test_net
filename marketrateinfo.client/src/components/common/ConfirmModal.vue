<script setup lang="ts">
import Dialog from 'primevue/dialog'
import Button from 'primevue/button'
import Message from 'primevue/message'

const props = defineProps<{ visible: boolean; header?: string; message: string }>()
const emit = defineEmits<{ (e: 'accept'): void; (e: 'reject'): void; (e: 'update:visible', value: boolean): void }>()

function close() { emit('update:visible', false) }
</script>

<template>
  <Dialog :visible="props.visible" :modal="true" :header="props.header ?? 'Confirm'" :draggable="false" class="w-[min(28rem,calc(100vw-2rem))]" @hide="close">
    <Message severity="warn" :closable="false" class="mb-5">{{ message }}</Message>
    <div class="flex justify-end gap-2">
      <Button label="Cancel" severity="secondary" text @click="() => { emit('reject'); close() }" />
      <Button label="Yes" icon="pi pi-check" severity="danger" @click="() => { emit('accept'); close() }" />
    </div>
  </Dialog>
</template>
