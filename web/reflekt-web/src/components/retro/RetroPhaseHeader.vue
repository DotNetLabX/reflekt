<template>
  <div class="flex flex-col gap-3 bg-surface rounded-md shadow-card px-4 py-3">
    <!-- Row 1: Sprint name | timer + badges + action button -->
    <div class="flex items-center justify-between gap-3">
      <h1 class="text-xl font-bold text-text-primary truncate">{{ sprintName }}</h1>
      <div class="flex items-center gap-2 flex-shrink-0">
        <!-- Timer display (Writing/Voting/Discussing phases only) -->
        <TimerDisplay v-if="isTimedPhase" :sprint-id="sprintId" />

        <!-- Readiness badge (Writing/Voting, >1 participant) -->
        <BaseTooltip
          v-if="showReadiness"
          variant="panel"
          panel-class="right-0 top-full mt-1 w-48"
          :disabled="!readinessTooltip"
        >
          <template #default="{ describedby }">
            <span
              class="inline-flex rounded-full focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500"
              :tabindex="readinessTooltip ? 0 : undefined"
              :aria-describedby="describedby"
            >
              <BaseBadge
                :variant="allDone ? 'success' : 'default'"
                :class="{ 'animate-attention': allDone }"
                >{{ readyCount }}/{{ totalCount }} done</BaseBadge
              >
            </span>
          </template>
          <template #content>
            <div v-if="readyParticipants.length > 0" class="mb-1">
              <p class="font-semibold text-badge-success-text">
                Done ({{ readyParticipants.length }})
              </p>
              <p v-for="name in readyParticipants" :key="name" class="truncate">{{ name }}</p>
            </div>
            <div v-if="notReadyParticipants.length > 0">
              <p class="font-semibold text-text-secondary">
                Not done ({{ notReadyParticipants.length }})
              </p>
              <p v-for="name in notReadyParticipants" :key="name" class="truncate">{{ name }}</p>
            </div>
          </template>
        </BaseTooltip>

        <!-- Votes remaining badge (Voting phase only) -->
        <BaseBadge v-if="showVotesBadge" variant="info">
          Votes: {{ voteStore.remainingBudget }}/{{ voteStore.votesPerParticipant }}
        </BaseBadge>

        <!-- SM controls -->
        <template v-if="isScrumMaster && !isClosed">
          <AdvancePhaseDialog v-if="!isReporting" :sprint-id="sprintId" />
          <CloseSprintDialog v-else :sprint-id="sprintId" />
        </template>
      </div>
    </div>

    <!-- Row 2: Phase stepper -->
    <PhaseIndicator :current-phase="currentPhase" />

    <!-- Row 3: Participant list -->
    <RetroParticipantList :participants="participantStore.participants" />
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useParticipantStore } from '@/stores/participant'
import { useSprintStore } from '@/stores/sprint'
import { useVoteStore } from '@/stores/vote'
import { useReadiness } from '@/composables/useReadiness'
import { useRetroPhase } from '@/composables/useRetroPhase'
import BaseBadge from '@/components/ui/BaseBadge.vue'
import BaseTooltip from '@/components/ui/BaseTooltip.vue'
import AdvancePhaseDialog from './AdvancePhaseDialog.vue'
import CloseSprintDialog from './CloseSprintDialog.vue'
import PhaseIndicator from './PhaseIndicator.vue'
import RetroParticipantList from './RetroParticipantList.vue'
import TimerDisplay from './TimerDisplay.vue'

const props = defineProps<{
  sprintId: number
}>()

const participantStore = useParticipantStore()
const sprintStore = useSprintStore()
const voteStore = useVoteStore()
const {
  currentPhase,
  isTimedPhase,
  isReporting,
  isClosed,
  isScrumMaster,
  showReadiness,
  showVotesBadge,
} = useRetroPhase()
const { readyCount, totalCount, readyParticipants, notReadyParticipants } = useReadiness()

const sprintName = computed(() => sprintStore.currentSummary?.name || `Sprint #${props.sprintId}`)
const readinessTooltip = computed(() => isScrumMaster.value && totalCount.value > 0)
const allDone = computed(() => totalCount.value > 0 && readyCount.value >= totalCount.value)
</script>
