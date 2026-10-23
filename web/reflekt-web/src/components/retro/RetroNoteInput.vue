<template>
  <div class="flex gap-2">
    <BaseInput
      v-model="text"
      :maxlength="2000"
      placeholder="Add a note..."
      aria-label="New note"
      class="flex-1"
      :disabled="saving"
      @keydown.enter="submit"
    />
    <BaseButton size="sm" variant="primary" :disabled="saving || !text.trim()" @click="submit"
      >Add</BaseButton
    >
  </div>
</template>

<script setup lang="ts">
import { ref, inject } from 'vue'
import BaseInput from '@/components/ui/BaseInput.vue'
import BaseButton from '@/components/ui/BaseButton.vue'
import { boardActionsKey, missingBoardActions } from '@/composables/useRetroBoardActions'

const props = defineProps<{
  column: 'Column1' | 'Column2' | 'Column3'
}>()

const actions = inject(boardActionsKey) ?? missingBoardActions('RetroNoteInput')

const text = ref('')
const saving = ref(false)

async function submit() {
  const value = text.value.trim()
  if (saving.value || !value) return
  saving.value = true
  text.value = ''
  const added = await actions.addNote(props.column, value)
  saving.value = false
  if (!added) text.value = value
}
</script>
