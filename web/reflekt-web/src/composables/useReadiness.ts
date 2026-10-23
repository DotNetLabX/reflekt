import { computed } from 'vue'
import { useReadinessStore } from '@/stores/readiness'
import { useParticipantStore } from '@/stores/participant'

export function useReadiness() {
  const readinessStore = useReadinessStore()
  const participantStore = useParticipantStore()

  function toggleReady(sprintId: number): Promise<void> {
    return readinessStore.toggleReady(sprintId, participantStore.currentParticipant?.id)
  }

  const isCurrentParticipantReady = computed(() => {
    const currentId = participantStore.currentParticipant?.id
    if (currentId === undefined) return false
    return readinessStore.isReady(currentId)
  })

  const readyCount = computed(() => readinessStore.readyCount)
  const totalCount = computed(() => readinessStore.totalCount)

  const readyParticipants = computed(() =>
    participantStore.participants
      .filter((p) => readinessStore.readyParticipantIds.includes(p.id))
      .map((p) => p.displayName),
  )

  const notReadyParticipants = computed(() =>
    participantStore.participants
      .filter((p) => !readinessStore.readyParticipantIds.includes(p.id))
      .map((p) => p.displayName),
  )

  return {
    toggleReady,
    isCurrentParticipantReady,
    readyCount,
    totalCount,
    readyParticipants,
    notReadyParticipants,
  }
}
