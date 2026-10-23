<template>
  <div v-if="showVoting" class="flex items-center gap-1">
    <button
      class="flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium border transition-all"
      :class="voteBtnClass"
      :disabled="remainingBudget <= 0 && myVoteCount === 0"
      :aria-pressed="myVoteCount > 0"
      :aria-label="voteButtonLabel"
      @click="myVoteCount > 0 ? actions.retractGroupVote(group) : actions.castGroupVote(group)"
    >
      <Check v-if="myVoteCount > 0" :size="14" /><ChevronUp v-else :size="14" /><span>{{
        voteCount
      }}</span>
    </button>
  </div>
  <span v-else-if="voteCount > 0" class="text-xs font-medium text-text-muted"
    >{{ voteCount }} votes</span
  >
</template>

<script setup lang="ts">
import { computed, inject } from 'vue'
import { ChevronUp, Check } from 'lucide-vue-next'
import { boardActionsKey, missingBoardActions } from '@/composables/useRetroBoardActions'
import type { CardGroup } from '@/types/card'

const props = defineProps<{
  group: CardGroup
  showVoting: boolean
  voteCount: number
  myVoteCount: number
  remainingBudget: number
}>()

const actions = inject(boardActionsKey) ?? missingBoardActions('RetroGroupVotes')

const voteBtnClass = computed(() =>
  props.myVoteCount > 0
    ? 'bg-primary-600 text-white border-primary-600 hover:bg-primary-700 cursor-pointer'
    : props.remainingBudget <= 0
      ? 'border-border text-text-muted cursor-not-allowed'
      : 'border-border text-text-muted hover:border-primary-400 hover:text-primary-600 cursor-pointer',
)

const voteButtonLabel = computed(() =>
  props.myVoteCount > 0
    ? `${props.voteCount} votes, remove your vote`
    : `${props.voteCount} votes, vote for this group`,
)
</script>
