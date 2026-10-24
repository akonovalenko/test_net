<script setup lang="ts">
import { useSpaRouter } from '@/router'
import Card from 'primevue/card'
import Button from 'primevue/button'
import Message from 'primevue/message'

const router = useSpaRouter()
</script>

<template>
  <main class="flex min-h-screen items-center justify-center bg-[var(--surface-ground)] p-6">
    <Card class="w-full max-w-lg text-center shadow-md">
      <template #title>Access denied</template>
      <template #content>
        <Message severity="warn" :closable="false" class="mb-5">
          You do not have permission to perform this action.
        </Message>
        <Button label="Back to dashboard" icon="pi pi-home" @click="router.push('/')" />
      </template>
    </Card>
  </main>
</template>
