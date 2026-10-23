<template>
  <div
    class="bg-background flex"
    :class="
      inRoom
        ? 'h-[calc(100vh-3.5rem)] overflow-hidden flex-col'
        : 'min-h-screen items-center justify-center'
    "
  >
    <RetroKickedNotice v-if="kicked" />
    <div v-else-if="loading" class="flex flex-col items-center gap-4">
      <LoadingSpinner size="lg">Joining retro...</LoadingSpinner>
    </div>
    <EmptyState v-else-if="roomError" :title="roomError">
      <BaseButton variant="secondary" size="sm" @click="retryEntry">Retry</BaseButton>
    </EmptyState>
    <RetroJoinForm
      v-else-if="!joined"
      :sprint-name="sprintName || `Sprint #${sprintId}`"
      :sprint-id="sprintId"
      :participants="summaryParticipants"
      :error-message="errorMessage"
      :busy="busy"
      :show-google-sign-in="showGoogleOption"
      @join="handleJoin"
    />
    <div v-else class="w-full flex-1 min-h-0 px-4 sm:px-8 lg:px-12 py-6 flex flex-col gap-5">
      <RetroPhaseHeader :sprint-id="sprintId" />
      <RetroRoomBanners :closed="isClosed" :refresh-failed="!!refreshError" @retry="retryRefresh" />
      <RetroReportTabs v-if="isReporting" v-model="reportingView" />
      <RetroReportPanel
        v-if="isReporting && reportingView === 'report'"
        @copy-markdown="handleCopyMarkdown"
        @retry="reportStore.fetchParticipantReport(sprintId)"
      />
      <RetroBoardPanel v-if="!isReporting || reportingView === 'board'" :sprint-id="sprintId" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { useReportStore } from '@/stores/report'
import { useToast } from '@/composables/useToast'
import { useRetroRoom } from '@/composables/useRetroRoom'
import { useRetroPhase } from '@/composables/useRetroPhase'
import { useCopyReport } from '@/composables/useCopyReport'
import { useChimeUnlock } from '@/composables/useTimer'
import LoadingSpinner from '@/components/ui/LoadingSpinner.vue'
import BaseButton from '@/components/ui/BaseButton.vue'
import EmptyState from '@/components/ui/EmptyState.vue'
import RetroJoinForm from '@/components/retro/RetroJoinForm.vue'
import RetroKickedNotice from '@/components/retro/RetroKickedNotice.vue'
import RetroPhaseHeader from '@/components/retro/RetroPhaseHeader.vue'
import RetroRoomBanners from '@/components/retro/RetroRoomBanners.vue'
import RetroReportTabs from '@/components/retro/RetroReportTabs.vue'
import RetroReportPanel from '@/components/retro/RetroReportPanel.vue'
import RetroBoardPanel from '@/components/retro/RetroBoardPanel.vue'

const reportStore = useReportStore()
const { addToast } = useToast()
const { copyReport } = useCopyReport()
const sprintId = Number(useRoute().params.sprintId)
const reportingView = ref<'report' | 'board'>('report')

const {
  kicked,
  busy,
  errorMessage,
  joined,
  loading,
  inRoom,
  roomError,
  refreshError,
  sprintName,
  summaryParticipants,
  handleJoin,
  retryEntry,
  retryRefresh,
  showGoogleOption,
} = useRetroRoom(sprintId)
const { isReporting, isClosed } = useRetroPhase()
useChimeUnlock()

watch(isReporting, (reporting) => {
  if (!reporting || !joined.value) return
  reportingView.value = 'report'
  reportStore.fetchParticipantReport(sprintId)
})

watch(isClosed, (closed) => {
  if (closed && joined.value) addToast('This retro has been closed by the Scrum Master.', 'info')
})

async function handleCopyMarkdown() {
  if (await copyReport()) addToast('Copied to clipboard', 'success')
}
</script>
