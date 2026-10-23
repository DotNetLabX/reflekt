<template>
  <button
    class="px-3 py-1.5 bg-primary-600 hover:bg-primary-700 text-white text-sm font-medium rounded-sm transition-colors cursor-pointer"
    @click="check"
  >
    Next: {{ nextPhase }}
  </button>

  <BaseModal :open="open" :title="prompt.title" @close="open = false">
    <p v-if="prompt.error" class="text-sm text-danger">{{ prompt.error }}</p>
    <p
      v-else
      class="text-sm text-text-secondary"
      :title="prompt.warning ? warningTooltip : undefined"
    >
      {{ prompt.message }}
    </p>
    <template #footer>
      <div class="flex justify-end gap-3">
        <BaseButton variant="secondary" @click="open = false">Cancel</BaseButton>
        <BaseButton v-if="!prompt.error" variant="primary" @click="advance">Advance</BaseButton>
      </div>
    </template>
  </BaseModal>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useSprintStore } from '@/stores/sprint'
import { useRetroPhase } from '@/composables/useRetroPhase'
import { useToast } from '@/composables/useToast'
import BaseButton from '@/components/ui/BaseButton.vue'
import BaseModal from '@/components/ui/BaseModal.vue'

const props = defineProps<{
  sprintId: number
}>()

const sprintStore = useSprintStore()
const { nextPhase, advancePrompt } = useRetroPhase()
const { addToast } = useToast()

const warningTooltip =
  "Some action items from the last retro haven't been reviewed yet. You can still advance if needed."

const open = ref(false)
const prompt = ref({ title: 'Advance Phase', message: '', error: '', warning: false })

async function check() {
  const teamId = sprintStore.currentSummary?.teamId
  if (!teamId) return
  try {
    const result = await sprintStore.advancePhaseCheck(teamId, props.sprintId)
    prompt.value = advancePrompt(result, prompt.value.warning)
    open.value = true
  } catch {
    addToast('Failed to check phase status.', 'error')
  }
}

async function advance() {
  open.value = false
  const teamId = sprintStore.currentSummary?.teamId
  if (!teamId) return
  try {
    await sprintStore.advancePhase(teamId, props.sprintId)
  } catch {
    addToast('Failed to advance phase.', 'error')
  }
}
</script>
