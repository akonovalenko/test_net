import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import axios from 'axios'
import * as authApi from '@/api/authApi'
import type { AuthUser, LoginRequest, RegisterRequest } from '@/api/authApi'

export const useAuthStore = defineStore('auth', () => {
  const user = ref<AuthUser | null>(null)
  const loading = ref(false)
  const initialized = ref(false)
  const error = ref<string | null>(null)

  const isAuthenticated = computed(() => user.value !== null)
  const isAdmin = computed(() => user.value?.roles.includes('Admin') ?? false)
  const isUser = computed(() => user.value?.roles.includes('User') ?? false)

  async function initialize(): Promise<void> {
    if (initialized.value) return

    loading.value = true
    error.value = null

    try {
      user.value = await authApi.getCurrentUser()
    } catch (err: unknown) {
      if (axios.isAxiosError(err) && err.response?.status === 401) {
        user.value = null
      } else {
        error.value = getErrorMessage(err)
        user.value = null
      }
    } finally {
      initialized.value = true
      loading.value = false
    }
  }

  async function login(request: LoginRequest): Promise<void> {
    loading.value = true
    error.value = null

    try {
      user.value = await authApi.login(request)
    } catch (err: unknown) {
      error.value = getErrorMessage(err)
      throw err
    } finally {
      loading.value = false
    }
  }

  async function register(request: RegisterRequest): Promise<void> {
    loading.value = true
    error.value = null

    try {
      user.value = await authApi.register(request)
    } catch (err: unknown) {
      error.value = getErrorMessage(err)
      throw err
    } finally {
      loading.value = false
    }
  }

  async function logout(): Promise<void> {
    loading.value = true
    error.value = null

    try {
      await authApi.logout()
    } finally {
      user.value = null
      loading.value = false
    }
  }

  function clearError(): void {
    error.value = null
  }

  function getErrorMessage(err: unknown): string {
    if (axios.isAxiosError(err)) {
      const data = err.response?.data as
        | { message?: string; errors?: Record<string, string[]> }
        | undefined

      if (data?.message) return data.message

      const validationErrors = data?.errors
        ? Object.values(data.errors).flat()
        : []

      if (validationErrors.length > 0) return validationErrors.join(' ')

      if (err.response?.status === 401) return 'Invalid email or password.'
      if (err.response?.status === 409) return 'A user with this email already exists.'
    }

    return err instanceof Error ? err.message : 'Authentication failed.'
  }

  return {
    user,
    loading,
    initialized,
    error,
    isAuthenticated,
    isAdmin,
    isUser,
    initialize,
    login,
    register,
    logout,
    clearError
  }
})
