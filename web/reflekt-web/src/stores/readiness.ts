import { defineStore } from 'pinia'
import { ref } from 'vue'
import { invoke, isHubConnected } from '@/api/hub'
import { createPendingValues } from './pendingValues'
import type { ReadinessChangedPayload, ReadinessSnapshot } from '@/types/participant'

export const useReadinessStore = defineStore('readiness', () => {
  const readyParticipantIds = ref<number[]>([])
  const readyCount = ref(0)
  const totalCount = ref(0)

  function isReady(participantId: number): boolean {
    return readyParticipantIds.value.includes(participantId)
  }

  function setReady(participantId: number, ready: boolean): void {
    if (ready === isReady(participantId)) return
    if (ready) readyParticipantIds.value.push(participantId)
    else readyParticipantIds.value = readyParticipantIds.value.filter((id) => id !== participantId)
  }

  const pendingFlags = createPendingValues<number, boolean>(setReady)
  const pendingCount = createPendingValues<'readyCount', number>((_key, count) => {
    readyCount.value = count
  })

  function settleAll(): void {
    pendingFlags.settleAll()
    pendingCount.settleAll()
  }

  async function toggleReady(sprintId: number, participantId: number | undefined): Promise<void> {
    if (!isHubConnected()) return
    if (participantId === undefined) {
      await invoke('ToggleReady', sprintId)
      return
    }
    const wasReady = isReady(participantId)
    const count = readyCount.value
    const flag = pendingFlags.change(participantId, wasReady, !wasReady)
    const total = pendingCount.change('readyCount', count, Math.max(0, count + (wasReady ? -1 : 1)))
    try {
      await invoke('ToggleReady', sprintId)
      pendingFlags.confirm(participantId, flag)
      pendingCount.confirm('readyCount', total)
    } catch (e) {
      pendingFlags.undo(participantId, flag)
      pendingCount.undo('readyCount', total)
      throw e
    }
  }

  function setFromSnapshot(payload: ReadinessSnapshot) {
    settleAll()
    readyParticipantIds.value = payload.readyParticipantIds
    readyCount.value = payload.readyCount
    totalCount.value = payload.totalCount
  }

  function applyChange(payload: ReadinessChangedPayload) {
    pendingCount.settleAll()
    pendingFlags.settle(payload.participantId)
    if (payload.isReady) {
      if (!readyParticipantIds.value.includes(payload.participantId)) {
        readyParticipantIds.value.push(payload.participantId)
      }
    } else {
      readyParticipantIds.value = readyParticipantIds.value.filter(
        (id) => id !== payload.participantId,
      )
    }
    readyCount.value = payload.readyCount
    totalCount.value = payload.totalCount
  }

  function reset() {
    settleAll()
    readyParticipantIds.value = []
    readyCount.value = 0
    totalCount.value = 0
  }

  return {
    readyParticipantIds,
    readyCount,
    totalCount,
    isReady,
    toggleReady,
    setFromSnapshot,
    applyChange,
    reset,
  }
})
