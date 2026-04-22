<template>
  <div
    class="relative rounded-[--radius-md] shadow-card p-3 flex flex-col gap-1 transition-shadow hover:shadow-card-hover"
    :class="cardBgClass"
  >
    <div v-if="blurred" class="relative select-none">
      <p class="text-sm text-gray-800 whitespace-pre-wrap blur-md" aria-hidden="true">Someone's note</p>
      <div class="absolute inset-0" />
    </div>
    <div v-else-if="!editing">
      <p class="text-sm text-gray-800 whitespace-pre-wrap">{{ card.text }}</p>
      <BaseBadge v-if="card.assigneeName" variant="info" class="mt-1">
        {{ card.assigneeName }}
      </BaseBadge>
    </div>

    <div v-else class="flex flex-col gap-1">
      <textarea
        v-model="editText"
        rows="2"
        maxlength="2000"
        class="border border-gray-300 rounded-[--radius-sm] px-2 py-1 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500 resize-none"
      />
      <BaseInput
        v-if="card.cardType === 'ActionItem'"
        v-model="editAssignee"
        :maxlength="50"
        placeholder="Assignee"
      />
      <div class="flex gap-2">
        <BaseButton
          size="sm"
          variant="primary"
          :disabled="!editText.trim() || (card.cardType === 'ActionItem' && !editAssignee.trim())"
          @click="commitEdit"
        >
          Save
        </BaseButton>
        <BaseButton size="sm" variant="secondary" @click="cancelEdit">
          Cancel
        </BaseButton>
      </div>
    </div>

    <!-- Evidence links -->
    <div v-if="!blurred && card.evidenceLinks && card.evidenceLinks.length > 0" class="flex flex-col gap-1 mt-1">
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
          <span class="text-xs text-gray-400 animate-pulse">...</span>
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
          v-if="canAttachLink ?? canEdit"
          class="ml-auto text-gray-300 hover:text-red-500 flex-shrink-0 leading-none cursor-pointer"
          title="Remove link"
          @click.prevent="$emit('detach-evidence-link', card.id, link.id)"
        >
          <XIcon :size="14" />
        </button>
      </a>
    </div>

    <!-- Attach evidence link UI -->
    <div v-if="!blurred && (canAttachLink ?? canEdit) && !editing" class="mt-1">
      <button
        v-if="!addingLink"
        class="text-gray-300 hover:text-blue-500 cursor-pointer"
        title="Add evidence link"
        @click="addingLink = true"
      >
        <LinkIcon :size="14" />
      </button>
      <div v-else class="flex gap-1 items-center">
        <input
          v-model="linkUrl"
          type="text"
          placeholder="Paste URL..."
          class="flex-1 border border-gray-300 rounded-[--radius-sm] px-2 py-0.5 text-xs focus:outline-none focus:ring-1 focus:ring-primary-500"
          @keydown.enter="submitLink"
          @keydown.escape="cancelLink"
        />
        <BaseButton size="sm" variant="primary" :disabled="!linkUrl.trim()" @click="submitLink">
          Add
        </BaseButton>
        <button class="text-gray-400 hover:text-gray-600 cursor-pointer" @click="cancelLink">
          <XIcon :size="14" />
        </button>
      </div>
    </div>

    <!-- Vote row — Note cards only -->
    <div v-if="!blurred && showVoting && !editing" class="flex items-center gap-2 mt-1">
      <button
        class="flex items-center gap-1 px-2 py-1 rounded-[--radius-sm] text-xs border transition-colors cursor-pointer"
        :class="
          (myVoteCount ?? 0) > 0
            ? 'bg-primary-50 border-primary-300 text-primary-700'
            : remainingBudget === 0 && (myVoteCount ?? 0) === 0
              ? 'border-gray-200 text-gray-300 cursor-not-allowed'
              : 'border-gray-200 text-gray-500 hover:border-primary-300 hover:text-primary-600'
        "
        :disabled="remainingBudget === 0 && (myVoteCount ?? 0) === 0"
        :title="remainingBudget === 0 && (myVoteCount ?? 0) === 0 ? 'No votes remaining' : 'Vote'"
        @click="$emit('vote', card)"
      >
        <ChevronUp :size="16" />
        <span class="transition-transform duration-150" :key="voteCount">{{ voteCount }}</span>
      </button>
      <button
        v-if="(myVoteCount ?? 0) > 0"
        class="text-gray-400 hover:text-red-500 px-1 cursor-pointer"
        title="Remove one vote"
        @click="$emit('retractVote', card)"
      >
        <Minus :size="14" />
      </button>
      <span v-if="(myVoteCount ?? 0) > 0" class="text-xs text-primary-600 font-medium">{{ myVoteCount }} mine</span>
    </div>

    <div v-if="!blurred && canEdit && !editing" class="flex gap-2 mt-1 justify-end">
      <BaseButton size="sm" variant="ghost" @click="startEdit">Edit</BaseButton>
      <BaseButton size="sm" variant="ghost" @click="$emit('delete', card)">Delete</BaseButton>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { Link as LinkIcon, X as XIcon, ChevronUp, Minus } from 'lucide-vue-next'
import type { Card, EvidenceLink } from '../stores/card'
import BaseButton from './ui/BaseButton.vue'
import BaseInput from './ui/BaseInput.vue'
import BaseBadge from './ui/BaseBadge.vue'

const props = defineProps<{
  card: Card
  canEdit: boolean
  blurred?: boolean
  canAttachLink?: boolean
  showVoting?: boolean
  voteCount?: number
  myVoteCount?: number
  remainingBudget?: number
  columnColor?: 'went-well' | 'didnt-go-well' | 'action-items'
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

const cardBgClasses: Record<string, string> = {
  'went-well': 'bg-went-well-light/30',
  'didnt-go-well': 'bg-didnt-go-well-light/30',
  'action-items': 'bg-action-items-light/30',
}
const cardBgClass = computed(() =>
  props.columnColor ? cardBgClasses[props.columnColor] ?? 'bg-white' : 'bg-white'
)

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
