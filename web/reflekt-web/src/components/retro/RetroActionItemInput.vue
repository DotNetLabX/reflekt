<template>
  <div class="flex flex-col gap-2">
    <BaseInput
      v-model="text"
      :maxlength="2000"
      placeholder="Action item..."
      aria-label="Action item"
      :disabled="saving"
      @keydown.enter="submit"
    />
    <select
      v-model="assignee"
      :disabled="saving"
      aria-label="Assignee"
      class="border border-border rounded-md px-3 py-2 text-sm text-text-primary bg-surface focus:outline-none focus:ring-2 focus:ring-primary-500"
    >
      <option value="">Assign to...</option>
      <option v-for="p in participants" :key="p.id" :value="p.displayName">
        {{ p.displayName }}
      </option>
    </select>
    <BaseButton
      size="sm"
      variant="primary"
      :disabled="saving || !text.trim() || !assignee"
      @click="submit"
      >Add</BaseButton
    >
  </div>
</template>

<script setup lang="ts">
import { ref, inject } from 'vue'
import BaseInput from '@/components/ui/BaseInput.vue'
import BaseButton from '@/components/ui/BaseButton.vue'
import { boardActionsKey, missingBoardActions } from '@/composables/useRetroBoardActions'
import type { Participant } from '@/types/participant'

defineProps<{
  participants: Participant[]
}>()

const actions = inject(boardActionsKey) ?? missingBoardActions('RetroActionItemInput')

const text = ref('')
const assignee = ref('')
const saving = ref(false)

async function submit() {
  const value = text.value.trim()
  const assigneeName = assignee.value
  if (saving.value || !value || !assigneeName) return
  saving.value = true
  text.value = ''
  assignee.value = ''
  const added = await actions.addActionItem(value, assigneeName)
  saving.value = false
  if (!added) {
    text.value = value
    assignee.value = assigneeName
  }
}
</script>
