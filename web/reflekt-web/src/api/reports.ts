import { apiFetch } from './client'
import type { ReportResponse } from '@/types/report'

export const reportsApi = {
  forTeam: (teamId: number, sprintId: number) =>
    apiFetch<ReportResponse>(`/api/teams/${teamId}/sprints/${sprintId}/report`, { guest: true }),
  forParticipant: (sprintId: number) =>
    apiFetch<ReportResponse>(`/api/sprints/${sprintId}/report`, { guest: true }),
}
