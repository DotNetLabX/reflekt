import { apiFetch } from './client'
import type { VoteDistribution, VoteResponse } from '@/types/vote'

export const votesApi = {
  mine: (sprintId: number) =>
    apiFetch<VoteDistribution[]>(`/api/sprints/${sprintId}/votes/mine`, { guest: true }),
  cast: (sprintId: number, groupId: number) =>
    apiFetch<VoteResponse>(`/api/sprints/${sprintId}/card-groups/${groupId}/votes`, {
      method: 'POST',
      body: {},
      guest: true,
    }),
  retract: (sprintId: number, groupId: number) =>
    apiFetch<VoteResponse>(`/api/sprints/${sprintId}/card-groups/${groupId}/votes`, {
      method: 'DELETE',
      guest: true,
    }),
}
