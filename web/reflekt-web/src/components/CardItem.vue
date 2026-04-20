<template>
  <div class="bg-white border rounded p-3 flex flex-col gap-1 shadow-sm">
    <div v-if="!editing">
      <p class="text-sm text-gray-800 whitespace-pre-wrap">{{ card.text }}</p>
      <span
        v-if="card.assigneeName"
        class="inline-block mt-1 px-2 py-0.5 bg-blue-100 text-blue-700 text-xs rounded-full"
      >
        {{ card.assigneeName }}
      </span>
    </div>

    <div v-else class="flex flex-col gap-1">
      <textarea
        v-model="editText"
        rows="2"
        maxlength="2000"
        class="border rounded px-2 py-1 text-sm focus:outline-none focus:ring-2 focus:ring-blue-400 resize-none"
      />
      <input
        v-if="card.cardType === 'ActionItem'"
        v-model="editAssignee"
        type="text"
        maxlength="50"
        placeholder="Assignee"
        class="border rounded px-2 py-1 text-sm focus:outline-none focus:ring-2 focus:ring-blue-400"
      />
      <div class="flex gap-2">
        <button
          class="px-2 py-1 bg-blue-600 text-white rounded text-xs hover:bg-blue-700 disabled:opacity-50"
          :disabled="!editText.trim() || (card.cardType === 'ActionItem' && !editAssignee.trim())"
          @click="commitEdit"
        >
          Save
        </button>
        <button class="px-2 py-1 border rounded text-xs hover:bg-gray-50" @click="cancelEdit">
          Cancel
        </button>
      </div>
    </div>

    <!-- Evidence links -->
    <div v-if="card.evidenceLinks && card.evidenceLinks.length > 0" class="flex flex-col gap-1 mt-1">
      <a
        v-for="link in card.evidenceLinks"
        :key="link.id"
        :href="link.url"
        target="_blank"
        rel="noopener noreferrer"
        class="flex items-center gap-1.5 group cursor-pointer"
      >
        <!-- Pending -->
        <template v-if="link.fetchStatus === 'Pending'">
          <span class="text-xs text-gray-400 italic truncate max-w-[200px]">{{ link.url }}</span>
          <span class="text-xs text-gray-400 animate-pulse">…</span>
        </template>

        <!-- Failed -->
        <template v-else-if="link.fetchStatus === 'Failed'">
          <img
            v-if="faviconUrl(link)"
            :src="faviconUrl(link)"
            class="w-3 h-3 flex-shrink-0 opacity-50"
            @error="($event.target as HTMLImageElement).style.display = 'none'"
          />
          <span class="text-xs text-gray-400 italic truncate max-w-[200px]">{{ link.url }}</span>
        </template>

        <!-- Ok -->
        <template v-else>
          <img
            v-if="faviconUrl(link)"
            :src="faviconUrl(link)"
            class="w-3 h-3 flex-shrink-0"
            @error="($event.target as HTMLImageElement).style.display = 'none'"
          />
          <!-- Jira chip -->
          <span
            v-if="link.source === 'Jira' && link.externalKey"
            class="px-1.5 py-0.5 bg-blue-600 text-white text-[10px] rounded font-mono flex-shrink-0"
          >
            {{ link.externalKey }}
          </span>
          <span class="text-xs text-gray-600 truncate max-w-[180px] group-hover:text-blue-600">
            {{ link.title || link.url }}
          </span>
        </template>

        <button
          v-if="canEdit"
          class="ml-auto text-gray-300 hover:text-red-500 text-xs flex-shrink-0 leading-none"
          title="Remove link"
          @click.prevent="$emit('detach-evidence-link', card.id, link.id)"
        >
          ×
        </button>
      </a>
    </div>

    <!-- Attach evidence link UI -->
    <div v-if="canEdit && !editing" class="mt-1">
      <button
        v-if="!addingLink"
        class="text-gray-300 hover:text-blue-500 text-xs"
        title="Add evidence link"
        @click="addingLink = true"
      >
        🔗
      </button>
      <div v-else class="flex gap-1 items-center">
        <input
          v-model="linkUrl"
          type="text"
          placeholder="Paste URL…"
          class="flex-1 border rounded px-2 py-0.5 text-xs focus:outline-none focus:ring-1 focus:ring-blue-400"
          @keydown.enter="submitLink"
          @keydown.escape="cancelLink"
        />
        <button
          class="px-2 py-0.5 bg-blue-600 text-white rounded text-xs hover:bg-blue-700 disabled:opacity-50"
          :disabled="!linkUrl.trim()"
          @click="submitLink"
        >
          Add
        </button>
        <button class="text-gray-400 hover:text-gray-600 text-xs" @click="cancelLink">✕</button>
      </div>
    </div>

    <!-- Vote row — Note cards only -->
    <div v-if="showVoting && !editing" class="flex items-center gap-2 mt-1">
      <button
        class="flex items-center gap-1 px-2 py-1 rounded text-xs border transition-colors"
        :class="
          (myVoteCount ?? 0) > 0
            ? 'bg-blue-50 border-blue-300 text-blue-700'
            : 'border-gray-200 text-gray-500 hover:border-blue-300 hover:text-blue-600'
        "
        :disabled="remainingBudget === 0 && (myVoteCount ?? 0) === 0"
        :title="remainingBudget === 0 && (myVoteCount ?? 0) === 0 ? 'No votes remaining' : 'Vote'"
        @click="$emit('vote', card)"
      >
        ▲ {{ voteCount }}
      </button>
      <button
        v-if="(myVoteCount ?? 0) > 0"
        class="text-xs text-gray-400 hover:text-red-500 px-1"
        title="Remove one vote"
        @click="$emit('retractVote', card)"
      >
        −
      </button>
      <span v-if="(myVoteCount ?? 0) > 0" class="text-xs text-blue-500">{{ myVoteCount }} mine</span>
    </div>

    <div v-if="canEdit && !editing" class="flex gap-2 mt-1 justify-end">
      <button class="text-gray-400 hover:text-blue-600 text-xs" @click="startEdit">Edit</button>
      <button class="text-gray-400 hover:text-red-600 text-xs" @click="$emit('delete', card)">Delete</button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import type { Card, EvidenceLink } from '../stores/card'

const props = defineProps<{
  card: Card
  canEdit: boolean
  showVoting?: boolean
  voteCount?: number
  myVoteCount?: number
  remainingBudget?: number
}>()

const emit = defineEmits<{
  edit: [card: Card, text: string, assigneeName: string | null]
  delete: [card: Card]
  vote: [card: Card]
  retractVote: [card: Card]
  'attach-evidence-link': [cardId: number, url: string]
  'detach-evidence-link': [cardId: number, evidenceLinkId: number]
}>()

const editing = ref(false)
const editText = ref('')
const editAssignee = ref('')
const addingLink = ref(false)
const linkUrl = ref('')

function startEdit() {
  editText.value = props.card.text
  editAssignee.value = props.card.assigneeName ?? ''
  editing.value = true
}

function cancelEdit() {
  editing.value = false
}

function commitEdit() {
  if (!editText.value.trim()) return
  const assigneeName = props.card.cardType === 'ActionItem' ? editAssignee.value.trim() || null : null
  emit('edit', props.card, editText.value.trim(), assigneeName)
  editing.value = false
}

function submitLink() {
  const url = linkUrl.value.trim()
  if (!url) return
  emit('attach-evidence-link', props.card.id, url)
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
    const { origin } = new URL(link.url)
    return `${origin}/favicon.ico`
  } catch {
    return undefined
  }
}
</script>
