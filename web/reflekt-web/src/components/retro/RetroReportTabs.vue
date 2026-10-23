<template>
  <div class="flex items-center gap-2">
    <button
      v-for="tab in tabs"
      :key="tab.value"
      class="px-4 py-2 text-sm font-medium rounded-sm transition-colors cursor-pointer"
      :class="
        model === tab.value
          ? 'bg-primary-600 text-white'
          : 'bg-surface-raised text-text-secondary hover:bg-border'
      "
      @click="model = tab.value"
    >
      {{ tab.label }}
    </button>
  </div>
</template>

<script setup lang="ts">
const model = defineModel<'report' | 'board'>({ required: true })

const tabs = [
  { value: 'report', label: 'Report' },
  { value: 'board', label: 'Board' },
] as const
</script>
