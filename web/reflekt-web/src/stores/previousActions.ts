import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { isHttpError } from '@/api/client'
import { previousActionsApi } from '@/api/previousActions'
import { createPendingValues } from './pendingValues'
import type { ReviewStatus } from '@/types/domain'
import type { PreviousActionItem, ReviewStatusChangedPayload } from '@/types/report'

export const usePreviousActionsStore = defineStore('previousActions', () => {
  const items = ref<PreviousActionItem[]>([])
  const loading = ref(false)
  const error = ref<string | null>(null)
  const previousSprintName = ref<string | null>(null)
  const previousSprintId = ref<number | null>(null)

  const totalCount = computed(() => items.value.length)
  const allReviewed = computed(
    () => items.value.length > 0 && items.value.every((i) => i.reviewStatus !== 'None'),
  )
  const summaryCounts = computed(() => ({
    done: items.value.filter((i) => i.reviewStatus === 'Done').length,
    carriedOver: items.value.filter((i) => i.reviewStatus === 'CarriedOver').length,
    dropped: items.value.filter((i) => i.reviewStatus === 'Dropped').length,
  }))

  let previousActionsRequest = 0

  const pendingStatuses = createPendingValues<number, ReviewStatus>((cardId, status) => {
    const item = items.value.find((i) => i.cardId === cardId)
    if (item) item.reviewStatus = status
  })

  async function fetchPreviousActions(sprintId: number): Promise<void> {
    const request = ++previousActionsRequest
    items.value = []
    previousSprintId.value = null
    previousSprintName.value = null
    error.value = null
    loading.value = true
    try {
      const data = await previousActionsApi.list(sprintId)
      if (request !== previousActionsRequest) return
      items.value = data
      pendingStatuses.settleAll()
      const first = data[0]
      previousSprintId.value = first?.previousSprintId ?? null
      previousSprintName.value = first?.previousSprintName ?? null
    } catch (e) {
      if (request === previousActionsRequest) {
        error.value = isHttpError(e) ? 'Failed to load previous actions' : "Can't reach the server"
      }
    } finally {
      if (request === previousActionsRequest) loading.value = false
    }
  }

  async function setReviewStatus(
    teamId: number,
    sprintId: number,
    cardId: number,
    status: ReviewStatus,
  ): Promise<void> {
    const item = items.value.find((i) => i.cardId === cardId)
    const change = item ? pendingStatuses.change(cardId, item.reviewStatus, status) : null
    try {
      await previousActionsApi.setReviewStatus(teamId, sprintId, cardId, status)
      if (change) pendingStatuses.confirm(cardId, change)
    } catch (e) {
      if (change) pendingStatuses.undo(cardId, change)
      throw e
    }
  }

  // The payload's reviewedCount and totalCount are ignored: the counts derive from `items`,
  // so they stay in sync without the server values.
  function applyReviewStatusChanged(payload: ReviewStatusChangedPayload): void {
    pendingStatuses.settle(payload.cardId)
    const item = items.value.find((i) => i.cardId === payload.cardId)
    if (item) {
      item.reviewStatus = payload.reviewStatus
    }
  }

  return {
    items,
    loading,
    error,
    previousSprintName,
    previousSprintId,
    totalCount,
    allReviewed,
    summaryCounts,
    fetchPreviousActions,
    setReviewStatus,
    applyReviewStatusChanged,
  }
})
