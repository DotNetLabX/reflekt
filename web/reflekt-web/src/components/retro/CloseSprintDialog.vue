<template>
  <button
    class="px-3 py-1.5 bg-danger-hover hover:bg-danger-active text-white text-sm font-medium rounded-sm transition-colors cursor-pointer"
    @click="open = true"
  >
    Close Sprint
  </button>

  <BaseModal :open="open" title="Close Sprint" @close="open = false">
    <p class="text-sm text-text-secondary">Close this sprint? No more changes will be allowed.</p>
    <template #footer>
      <div class="flex justify-end gap-3">
        <BaseButton variant="secondary" @click="open = false">Cancel</BaseButton>
        <BaseButton variant="danger" @click="close">Close Sprint</BaseButton>
      </div>
    </template>
  </BaseModal>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useSprintStore } from '@/stores/sprint'
import { useToast } from '@/composables/useToast'
import BaseButton from '@/components/ui/BaseButton.vue'
import BaseModal from '@/components/ui/BaseModal.vue'

const props = defineProps<{
  sprintId: number
}>()

const sprintStore = useSprintStore()
const { addToast } = useToast()

const open = ref(false)

async function close() {
  open.value = false
  const teamId = sprintStore.currentSummary?.teamId
  if (!teamId) return
  try {
    await sprintStore.closeSprint(teamId, props.sprintId)
  } catch {
    addToast('Failed to close sprint.', 'error')
  }
}
</script>
