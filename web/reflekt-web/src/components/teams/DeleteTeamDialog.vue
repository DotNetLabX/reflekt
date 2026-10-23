<template>
  <BaseModal
    :open="open"
    :title="team.hasActiveSprint ? 'Cannot Delete Team' : 'Delete Team'"
    @close="$emit('close')"
  >
    <!-- Active sprint blocked state -->
    <p v-if="team.hasActiveSprint" class="text-sm text-text-secondary">
      This team has an active sprint. Close or delete it before deleting the team.
    </p>

    <!-- Confirmation state -->
    <template v-else>
      <p class="text-sm text-text-secondary mb-4">
        Delete team <strong>"{{ team.name }}"</strong> and all its sprints? This cannot be undone.
      </p>

      <div>
        <label :for="confirmInputId" class="block text-sm font-medium text-text-secondary mb-1">
          Type the team name to confirm
        </label>
        <BaseTooltip
          v-slot="{ describedby }"
          text="Type the team name exactly as shown above to confirm deletion."
          panel-class="left-0 bottom-10 w-64 whitespace-normal"
        >
          <BaseInput
            :id="confirmInputId"
            v-model="confirmationInput"
            :placeholder="team.name"
            :aria-describedby="describedby"
          />
        </BaseTooltip>
      </div>

      <p v-if="errorMessage" class="text-danger text-sm mt-4">{{ errorMessage }}</p>
    </template>

    <template #footer>
      <BaseButton
        v-if="team.hasActiveSprint"
        variant="secondary"
        class="w-full"
        @click="$emit('close')"
      >
        Close
      </BaseButton>
      <div v-else class="flex gap-2 justify-end">
        <BaseButton variant="ghost" @click="$emit('close')">Cancel</BaseButton>
        <BaseButton
          variant="danger"
          :disabled="confirmationInput !== team.name || busy"
          @click="handleConfirm"
        >
          {{ busy ? 'Deleting…' : 'Delete Team' }}
        </BaseButton>
      </div>
    </template>
  </BaseModal>
</template>

<script setup lang="ts">
import { ref, useId, watch } from 'vue'
import { describeApiError } from '@/api/client'
import { useTeamsStore } from '@/stores/teams'
import BaseButton from '@/components/ui/BaseButton.vue'
import BaseInput from '@/components/ui/BaseInput.vue'
import BaseModal from '@/components/ui/BaseModal.vue'
import BaseTooltip from '@/components/ui/BaseTooltip.vue'
import type { TeamSummary } from '@/types/team'

const props = defineProps<{
  team: TeamSummary
  open: boolean
}>()

const emit = defineEmits<{
  deleted: []
  close: []
}>()

const teamsStore = useTeamsStore()
const confirmInputId = useId()
const confirmationInput = ref('')
const errorMessage = ref('')
const busy = ref(false)

watch(
  () => props.open,
  (open) => {
    if (!open) {
      confirmationInput.value = ''
      errorMessage.value = ''
      busy.value = false
    }
  },
)

watch(
  () => props.team.id,
  () => {
    confirmationInput.value = ''
    errorMessage.value = ''
    busy.value = false
  },
)

async function handleConfirm() {
  if (confirmationInput.value !== props.team.name) return
  errorMessage.value = ''
  busy.value = true
  try {
    await teamsStore.deleteTeam(props.team.id, confirmationInput.value)
    emit('deleted')
  } catch (err: unknown) {
    errorMessage.value = describeApiError(err, { fallback: 'Failed to delete team.' })
  } finally {
    busy.value = false
  }
}
</script>
