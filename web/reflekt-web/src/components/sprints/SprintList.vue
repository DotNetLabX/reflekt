<template>
  <BaseCard padding="none" radius="sm" class="w-full max-w-md">
    <div class="flex flex-col gap-4 p-8">
      <h2 class="text-xl font-bold text-text-primary">{{ teamName }}</h2>

      <EmptyState v-if="error" :title="error">
        <BaseButton variant="secondary" size="sm" @click="$emit('retry')">Retry</BaseButton>
      </EmptyState>
      <EmptyState
        v-else-if="!loading && sprints.length === 0"
        title="No sprints yet. Create your first sprint."
      />
      <ul v-else class="flex flex-col gap-2">
        <li v-for="sprint in sprints" :key="sprint.id">
          <button
            type="button"
            class="w-full flex items-center justify-between px-3 py-2 border border-border rounded hover:bg-surface-raised cursor-pointer text-left"
            @click="$emit('select', sprint.id)"
          >
            <span class="text-sm font-medium text-text-primary">{{ sprint.name }}</span>
            <span class="flex items-center gap-2 text-xs">
              <span class="text-text-muted">{{ sprint.currentPhase }}</span>
              <BaseBadge :variant="SPRINT_STATE_BADGE[sprint.state]">{{ sprint.state }}</BaseBadge>
            </span>
          </button>
        </li>
      </ul>

      <CreateSprintForm :team-id="teamId" />
    </div>
  </BaseCard>
</template>

<script setup lang="ts">
import CreateSprintForm from './CreateSprintForm.vue'
import EmptyState from '@/components/ui/EmptyState.vue'
import BaseBadge from '@/components/ui/BaseBadge.vue'
import BaseButton from '@/components/ui/BaseButton.vue'
import BaseCard from '@/components/ui/BaseCard.vue'
import { SPRINT_STATE_BADGE } from '@/constants/badges'
import type { Sprint } from '@/types/sprint'

defineProps<{
  teamId: number
  teamName: string
  sprints: Pick<Sprint, 'id' | 'name' | 'currentPhase' | 'state'>[]
  loading: boolean
  error: string | null
}>()

defineEmits<{
  select: [sprintId: number]
  retry: []
}>()
</script>
