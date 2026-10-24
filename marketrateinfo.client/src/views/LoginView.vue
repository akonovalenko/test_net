<script setup lang="ts">
import { reactive, ref } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { useSpaRouter } from '@/router'
import Card from 'primevue/card'
import InputText from 'primevue/inputtext'
import Password from 'primevue/password'
import Checkbox from 'primevue/checkbox'
import Button from 'primevue/button'
import Message from 'primevue/message'

const auth = useAuthStore()
const router = useSpaRouter()
const form = reactive({ email: '', password: '', rememberMe: true })
const error = ref<string | null>(null)

function getReturnUrl(): string {
  const query = new URLSearchParams(window.location.search)
  return query.get('returnUrl') || '/'
}

async function submit(): Promise<void> {
  error.value = null
  try {
    await auth.login(form)
    await router.push(getReturnUrl())
  } catch (err: unknown) {
    error.value = auth.error ?? (err instanceof Error ? err.message : 'Login failed.')
  }
}
</script>

<template>
  <main class="flex min-h-screen items-center justify-center bg-[var(--surface-ground)] p-4 md:p-6">
    <Card class="w-full max-w-md shadow-md">
      <template #title>Sign in</template>
      <template #subtitle>Sign in to Market Rate Info.</template>

      <template #content>
        <form class="flex flex-col gap-5" @submit.prevent="submit">
          <div class="flex flex-col gap-2">
            <label for="email" class="text-sm font-medium">Email</label>
            <InputText id="email" v-model="form.email" type="email" required autocomplete="email" class="w-full" />
          </div>

          <div class="flex flex-col gap-2">
            <label for="password" class="text-sm font-medium">Password</label>
            <Password id="password" v-model="form.password" required autocomplete="current-password" :feedback="false" toggle-mask fluid />
          </div>

          <div class="flex items-center gap-2">
            <Checkbox v-model="form.rememberMe" input-id="remember" binary />
            <label for="remember" class="text-sm text-[var(--text-color-secondary)]">Remember me</label>
          </div>

          <Message v-if="error" severity="error" :closable="false">{{ error }}</Message>

          <Button type="submit" label="Sign in" icon="pi pi-sign-in" :loading="auth.loading" fluid />
        </form>
      </template>

      <template #footer>
        <div class="text-center text-sm text-[var(--text-color-secondary)]">
          Don't have an account?
          <Button label="Register" link size="small" @click="router.push('/register')" />
        </div>
      </template>
    </Card>
  </main>
</template>
