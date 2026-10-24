<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import axios from 'axios'
import {
  createAdminUser,
  deleteAdminUser,
  getAdminUsers,
  updateAdminUser,
  type AdminUser,
  type CreateAdminUserRequest,
  type UpdateAdminUserRequest
} from '@/api/authApi'
import { useAuthStore } from '@/stores/auth'
import Card from 'primevue/card'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Button from 'primevue/button'
import Tag from 'primevue/tag'
import Dialog from 'primevue/dialog'
import InputText from 'primevue/inputtext'
import Password from 'primevue/password'
import Select from 'primevue/select'
import Message from 'primevue/message'
import ProgressSpinner from 'primevue/progressspinner'

const auth = useAuthStore()
const users = ref<AdminUser[]>([])
const loading = ref(false)
const saving = ref(false)
const error = ref<string | null>(null)
const success = ref<string | null>(null)
const showCreate = ref(false)
const editingUser = ref<AdminUser | null>(null)
const deletingUser = ref<AdminUser | null>(null)
const formError = ref<string | null>(null)

const createForm = ref<CreateAdminUserRequest>({ email: '', password: '', confirmPassword: '', role: 'User' })
const editForm = ref<UpdateAdminUserRequest>({ email: '', password: '', confirmPassword: '', role: 'User' })
const roles = ['User', 'Admin']

const sortedUsers = computed(() => [...users.value].sort((a, b) => a.email.localeCompare(b.email)))

function getErrorMessage(err: unknown): string {
  if (axios.isAxiosError(err)) {
    const data = err.response?.data as { message?: string; errors?: Record<string, string[]> } | undefined
    if (data?.message) return data.message
    const validationErrors = data?.errors ? Object.values(data.errors).flat() : []
    if (validationErrors.length) return validationErrors.join(' ')
  }
  return err instanceof Error ? err.message : 'Operation failed.'
}

function resetMessages(): void {
  error.value = null
  success.value = null
  formError.value = null
}

async function loadUsers(): Promise<void> {
  loading.value = true
  error.value = null
  try {
    users.value = await getAdminUsers()
  } catch (err) {
    error.value = getErrorMessage(err)
  } finally {
    loading.value = false
  }
}

function openCreate(): void {
  resetMessages()
  createForm.value = { email: '', password: '', confirmPassword: '', role: 'User' }
  showCreate.value = true
}

function openEdit(user: AdminUser): void {
  resetMessages()
  editingUser.value = user
  editForm.value = {
    email: user.email,
    password: '',
    confirmPassword: '',
    role: user.roles.includes('Admin') ? 'Admin' : 'User'
  }
}

function closeDialogs(): void {
  if (!saving.value) {
    showCreate.value = false
    editingUser.value = null
    deletingUser.value = null
  }
}

async function createUser(): Promise<void> {
  formError.value = null
  saving.value = true
  try {
    const created = await createAdminUser(createForm.value)
    users.value.push(created)
    showCreate.value = false
    success.value = `User ${created.email} was created.`
  } catch (err) {
    formError.value = getErrorMessage(err)
  } finally {
    saving.value = false
  }
}

async function saveUser(): Promise<void> {
  if (!editingUser.value) return
  formError.value = null
  saving.value = true
  try {
    const updated = await updateAdminUser(editingUser.value.id, editForm.value)
    const index = users.value.findIndex(x => x.id === updated.id)
    if (index >= 0) users.value[index] = updated
    if (updated.id === auth.user?.id) auth.user = updated
    editingUser.value = null
    success.value = `User ${updated.email} was updated.`
  } catch (err) {
    formError.value = getErrorMessage(err)
  } finally {
    saving.value = false
  }
}

async function confirmDelete(): Promise<void> {
  if (!deletingUser.value) return
  saving.value = true
  formError.value = null
  try {
    const deleted = deletingUser.value
    await deleteAdminUser(deleted.id)
    users.value = users.value.filter(x => x.id !== deleted.id)
    deletingUser.value = null
    success.value = `User ${deleted.email} was deleted.`
  } catch (err) {
    formError.value = getErrorMessage(err)
  } finally {
    saving.value = false
  }
}

function isCurrentUser(user: AdminUser): boolean {
  return user.id === auth.user?.id
}

onMounted(loadUsers)
</script>

<template>
  <main class="page-shell">
    <div class="page-container">
      <div class="mb-6 flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
        <div>
          <h1 class="text-2xl font-semibold tracking-tight md:text-3xl">User management</h1>
          <p class="mt-1 text-sm text-[var(--text-color-secondary)]">Create users, manage roles and passwords, or remove accounts.</p>
        </div>
        <Button label="Add user" icon="pi pi-user-plus" @click="openCreate" />
      </div>

      <div class="mb-4 space-y-3">
        <Message v-if="success" severity="success" :closable="false">{{ success }}</Message>
        <Message v-if="error" severity="error" :closable="false">{{ error }}</Message>
      </div>

      <Card>
        <template #content>
          <DataTable
            :value="sortedUsers"
            :loading="loading"
            data-key="id"
            striped-rows
            paginator
            :rows="10"
            :rows-per-page-options="[10, 25, 50]"
            responsive-layout="scroll"
            empty-message="No users found."
          >
            <template #empty>No users found.</template>
            <template #loading><div class="flex items-center justify-center gap-3 py-8"><ProgressSpinner style="width: 2rem; height: 2rem" stroke-width="4" /><span>Loading users...</span></div></template>

            <Column field="email" header="Email" sortable>
              <template #body="{ data }">
                <div class="flex items-center gap-2 font-medium">
                  <i class="pi pi-user text-[var(--text-color-secondary)]" />
                  <span>{{ data.email }}</span>
                  <Tag v-if="isCurrentUser(data)" value="You" severity="info" rounded />
                </div>
              </template>
            </Column>

            <Column header="Role" sortable>
              <template #body="{ data }">
                <Tag :value="data.roles.includes('Admin') ? 'Admin' : 'User'" :severity="data.roles.includes('Admin') ? 'contrast' : 'secondary'" rounded />
              </template>
            </Column>

            <Column header="Actions" :exportable="false">
              <template #body="{ data }">
                <div class="flex justify-end gap-2">
                  <Button icon="pi pi-pencil" label="Edit" severity="secondary" outlined size="small" @click="openEdit(data)" />
                  <Button v-if="!isCurrentUser(data)" icon="pi pi-trash" label="Delete" severity="danger" outlined size="small" @click="deletingUser = data; formError = null" />
                </div>
              </template>
            </Column>
          </DataTable>
        </template>
      </Card>
    </div>

    <Dialog v-model:visible="showCreate" header="Add user" modal :draggable="false" :closable="!saving" class="w-[min(32rem,calc(100vw-2rem))]">
      <form class="flex flex-col gap-5" @submit.prevent="createUser">
        <div class="flex flex-col gap-2"><label for="create-email" class="text-sm font-medium">Email</label><InputText id="create-email" v-model="createForm.email" type="email" required class="w-full" /></div>
        <div class="flex flex-col gap-2"><label for="create-password" class="text-sm font-medium">Password</label><Password id="create-password" v-model="createForm.password" required minlength="8" toggle-mask fluid /></div>
        <div class="flex flex-col gap-2"><label for="create-confirm" class="text-sm font-medium">Confirm password</label><Password id="create-confirm" v-model="createForm.confirmPassword" required minlength="8" :feedback="false" toggle-mask fluid /></div>
        <div class="flex flex-col gap-2"><label for="create-role" class="text-sm font-medium">Role</label><Select id="create-role" v-model="createForm.role" :options="roles" class="w-full" /></div>
        <Message v-if="formError" severity="error" :closable="false">{{ formError }}</Message>
        <div class="flex justify-end gap-2 pt-2"><Button type="button" label="Cancel" severity="secondary" text :disabled="saving" @click="closeDialogs" /><Button type="submit" label="Create user" icon="pi pi-check" :loading="saving" /></div>
      </form>
    </Dialog>

    <Dialog :visible="!!editingUser" header="Edit user" modal :draggable="false" :closable="!saving" class="w-[min(32rem,calc(100vw-2rem))]" @update:visible="value => !value && closeDialogs()">
      <form v-if="editingUser" class="flex flex-col gap-5" @submit.prevent="saveUser">
        <div class="flex flex-col gap-2"><label for="edit-email" class="text-sm font-medium">Email</label><InputText id="edit-email" v-model="editForm.email" type="email" required class="w-full" /></div>
        <div class="flex flex-col gap-2"><label for="edit-password" class="text-sm font-medium">New password <span class="font-normal text-[var(--text-color-secondary)]">(optional)</span></label><Password id="edit-password" v-model="editForm.password" minlength="8" :feedback="false" toggle-mask fluid /></div>
        <div class="flex flex-col gap-2"><label for="edit-confirm" class="text-sm font-medium">Confirm new password</label><Password id="edit-confirm" v-model="editForm.confirmPassword" minlength="8" :feedback="false" toggle-mask fluid /></div>
        <div class="flex flex-col gap-2"><label for="edit-role" class="text-sm font-medium">Role</label><Select id="edit-role" v-model="editForm.role" :options="roles" :disabled="isCurrentUser(editingUser)" class="w-full" /></div>
        <Message v-if="isCurrentUser(editingUser)" severity="info" :closable="false">Your own Admin role cannot be removed.</Message>
        <Message v-if="formError" severity="error" :closable="false">{{ formError }}</Message>
        <div class="flex justify-end gap-2 pt-2"><Button type="button" label="Cancel" severity="secondary" text :disabled="saving" @click="closeDialogs" /><Button type="submit" label="Save changes" icon="pi pi-check" :loading="saving" /></div>
      </form>
    </Dialog>

    <Dialog :visible="!!deletingUser" header="Delete user" modal :draggable="false" :closable="!saving" class="w-[min(30rem,calc(100vw-2rem))]" @update:visible="value => !value && closeDialogs()">
      <div v-if="deletingUser" class="flex flex-col gap-5">
        <Message severity="warn" :closable="false">This action cannot be undone.</Message>
        <p class="text-[var(--text-color-secondary)]">Are you sure you want to delete <strong class="text-[var(--text-color)]">{{ deletingUser.email }}</strong>?</p>
        <Message v-if="formError" severity="error" :closable="false">{{ formError }}</Message>
        <div class="flex justify-end gap-2"><Button label="Cancel" severity="secondary" text :disabled="saving" @click="closeDialogs" /><Button label="Delete user" icon="pi pi-trash" severity="danger" :loading="saving" @click="confirmDelete" /></div>
      </div>
    </Dialog>
  </main>
</template>
