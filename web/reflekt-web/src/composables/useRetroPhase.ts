import { computed } from 'vue'
import { DEFAULT_TIMERS } from '@/constants/columns'
import { useParticipantStore } from '@/stores/participant'
import { useReadinessStore } from '@/stores/readiness'
import { useSprintStore } from '@/stores/sprint'
import { PHASES, type SprintPhase } from '@/types/domain'
import type { AdvancePhaseCheckResponse } from '@/types/sprint'

function plural(count: number): string {
  return count === 1 ? '' : 's'
}

export function useRetroPhase() {
  const sprintStore = useSprintStore()
  const participantStore = useParticipantStore()
  const readinessStore = useReadinessStore()

  const summary = computed(() => sprintStore.currentSummary)
  const currentPhase = computed<SprintPhase | null>(() => summary.value?.currentPhase ?? null)
  const phaseInfo = computed(() => PHASES.find((p) => p.name === currentPhase.value) ?? null)
  const nextPhase = computed(() => phaseInfo.value?.next ?? null)

  const isWriting = computed(() => currentPhase.value === 'Writing')
  const isVoting = computed(() => currentPhase.value === 'Voting')
  const isDiscussing = computed(() => currentPhase.value === 'Discussing')
  const isReporting = computed(() => currentPhase.value === 'Reporting')
  const isTimedPhase = computed(() => phaseInfo.value?.timed ?? false)
  const showGroups = computed(() => currentPhase.value !== null && !isWriting.value)

  const isClosed = computed(() => summary.value?.state === 'Closed')
  const isScrumMaster = computed(() => summary.value?.isScrumMaster ?? false)
  const showReadiness = computed(
    () => (phaseInfo.value?.readiness ?? false) && participantStore.participants.length > 1,
  )
  const showVotesBadge = isVoting

  // The sprint's own timer setting for the current phase, in minutes; 0 outside timed phases.
  const timerDefaultMinutes = computed(() => {
    const field = phaseInfo.value?.timerDefault
    if (!field) return 0
    return summary.value?.[field] ?? DEFAULT_TIMERS[field]
  })

  // Only the Writing check decides `warning`; every other phase keeps the previous prompt's.
  function advancePrompt(check: AdvancePhaseCheckResponse, warning: boolean) {
    const next = nextPhase.value ?? ''
    const plain = { title: `Advance to ${next}`, message: '', error: '', warning }
    switch (check.currentPhase) {
      case 'Writing': {
        const unreviewed = check.unreviewedActionCount ?? 0
        const total = check.totalPreviousActionCount ?? 0
        if (unreviewed > 0 && total > 0)
          return {
            ...plain,
            message: `${unreviewed} of ${total} previous action item${plural(total)} haven't been reviewed yet. Advance anyway?`,
            warning: true,
          }
        return { ...plain, message: `Advance to ${next}? This cannot be undone.`, warning: false }
      }
      case 'Voting': {
        const n = check.nonVotingParticipantCount ?? 0
        const total = readinessStore.totalCount
        const notDone = total - readinessStore.readyCount
        if (n > 0)
          return {
            ...plain,
            message: `${n} participant${plural(n)} haven't voted yet. Advance to Discussing anyway?`,
          }
        if (notDone > 0)
          return {
            ...plain,
            message: `All participants have voted, but ${notDone} of ${total} haven't marked as done. Advance to Discussing anyway?`,
          }
        return {
          ...plain,
          message: `All participants have voted and are done. Advance to Discussing?`,
        }
      }
      case 'Discussing': {
        const discussed = check.discussedGroupCount ?? 0
        if (discussed < 2)
          return {
            ...plain,
            title: 'Cannot Advance',
            error: `At least 2 groups must be discussed before advancing to Reporting. Currently discussed: ${discussed}.`,
          }
        return {
          ...plain,
          message: `Advance to Reporting? ${discussed} group${plural(discussed)} have been discussed.`,
        }
      }
      default:
        return { ...plain, message: `Advance to ${next}? This cannot be undone.` }
    }
  }

  return {
    currentPhase,
    nextPhase,
    isWriting,
    isVoting,
    isDiscussing,
    isReporting,
    isTimedPhase,
    showGroups,
    isClosed,
    isScrumMaster,
    showReadiness,
    showVotesBadge,
    timerDefaultMinutes,
    advancePrompt,
  }
}
