<template>
  <div class="flex justify-end">
    <button
      :class="[
        'px-3 py-1.5 text-sm font-medium rounded-sm border transition-colors cursor-pointer',
        isCurrentParticipantReady
          ? 'bg-success-solid border-success-solid text-white hover:bg-success-hover'
          : 'bg-ready-idle border-ready-idle-border text-ready-idle-text hover:bg-ready-idle-hover',
      ]"
      @click="toggleReady(sprintId)"
    >
      {{ isCurrentParticipantReady ? 'Done!' : "I'm done" }}
    </button>
  </div>
</template>

<script setup lang="ts">
import { useReadiness } from '@/composables/useReadiness'

defineProps<{
  sprintId: number
}>()

const { isCurrentParticipantReady, toggleReady } = useReadiness()
</script>
