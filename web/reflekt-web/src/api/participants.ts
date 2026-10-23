import { apiFetch } from './client'
import type { JoinSprintResponse, Participant } from '@/types/participant'

export const participantsApi = {
  join: (sprintId: number, displayName: string) =>
    apiFetch<JoinSprintResponse>(`/api/sprints/${sprintId}/join`, {
      method: 'POST',
      body: { displayName },
    }),
  joinWithGoogle: (sprintId: number) =>
    apiFetch<JoinSprintResponse>(`/api/sprints/${sprintId}/join/google`, {
      method: 'POST',
      body: {},
    }),
  me: (sprintId: number) =>
    apiFetch<Participant>(`/api/sprints/${sprintId}/participants/me`, { guest: true }),
  list: (sprintId: number) =>
    apiFetch<Participant[]>(`/api/sprints/${sprintId}/participants`, { guest: true }),
  kick: (sprintId: number, participantId: number) =>
    apiFetch<void>(`/api/sprints/${sprintId}/participants/${participantId}`, { method: 'DELETE' }),
}
