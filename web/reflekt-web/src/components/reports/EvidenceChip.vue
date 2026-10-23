<template>
  <a
    :href="link.url"
    target="_blank"
    rel="noopener"
    class="inline-block px-2 py-0.5 rounded text-xs hover:opacity-80"
    :class="`bg-col-${color}-light text-col-${color}`"
  >
    {{ link.title || truncateUrl(link.url) }}
  </a>
</template>

<script setup lang="ts">
import { truncateUrl } from '@/format/format'
import type { ReportEvidenceLinkEntry } from '@/types/report'
import type { ColumnColor } from '@/types/domain'

defineProps<{
  link: ReportEvidenceLinkEntry
  color: ColumnColor
}>()
</script>
