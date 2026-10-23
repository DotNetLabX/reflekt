<template>
  <div class="min-h-screen bg-background">
    <slot v-if="loading" name="loading">
      <div class="flex items-center justify-center min-h-screen">
        <slot name="loading-content">
          <p class="text-text-muted text-sm">Loading...</p>
        </slot>
      </div>
    </slot>

    <slot v-else-if="empty" name="empty">
      <div class="flex items-center justify-center min-h-screen">
        <slot name="empty-content">
          <EmptyState title="Nothing here yet." />
        </slot>
      </div>
    </slot>

    <div v-else class="w-full max-w-6xl mx-auto px-4 sm:px-8 lg:px-12 py-6 flex flex-col gap-5">
      <slot name="header" />
      <slot />
    </div>
  </div>
</template>

<script setup lang="ts">
import EmptyState from '@/components/ui/EmptyState.vue'

withDefaults(
  defineProps<{
    loading?: boolean
    empty?: boolean
  }>(),
  {
    loading: false,
    empty: false,
  },
)
</script>
