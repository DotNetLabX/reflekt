import { apiFetch } from './client'
import type {
  AdvancePhaseCheckResponse,
  ColumnConfigPayload,
  CreateSprintPayload,
  Sprint,
  SprintSummary,
} from '@/types/sprint'

export const sprintsApi = {
  listForTeam: (teamId: number) => apiFetch<Sprint[]>(`/api/teams/${teamId}/sprints`),
  get: (teamId: number, sprintId: number) =>
    apiFetch<Sprint>(`/api/teams/${teamId}/sprints/${sprintId}`),
  summary: (sprintId: number) => apiFetch<SprintSummary>(`/api/sprints/${sprintId}/summary`),
  create: (teamId: number, payload: CreateSprintPayload) =>
    apiFetch<Sprint>(`/api/teams/${teamId}/sprints`, { method: 'POST', body: payload }),
  updateColumns: (sprintId: number, payload: ColumnConfigPayload) =>
    apiFetch<Sprint>(`/api/sprints/${sprintId}/columns`, { method: 'PUT', body: payload }),
  start: (teamId: number, sprintId: number) =>
    apiFetch<Sprint>(`/api/teams/${teamId}/sprints/${sprintId}/start`, {
      method: 'POST',
      body: {},
    }),
  advanceCheck: (teamId: number, sprintId: number) =>
    apiFetch<AdvancePhaseCheckResponse>(
      `/api/teams/${teamId}/sprints/${sprintId}/advance-phase/check`,
      { method: 'POST', body: {} },
    ),
  advance: (teamId: number, sprintId: number) =>
    apiFetch<Sprint>(`/api/teams/${teamId}/sprints/${sprintId}/advance-phase`, {
      method: 'POST',
      body: {},
    }),
  close: (teamId: number, sprintId: number) =>
    apiFetch<Sprint>(`/api/teams/${teamId}/sprints/${sprintId}/close`, {
      method: 'POST',
      body: {},
    }),
}
