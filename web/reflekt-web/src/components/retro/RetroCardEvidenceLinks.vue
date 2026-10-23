<template>
  <div v-if="!blurred && card.evidenceLinks?.length" class="flex flex-col gap-1 mt-1">
    <a
      v-for="link in card.evidenceLinks"
      :key="link.id"
      :href="link.url"
      target="_blank"
      rel="noopener noreferrer"
      class="flex items-center gap-1.5 group cursor-pointer"
    >
      <template v-if="link.fetchStatus === 'Pending'">
        <span class="text-xs text-text-muted italic truncate max-w-[200px]">{{ link.url }}</span>
        <span class="text-xs text-text-muted animate-pulse">...</span>
      </template>
      <template v-else>
        <img
          v-if="faviconUrl(link)"
          :src="faviconUrl(link)"
          class="w-3 h-3 flex-shrink-0"
          :class="{ 'opacity-50': link.fetchStatus === 'Failed' }"
          @error="($event.target as HTMLImageElement).style.display = 'none'"
        />
        <span
          v-if="link.source === 'Jira' && link.externalKey"
          class="px-1.5 py-0.5 bg-info text-white text-[10px] rounded font-mono flex-shrink-0"
          >{{ link.externalKey }}</span
        >
        <span class="text-xs text-text-secondary truncate max-w-[180px] group-hover:text-info">{{
          link.title || link.url
        }}</span>
      </template>
      <button
        v-if="canManage"
        class="ml-auto text-text-muted hover:text-danger flex-shrink-0 leading-none cursor-pointer"
        title="Remove link"
        aria-label="Remove link"
        @click.prevent="actions.detachEvidenceLink(card.id, link.id)"
      >
        <XIcon :size="14" />
      </button>
    </a>
  </div>

  <!-- Add evidence link -->
  <div v-if="!blurred && canManage && !editing" class="mt-1">
    <button
      v-if="!addingLink"
      class="text-text-muted hover:text-info cursor-pointer"
      title="Add evidence link"
      aria-label="Add evidence link"
      @click="addingLink = true"
    >
      <LinkIcon :size="14" />
    </button>
    <div v-else class="flex gap-1 items-center">
      <input
        v-model="linkUrl"
        type="text"
        placeholder="Paste URL..."
        aria-label="Evidence link URL"
        class="flex-1 border border-border rounded-sm px-2 py-0.5 text-xs text-text-primary bg-surface focus:outline-none focus:ring-1 focus:ring-primary-500"
        @keydown.enter="submitLink"
        @keydown.escape="cancelLink"
      />
      <BaseButton size="sm" variant="primary" :disabled="!linkUrl.trim()" @click="submitLink"
        >Add</BaseButton
      >
      <button
        class="text-text-muted hover:text-text-secondary cursor-pointer"
        aria-label="Cancel adding link"
        @click="cancelLink"
      >
        <XIcon :size="14" />
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, inject } from 'vue'
import { Link as LinkIcon, X as XIcon } from 'lucide-vue-next'
import type { Card, EvidenceLink } from '@/types/card'
import BaseButton from '@/components/ui/BaseButton.vue'
import { boardActionsKey, missingBoardActions } from '@/composables/useRetroBoardActions'

const props = defineProps<{
  card: Card
  blurred: boolean
  editing: boolean
  canManage: boolean
}>()

const actions = inject(boardActionsKey) ?? missingBoardActions('RetroCardEvidenceLinks')

const addingLink = ref(false)
const linkUrl = ref('')

function submitLink() {
  if (!linkUrl.value.trim()) return
  actions.attachEvidenceLink(props.card.id, linkUrl.value.trim())
  linkUrl.value = ''
  addingLink.value = false
}

function cancelLink() {
  linkUrl.value = ''
  addingLink.value = false
}

function faviconUrl(link: EvidenceLink): string | undefined {
  if (link.iconUrl) {
    try {
      return new URL(link.iconUrl, link.url).href
    } catch {
      return link.iconUrl
    }
  }
  try {
    return `${new URL(link.url).origin}/favicon.ico`
  } catch {
    return undefined
  }
}
</script>
