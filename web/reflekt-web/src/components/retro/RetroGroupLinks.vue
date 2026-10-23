<template>
  <div class="flex flex-col gap-1 mt-1">
    <a
      v-for="link in links"
      :key="link.id"
      :href="link.url"
      target="_blank"
      rel="noopener noreferrer"
      class="text-xs text-text-secondary truncate max-w-[200px] hover:text-info"
      >{{ link.title || link.url }}</a
    >
  </div>
</template>

<script setup lang="ts">
import type { CardGroupCard } from '@/types/card'

defineProps<{ links: CardGroupCard['evidenceLinks'] }>()
</script>
