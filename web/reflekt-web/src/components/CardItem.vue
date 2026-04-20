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

    <!-- Vote row — Note cards only -->
    <div v-if="showVoting && !editing" class="flex items-center gap-2 mt-1">
      <button
        class="flex items-center gap-1 px-2 py-1 rounded text-xs border transition-colors"
        :class="
          myVoteCount > 0
            ? 'bg-blue-50 border-blue-300 text-blue-700'
            : 'border-gray-200 text-gray-500 hover:border-blue-300 hover:text-blue-600'
        "
        :disabled="remainingBudget === 0 && myVoteCount === 0"
        :title="remainingBudget === 0 && myVoteCount === 0 ? 'No votes remaining' : 'Vote'"
        @click="$emit('vote', card)"
      >
        ▲ {{ voteCount }}
      </button>
      <button
        v-if="myVoteCount > 0"
        class="text-xs text-gray-400 hover:text-red-500 px-1"
        title="Remove one vote"
        @click="$emit('retractVote', card)"
      >
        −
      </button>
      <span v-if="myVoteCount > 0" class="text-xs text-blue-500">{{ myVoteCount }} mine</span>
    </div>

    <div v-if="canEdit && !editing" class="flex gap-2 mt-1 justify-end">
      <button class="text-gray-400 hover:text-blue-600 text-xs" @click="startEdit">Edit</button>
      <button class="text-gray-400 hover:text-red-600 text-xs" @click="$emit('delete', card)">Delete</button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import type { Card } from '../stores/card'

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
}>()

const editing = ref(false)
const editText = ref('')
const editAssignee = ref('')

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
</script>
