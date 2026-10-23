import { apiFetch } from './client'
import type { ReviewStatus } from '@/types/domain'
import type { PreviousActionItem } from '@/types/report'

export const previousActionsApi = {
  list: (sprintId: number) =>
    apiFetch<PreviousActionItem[]>(`/api/sprints/${sprintId}/previous-actions`, { guest: true }),
  setReviewStatus: (teamId: number, sprintId: number, cardId: number, reviewStatus: ReviewStatus) =>
    apiFetch<void>(`/api/teams/${teamId}/sprints/${sprintId}/previous-actions/${cardId}`, {
      method: 'PATCH',
      body: { reviewStatus },
      guest: true,
    }),
}
