import { apiFetch } from './client'
import type { User } from '@/types/auth'

export const authApi = {
  me: () => apiFetch<User>('/auth/me'),
  logout: () => apiFetch<void>('/auth/logout', { method: 'POST' }),
}
