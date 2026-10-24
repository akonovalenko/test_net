<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { initializeSpaRouter, subscribeToBrowserNavigation, useSpaRouter } from '@/router'
import HomeView from '@/views/HomeView.vue'
import LoginView from '@/views/LoginView.vue'
import RegisterView from '@/views/RegisterView.vue'
import AccessDeniedView from '@/views/AccessDeniedView.vue'
import AdminUsersView from '@/views/AdminUsersView.vue'
import Button from 'primevue/button'
import Tag from 'primevue/tag'

const auth = useAuthStore()
const router = useSpaRouter()
const ready = ref(false)
let unsubscribe: (() => void) | undefined

const currentView = computed(() => {
  switch (router.routeName.value) {
    case 'login': return LoginView
    case 'register': return RegisterView
    case 'access-denied': return AccessDeniedView
    case 'admin-users': return AdminUsersView
    default: return HomeView
  }
})

async function navigate(path: string): Promise<void> {
  await router.push(path)
}

async function logout(): Promise<void> {
  await auth.logout()
  await navigate('/login')
}

onMounted(async () => {
  unsubscribe = subscribeToBrowserNavigation(() => undefined)
  await initializeSpaRouter()
  ready.value = true
})

onUnmounted(() => unsubscribe?.())
</script>

<template>
  <div v-if="ready" class="min-h-screen bg-[var(--surface-ground)]">
    <header v-if="auth.isAuthenticated" class="sticky top-0 z-40 border-b border-[var(--surface-border)] bg-[var(--surface-card)]/95 backdrop-blur">
      <div class="mx-auto flex min-h-[72px] max-w-[1200px] items-center justify-between gap-4 px-4 md:px-6">
        <Button
          label="Market Rate Info"
          icon="pi pi-chart-line"
          text
          class="!px-2 !text-lg !font-semibold"
          @click="navigate('/')"
        />

        <div class="flex items-center gap-2 md:gap-3">
          <div class="hidden items-center gap-2 sm:flex">
            <span class="text-sm text-[var(--text-color-secondary)]">{{ auth.user?.email }}</span>
            <Tag :value="auth.isAdmin ? 'Admin' : 'User'" :severity="auth.isAdmin ? 'contrast' : 'secondary'" rounded />
          </div>

          <Button
            v-if="auth.isAdmin"
            label="Users"
            icon="pi pi-users"
            severity="secondary"
            outlined
            size="small"
            @click="navigate('/admin/users')"
          />

          <Button
            label="Logout"
            icon="pi pi-sign-out"
            severity="secondary"
            text
            size="small"
            :loading="auth.loading"
            @click="logout"
          />
        </div>
      </div>
    </header>

    <component :is="currentView" />
  </div>
</template>
