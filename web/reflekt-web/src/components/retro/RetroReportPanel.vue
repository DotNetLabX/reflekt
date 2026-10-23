<template>
  <div class="flex-1 min-h-0 overflow-y-auto">
    <div v-if="reportStore.loading" class="text-text-muted text-sm text-center py-8">
      Loading report...
    </div>
    <div v-else-if="reportStore.error" class="flex flex-col items-center gap-3 py-8">
      <p class="text-danger text-sm text-center">{{ reportStore.error }}</p>
      <BaseButton variant="secondary" size="sm" @click="$emit('retry')">Retry</BaseButton>
    </div>
    <SprintReportPanel
      v-else-if="reportStore.report"
      :report="reportStore.report"
      title-tag="h2"
      @copy-markdown="$emit('copy-markdown')"
    />
  </div>
</template>

<script setup lang="ts">
import { useReportStore } from '@/stores/report'
import BaseButton from '@/components/ui/BaseButton.vue'
import SprintReportPanel from '@/components/reports/SprintReportPanel.vue'

defineEmits<{
  'copy-markdown': []
  retry: []
}>()

const reportStore = useReportStore()
</script>
