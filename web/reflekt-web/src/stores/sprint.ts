import { defineStore } from 'pinia'
import { ref } from 'vue'
import { describeApiError, isHttpError } from '@/api/client'
import { sprintsApi } from '@/api/sprints'
import { useCardStore } from './card'
import { useCardGroupStore } from './cardGroup'
import type { SprintPhase } from '@/types/domain'
import type {
  AdvancePhaseCheckResponse,
  ColumnConfig,
  ColumnConfigPayload,
  ColumnConfigUpdatedPayload,
  CreateSprintPayload,
  Sprint,
  SprintSummary,
} from '@/types/sprint'

const SERVER_UNREACHABLE = "Can't reach the server"

function sprintLoadError(e: unknown): string {
  if (!isHttpError(e)) return SERVER_UNREACHABLE
  return describeApiError(e, {
    statusMessages: { 404: 'Sprint not found' },
    fallback: 'Failed to load sprint',
  })
}

export const useSprintStore = defineStore('sprint', () => {
  const cardStore = useCardStore()
  const cardGroupStore = useCardGroupStore()

  const sprints = ref<Sprint[]>([])
  const currentSprint = ref<Sprint | null>(null)
  const currentSummary = ref<SprintSummary | null>(null)
  const loaded = ref(false)
  const sprintsLoading = ref(false)
  const sprintsError = ref<string | null>(null)
  const currentSprintLoading = ref(false)
  const currentSprintError = ref<string | null>(null)
  const currentSummaryLoading = ref(false)
  const currentSummaryError = ref<string | null>(null)

  let sprintsRequest = 0
  let currentSprintRequest = 0
  let currentSummaryRequest = 0

  async function fetchTeamSprints(teamId: number) {
    const request = ++sprintsRequest
    sprints.value = []
    sprintsError.value = null
    sprintsLoading.value = true
    try {
      const result = await sprintsApi.listForTeam(teamId)
      if (request === sprintsRequest) sprints.value = result
    } catch (e) {
      if (request === sprintsRequest) {
        sprintsError.value = isHttpError(e) ? 'Failed to load sprints' : SERVER_UNREACHABLE
      }
    } finally {
      if (request === sprintsRequest) {
        sprintsLoading.value = false
        loaded.value = true
      }
    }
  }

  async function fetchSprint(teamId: number, sprintId: number) {
    const request = ++currentSprintRequest
    currentSprint.value = null
    currentSprintError.value = null
    currentSprintLoading.value = true
    try {
      const sprint = await sprintsApi.get(teamId, sprintId)
      if (request === currentSprintRequest) currentSprint.value = sprint
    } catch (e) {
      if (request === currentSprintRequest) currentSprintError.value = sprintLoadError(e)
    } finally {
      if (request === currentSprintRequest) currentSprintLoading.value = false
    }
  }

  async function fetchSprintSummary(sprintId: number) {
    const request = ++currentSummaryRequest
    currentSummary.value = null
    currentSummaryError.value = null
    currentSummaryLoading.value = true
    try {
      const summary = await sprintsApi.summary(sprintId)
      if (request === currentSummaryRequest) currentSummary.value = summary
    } catch (e) {
      if (request === currentSummaryRequest) currentSummaryError.value = sprintLoadError(e)
    } finally {
      if (request === currentSummaryRequest) currentSummaryLoading.value = false
    }
  }

  async function createSprint(teamId: number, payload: CreateSprintPayload): Promise<Sprint> {
    const sprint = await sprintsApi.create(teamId, payload)
    sprints.value.unshift(sprint)
    return sprint
  }

  async function updateColumns(sprintId: number, payload: ColumnConfigPayload): Promise<Sprint> {
    const sprint = await sprintsApi.updateColumns(sprintId, payload)
    replaceSprint(sprint)
    setSummaryColumns(sprint.id, sprint)
    return sprint
  }

  async function startSprint(teamId: number, sprintId: number) {
    replaceSprint(await sprintsApi.start(teamId, sprintId))
  }

  function advancePhaseCheck(teamId: number, sprintId: number): Promise<AdvancePhaseCheckResponse> {
    return sprintsApi.advanceCheck(teamId, sprintId)
  }

  async function advancePhase(teamId: number, sprintId: number) {
    replaceSprint(await sprintsApi.advance(teamId, sprintId))
  }

  async function closeSprint(teamId: number, sprintId: number) {
    replaceSprint(await sprintsApi.close(teamId, sprintId))
  }

  function setSummaryColumns(sprintId: number, columns: ColumnConfig) {
    const summary = currentSummary.value
    if (summary?.id !== sprintId) return
    summary.column1Name = columns.column1Name
    summary.column1Color = columns.column1Color
    summary.column2Name = columns.column2Name
    summary.column2Color = columns.column2Color
    summary.column3Name = columns.column3Name
    summary.column3Color = columns.column3Color
  }

  function applyColumnConfig(payload: ColumnConfigUpdatedPayload): void {
    setSummaryColumns(payload.sprintId, payload)
  }

  function applyPhaseChanged(sprintId: number, phase: SprintPhase): void {
    if (currentSummary.value) currentSummary.value.currentPhase = phase
    if (phase !== 'Writing') {
      cardStore.fetchCards(sprintId, { background: true })
      cardGroupStore.fetchGroups(sprintId, { background: true })
    }
  }

  function applySprintClosed(): void {
    if (currentSummary.value) currentSummary.value.state = 'Closed'
  }

  function replaceSprint(sprint: Sprint) {
    const idx = sprints.value.findIndex((s) => s.id === sprint.id)
    if (idx !== -1) sprints.value[idx] = sprint
    if (currentSprint.value?.id === sprint.id) currentSprint.value = sprint
  }

  return {
    sprints,
    currentSprint,
    currentSummary,
    loaded,
    sprintsLoading,
    sprintsError,
    currentSprintLoading,
    currentSprintError,
    currentSummaryLoading,
    currentSummaryError,
    fetchTeamSprints,
    fetchSprint,
    fetchSprintSummary,
    createSprint,
    updateColumns,
    startSprint,
    advancePhaseCheck,
    advancePhase,
    closeSprint,
    applyColumnConfig,
    applyPhaseChanged,
    applySprintClosed,
  }
})
