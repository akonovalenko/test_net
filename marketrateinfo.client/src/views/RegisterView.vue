<script setup lang="ts">
import { reactive, ref } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { useSpaRouter } from '@/router'
import Card from 'primevue/card'
import InputText from 'primevue/inputtext'
import Password from 'primevue/password'
import Button from 'primevue/button'
import Message from 'primevue/message'

const auth = useAuthStore()
const router = useSpaRouter()
const form = reactive({ email: '', password: '', confirmPassword: '' })
const error = ref<string | null>(null)

async function submit(): Promise<void> {
  error.value = null
  if (form.password !== form.confirmPassword) {
    error.value = 'Passwords do not match.'
    return
  }
  try {
    await auth.register(form)
    await router.push('/')
  } catch (err: unknown) {
    error.value = auth.error ?? (err instanceof Error ? err.message : 'Registration failed.')
  }
}
</script>

<template>
  <main class="flex min-h-screen items-center justify-center bg-[var(--surface-ground)] p-4 md:p-6">
    <Card class="w-full max-w-md shadow-md">
      <template #title>Create account</template>
      <template #subtitle>New accounts are assigned the User role.</template>

      <template #content>
        <form class="flex flex-col gap-5" @submit.prevent="submit">
          <div class="flex flex-col gap-2">
            <label for="email" class="text-sm font-medium">Email</label>
            <InputText id="email" v-model="form.email" type="email" required autocomplete="email" class="w-full" />
          </div>

          <div class="flex flex-col gap-2">
            <label for="password" class="text-sm font-medium">Password</label>
            <Password id="password" v-model="form.password" required minlength="8" autocomplete="new-password" toggle-mask fluid />
          </div>

          <div class="flex flex-col gap-2">
            <label for="confirmPassword" class="text-sm font-medium">Confirm password</label>
            <Password id="confirmPassword" v-model="form.confirmPassword" required minlength="8" autocomplete="new-password" :feedback="false" toggle-mask fluid />
          </div>

          <Message v-if="error" severity="error" :closable="false">{{ error }}</Message>

          <Button type="submit" label="Register" icon="pi pi-user-plus" :loading="auth.loading" fluid />
        </form>
      </template>

      <template #footer>
        <div class="text-center text-sm text-[var(--text-color-secondary)]">
          Already registered?
          <Button label="Sign in" link size="small" @click="router.push('/login')" />
        </div>
      </template>
    </Card>
  </main>
</template>
