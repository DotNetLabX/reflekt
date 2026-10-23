<template>
  <div
    class="relative rounded-md shadow-card border border-border p-3 flex flex-col gap-1 transition-shadow hover:shadow-card-hover"
    :class="cardBgClass"
  >
    <!-- Blurred -->
    <div v-if="blurred" class="relative select-none">
      <p class="text-sm text-text-primary whitespace-pre-wrap blur-md" aria-hidden="true">
        Someone's note
      </p>
      <div class="absolute inset-0" />
    </div>

    <!-- Display mode -->
    <div v-else-if="!editing">
      <p class="text-sm text-text-primary whitespace-pre-wrap">{{ card.text }}</p>
      <BaseBadge v-if="card.assigneeName" variant="info" class="mt-1">{{
        card.assigneeName
      }}</BaseBadge>
    </div>

    <!-- Edit mode -->
    <div v-else class="flex flex-col gap-1">
      <textarea
        v-model="editText"
        rows="2"
        maxlength="2000"
        aria-label="Card text"
        class="border border-border rounded-sm px-2 py-1 text-sm text-text-primary bg-surface focus:outline-none focus:ring-2 focus:ring-primary-500 resize-none"
      />
      <select
        v-if="card.cardType === 'ActionItem'"
        v-model="editAssignee"
        aria-label="Assignee"
        class="border border-border rounded-sm px-2 py-1 text-sm text-text-primary bg-surface focus:outline-none focus:ring-2 focus:ring-primary-500"
      >
        <option value="">Assign to...</option>
        <option v-for="p in participants" :key="p.displayName" :value="p.displayName">
          {{ p.displayName }}
        </option>
      </select>
      <div class="flex gap-2">
        <BaseButton
          size="sm"
          variant="primary"
          :disabled="!editText.trim() || (card.cardType === 'ActionItem' && !editAssignee.trim())"
          @click="commitEdit"
          >Save</BaseButton
        >
        <BaseButton size="sm" variant="secondary" @click="editing = false">Cancel</BaseButton>
      </div>
    </div>

    <RetroCardEvidenceLinks
      :card="card"
      :blurred="blurred"
      :editing="editing"
      :can-manage="canAttachLink ?? canEdit"
    />

    <!-- Edit/Delete actions -->
    <div v-if="!blurred && canEdit && !editing" class="flex gap-2 mt-1 justify-end">
      <BaseButton size="sm" variant="ghost" @click="startEdit">Edit</BaseButton>
      <BaseButton size="sm" variant="ghost" @click="actions.requestDelete(card)">Delete</BaseButton>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, inject } from 'vue'
import type { Card } from '@/types/card'
import type { ColumnColor } from '@/types/domain'
import BaseButton from '@/components/ui/BaseButton.vue'
import BaseBadge from '@/components/ui/BaseBadge.vue'
import { boardActionsKey, missingBoardActions } from '@/composables/useRetroBoardActions'
import RetroCardEvidenceLinks from './RetroCardEvidenceLinks.vue'

const props = defineProps<{
  card: Card
  canEdit: boolean
  blurred?: boolean
  canAttachLink?: boolean
  columnColor?: ColumnColor
  participants?: { displayName: string }[]
}>()

const actions = inject(boardActionsKey) ?? missingBoardActions('RetroCardItem')

const editing = ref(false)
const editText = ref('')
const editAssignee = ref('')

// Full class names: Tailwind emits only classes it finds written out in source.
const cardTintClasses: Record<ColumnColor, string> = {
  green: 'bg-col-green-light/30',
  red: 'bg-col-red-light/30',
  amber: 'bg-col-amber-light/30',
  blue: 'bg-col-blue-light/30',
  purple: 'bg-col-purple-light/30',
  teal: 'bg-col-teal-light/30',
  pink: 'bg-col-pink-light/30',
  slate: 'bg-col-slate-light/30',
}

const cardBgClass = computed(() => {
  if (!props.columnColor) return 'bg-surface'
  return cardTintClasses[props.columnColor]
})

function startEdit() {
  editText.value = props.card.text
  editAssignee.value = props.card.assigneeName ?? ''
  editing.value = true
}

function commitEdit() {
  if (!editText.value.trim()) return
  const assigneeName =
    props.card.cardType === 'ActionItem' ? editAssignee.value.trim() || null : null
  actions.saveEdit(props.card, editText.value.trim(), assigneeName)
  editing.value = false
}
</script>
