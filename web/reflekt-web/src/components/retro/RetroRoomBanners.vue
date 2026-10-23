<template>
  <div
    v-if="closed"
    class="bg-badge-warning-bg border border-badge-warning-text rounded-sm px-4 py-2 text-sm text-badge-warning-text"
  >
    This retro has been closed. The board is now read-only.
  </div>

  <div
    v-if="refreshFailed"
    role="status"
    class="flex items-center justify-between gap-3 bg-badge-danger-bg border border-badge-danger-text rounded-sm px-4 py-2 text-sm text-badge-danger-text"
  >
    <span>The board could not be refreshed.</span>
    <BaseButton variant="secondary" size="sm" @click="$emit('retry')">Retry</BaseButton>
  </div>
</template>

<script setup lang="ts">
import BaseButton from '@/components/ui/BaseButton.vue'

defineProps<{
  closed: boolean
  refreshFailed: boolean
}>()

defineEmits<{
  retry: []
}>()
</script>
