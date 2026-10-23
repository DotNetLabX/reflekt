<template>
  <BaseCard padding="none" radius="sm" class="w-full max-w-lg">
    <div class="p-8 flex flex-col gap-4">
      <router-link
        :to="{ name: 'team-sprints', params: { teamId } }"
        class="inline-flex items-center gap-1 text-sm text-text-muted hover:text-text-secondary self-start"
      >
        <ArrowLeft :size="14" />
        Back to sprints
      </router-link>

      <div class="flex items-center justify-between">
        <h1 class="text-2xl font-bold text-text-primary">{{ sprint.name }}</h1>
        <BaseBadge :variant="SPRINT_STATE_BADGE[sprint.state]">{{ sprint.state }}</BaseBadge>
      </div>

      <div class="grid grid-cols-2 gap-2 text-sm text-text-secondary">
        <span class="font-medium">Phase</span><span>{{ sprint.currentPhase }}</span>
        <span class="font-medium">Votes per participant</span
        ><span>{{ sprint.votesPerParticipant }}</span> <span class="font-medium">Created</span
        ><span>{{ formatDateTime(sprint.createdAt) }}</span>
        <template v-if="sprint.closedAt">
          <span class="font-medium">Closed</span><span>{{ formatDateTime(sprint.closedAt) }}</span>
        </template>
      </div>

      <div class="flex flex-col gap-2 pt-2">
        <BaseButton v-if="sprint.state === 'Draft'" :disabled="busy" @click="$emit('start')">
          Start Sprint
        </BaseButton>
        <BaseButton
          v-if="sprint.state === 'Active'"
          :to="{ name: 'retro-room', params: { sprintId: sprint.id } }"
        >
          Open Retro Room
        </BaseButton>
        <BaseButton
          v-if="sprint.state === 'Active'"
          variant="secondary"
          @click="$emit('copy-invite')"
        >
          {{ copied ? 'Invite link copied!' : 'Copy invite link' }}
        </BaseButton>
        <BaseButton
          v-if="sprint.state === 'Closed'"
          :to="{ name: 'report', params: { teamId, sprintId: sprint.id } }"
        >
          View Report
        </BaseButton>
      </div>

      <!-- Participants -->
      <div
        v-if="sprint.state === 'Active' && participants.length > 0"
        class="flex flex-col gap-2 pt-2 border-t border-border"
      >
        <h2 class="text-sm font-semibold text-text-secondary uppercase tracking-wide">
          Participants
        </h2>
        <div class="flex flex-col gap-1">
          <div
            v-for="p in participants"
            :key="p.id"
            class="flex items-center justify-between px-3 py-2 bg-surface-raised rounded text-sm"
          >
            <span class="text-text-primary">{{ p.displayName }}</span>
            <BaseButton variant="danger" size="sm" :disabled="busy" @click="$emit('kick', p)">
              Kick
            </BaseButton>
          </div>
        </div>
      </div>

      <p v-if="errorMessage" class="text-danger text-sm">{{ errorMessage }}</p>
    </div>
  </BaseCard>
</template>

<script setup lang="ts">
import { ArrowLeft } from 'lucide-vue-next'
import BaseBadge from '@/components/ui/BaseBadge.vue'
import BaseButton from '@/components/ui/BaseButton.vue'
import BaseCard from '@/components/ui/BaseCard.vue'
import { SPRINT_STATE_BADGE } from '@/constants/badges'
import { formatDateTime } from '@/format/format'
import type { ParticipantName } from '@/types/participant'
import type { Sprint } from '@/types/sprint'

defineProps<{
  sprint: Pick<
    Sprint,
    'id' | 'name' | 'state' | 'currentPhase' | 'votesPerParticipant' | 'createdAt' | 'closedAt'
  >
  teamId: number
  participants: ParticipantName[]
  busy: boolean
  copied: boolean
  errorMessage: string
}>()

defineEmits<{
  start: []
  'copy-invite': []
  kick: [p: ParticipantName]
}>()
</script>
