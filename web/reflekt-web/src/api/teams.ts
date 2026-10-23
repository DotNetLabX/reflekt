import { apiFetch } from './client'
import type { TeamSummary } from '@/types/team'

export const teamsApi = {
  list: () => apiFetch<TeamSummary[]>('/api/teams'),
  create: (name: string) => apiFetch<TeamSummary>('/api/teams', { method: 'POST', body: { name } }),
  remove: (teamId: number, confirmationName: string) =>
    apiFetch<void>(`/api/teams/${teamId}`, { method: 'DELETE', body: { confirmationName } }),
}
