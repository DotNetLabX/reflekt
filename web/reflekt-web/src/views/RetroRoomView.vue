<template>
  <div :class="['bg-background flex', joined && !kicked && !loading ? 'h-[calc(100vh-3.5rem)] overflow-hidden flex-col' : 'min-h-screen items-center justify-center']">
    <div v-if="kicked" class="w-full max-w-sm p-8 bg-surface rounded-[--radius-md] shadow-card flex flex-col gap-4 items-center">
      <h1 class="text-2xl font-bold text-danger">You were removed</h1>
      <p class="text-sm text-gray-600 text-center">The Scrum Master removed you from this retro. You'll need a new invite to rejoin.</p>
    </div>

    <div v-else-if="loading" class="flex flex-col items-center gap-4">
      <LoadingSpinner size="lg">Joining retro...</LoadingSpinner>
    </div>

    <RetroJoinForm
      v-else-if="!joined"
      :sprint-name="sprintName || `Sprint #${sprintId}`"
      :participants="summaryParticipants"
      :error-message="errorMessage"
      :busy="busy"
      @join="handleJoin"
    />

    <div v-else class="w-full flex-1 min-h-0 px-4 sm:px-8 lg:px-12 py-6 flex flex-col gap-5">
      <RetroPhaseHeader
        :sprint-name="sprintName || `Sprint #${sprintId}`"
        :remaining-budget="voteStore.remainingBudget"
        :votes-per-participant="voteStore.votesPerParticipant"
      />
      <PhaseIndicator :current-phase="currentPhase" />
      <RetroParticipantList :participants="participants" />

      <div v-if="isReportingPhase" class="flex items-center gap-2">
        <button class="px-4 py-2 text-sm font-medium rounded-[--radius-sm] transition-colors cursor-pointer" :class="reportingView === 'report' ? 'bg-primary-600 text-white' : 'bg-gray-200 text-gray-700 hover:bg-gray-300'" @click="reportingView = 'report'">Report</button>
        <button class="px-4 py-2 text-sm font-medium rounded-[--radius-sm] transition-colors cursor-pointer" :class="reportingView === 'board' ? 'bg-primary-600 text-white' : 'bg-gray-200 text-gray-700 hover:bg-gray-300'" @click="reportingView = 'board'">Board</button>
      </div>

      <RetroReportPanel v-if="isReportingPhase && reportingView === 'report'" @copy-markdown="handleCopyMarkdown" />
      <RetroBoardPanel v-if="!isReportingPhase || reportingView === 'board'" :sprint-id="sprintId" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { useRoute } from 'vue-router'
import { useParticipantStore } from '../stores/participant'
import { useVoteStore } from '../stores/vote'
import { useToast } from '../composables/useToast'
import { useRetroRoom } from '../composables/useRetroRoom'
import PhaseIndicator from '../components/PhaseIndicator.vue'
import LoadingSpinner from '../components/ui/LoadingSpinner.vue'
import RetroJoinForm from '../components/retro/RetroJoinForm.vue'
import RetroPhaseHeader from '../components/retro/RetroPhaseHeader.vue'
import RetroParticipantList from '../components/retro/RetroParticipantList.vue'
import RetroReportPanel from '../components/retro/RetroReportPanel.vue'
import RetroBoardPanel from '../components/retro/RetroBoardPanel.vue'

const route = useRoute()
const participantStore = useParticipantStore()
const voteStore = useVoteStore()
const { addToast } = useToast()

const sprintId = Number(route.params.sprintId)
const reportingView = ref<'report' | 'board'>('report')

const {
  kicked, busy, errorMessage, joined, loading,
  sprintName, currentPhase, summaryParticipants, isReportingPhase,
  handleJoin, reportStore,
} = useRetroRoom(sprintId)

const participants = computed(() => participantStore.participants)

watch(isReportingPhase, (reporting) => {
  if (reporting && joined.value) { reportingView.value = 'report'; reportStore.fetchParticipantReport(sprintId) }
})

async function handleCopyMarkdown() {
  const ok = await reportStore.copyAsMarkdown()
  if (ok) addToast('Copied to clipboard', 'success')
}
</script>
