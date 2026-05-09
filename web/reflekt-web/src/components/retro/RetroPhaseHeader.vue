<template>
  <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
    <h1 class="text-2xl font-bold text-gray-800">{{ sprintName }}</h1>
    <div class="flex items-center gap-3">
      <BaseBadge variant="info">
        Votes: {{ remainingBudget }}/{{ votesPerParticipant }}
      </BaseBadge>
    </div>
  </div>
</template>

<script setup lang="ts">
import BaseBadge from '../ui/BaseBadge.vue'

defineProps<{
  sprintName: string
  remainingBudget: number
  votesPerParticipant: number
}>()
</script>
