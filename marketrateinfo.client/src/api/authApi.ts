import apiClient from './axios'

export interface AuthUser {
  id: string
  email: string
  roles: string[]
}

export interface LoginRequest {
  email: string
  password: string
  rememberMe: boolean
}

export interface RegisterRequest {
  email: string
  password: string
  confirmPassword: string
}

export interface AdminUser {
  id: string
  email: string
  roles: string[]
}

export interface CreateAdminUserRequest {
  email: string
  password: string
  confirmPassword: string
  role: 'Admin' | 'User'
}

export interface UpdateAdminUserRequest {
  email: string
  password?: string
  confirmPassword?: string
  role: 'Admin' | 'User'
}

export async function login(request: LoginRequest): Promise<AuthUser> {
  const response = await apiClient.post<AuthUser>('/auth/login', request)
  return response.data
}

export async function register(request: RegisterRequest): Promise<AuthUser> {
  const response = await apiClient.post<AuthUser>('/auth/register', request)
  return response.data
}

export async function logout(): Promise<void> {
  await apiClient.post('/auth/logout')
}

export async function getCurrentUser(): Promise<AuthUser> {
  const response = await apiClient.get<AuthUser>('/auth/me')
  return response.data
}

export async function getAdminUsers(): Promise<AdminUser[]> {
  const response = await apiClient.get<AdminUser[]>('/admin/users')
  return response.data
}

export async function createAdminUser(
  request: CreateAdminUserRequest
): Promise<AdminUser> {
  const response = await apiClient.post<AdminUser>('/admin/users', request)
  return response.data
}

export async function updateAdminUser(
  id: string,
  request: UpdateAdminUserRequest
): Promise<AdminUser> {
  const response = await apiClient.put<AdminUser>(`/admin/users/${id}`, request)
  return response.data
}

export async function deleteAdminUser(id: string): Promise<void> {
  await apiClient.delete(`/admin/users/${id}`)
}
