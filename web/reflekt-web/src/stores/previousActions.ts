import { defineStore } from 'pinia'
import { useGuestStore } from './guest'

export interface PreviousActionItem {
  cardId: number
  text: string
  assigneeName: string | null
  reviewStatus: string
  previousSprintId: number
  previousSprintName: string
}

let previousActionsRequest = 0

export const usePreviousActionsStore = defineStore('previousActions', {
  state: () => ({
    items: [] as PreviousActionItem[],
    loading: false,
    error: null as string | null,
    previousSprintName: null as string | null,
    previousSprintId: null as number | null,
  }),
  getters: {
    totalCount: (state): number => state.items.length,
    reviewedCount: (state): number =>
      state.items.filter((i) => i.reviewStatus !== 'None').length,
    unreviewedCount: (state): number =>
      state.items.filter((i) => i.reviewStatus === 'None').length,
    allReviewed: (state): boolean =>
      state.items.length > 0 && state.items.every((i) => i.reviewStatus !== 'None'),
    summaryCounts: (state) => ({
      done: state.items.filter((i) => i.reviewStatus === 'Done').length,
      carriedOver: state.items.filter((i) => i.reviewStatus === 'CarriedOver').length,
      dropped: state.items.filter((i) => i.reviewStatus === 'Dropped').length,
    }),
  },
  actions: {
    async fetchPreviousActions(sprintId: number): Promise<void> {
      const request = ++previousActionsRequest
      this.items = []
      this.previousSprintId = null
      this.previousSprintName = null
      this.error = null
      this.loading = true
      try {
        const guestStore = useGuestStore()
        const headers: Record<string, string> = {}
        if (guestStore.token) headers['X-Guest-Token'] = guestStore.token
        const res = await fetch(`/api/sprints/${sprintId}/previous-actions`, {
          headers,
          credentials: 'include',
        })
        if (!res.ok) {
          if (request === previousActionsRequest) this.error = 'Failed to load previous actions'
          return
        }
        const data = (await res.json()) as PreviousActionItem[]
        if (request !== previousActionsRequest) return
        this.items = data
        const first = data[0]
        this.previousSprintId = first?.previousSprintId ?? null
        this.previousSprintName = first?.previousSprintName ?? null
      } catch {
        if (request === previousActionsRequest) this.error = "Can't reach the server"
      } finally {
        if (request === previousActionsRequest) this.loading = false
      }
    },

    applyReviewStatusChanged(payload: {
      cardId: number
      reviewStatus: string
      // reviewedCount and totalCount are provided by the server but intentionally unused here:
      // allReviewed, reviewedCount, and totalCount are derived from local `items` state via
      // computed getters, so they stay in sync automatically without needing the server values.
      reviewedCount: number
      totalCount: number
    }): void {
      const item = this.items.find((i) => i.cardId === payload.cardId)
      if (item) {
        item.reviewStatus = payload.reviewStatus
      }
    },
  },
})
