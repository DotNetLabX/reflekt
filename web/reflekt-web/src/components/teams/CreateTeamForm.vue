<template>
  <BaseCard padding="none" radius="sm" class="w-72">
    <div class="flex flex-col gap-3 p-8">
      <h2 class="text-lg font-semibold text-text-primary">{{ heading }}</h2>
      <BaseTooltip
        v-slot="{ describedby }"
        text="Choose a name for your team. Must be unique among your teams. Max 100 characters."
        panel-class="left-0 bottom-10 w-64 whitespace-normal"
      >
        <BaseInput
          v-model="teamName"
          placeholder="Team name"
          aria-label="Team name"
          :maxlength="100"
          :aria-describedby="describedby"
        />
      </BaseTooltip>
      <p v-if="errorMessage" class="text-danger text-sm">{{ errorMessage }}</p>
      <div class="flex gap-2">
        <BaseButton class="flex-1" :disabled="busy" @click="handleCreate">
          {{ busy ? 'Creating…' : 'Create Team' }}
        </BaseButton>
        <BaseButton v-if="showCancel" variant="secondary" @click="$emit('cancel')">
          Cancel
        </BaseButton>
      </div>
    </div>
  </BaseCard>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { describeApiError } from '@/api/client'
import { useTeamsStore } from '@/stores/teams'
import BaseButton from '@/components/ui/BaseButton.vue'
import BaseCard from '@/components/ui/BaseCard.vue'
import BaseInput from '@/components/ui/BaseInput.vue'
import BaseTooltip from '@/components/ui/BaseTooltip.vue'
import type { TeamSummary } from '@/types/team'

withDefaults(defineProps<{ heading?: string; showCancel?: boolean }>(), {
  heading: 'Create a new team',
})

const emit = defineEmits<{
  created: [team: TeamSummary]
  cancel: []
}>()

const teamsStore = useTeamsStore()
const teamName = ref('')
const errorMessage = ref('')
const busy = ref(false)

async function handleCreate() {
  errorMessage.value = ''
  const name = teamName.value.trim()
  if (!name) {
    errorMessage.value = 'Team name is required.'
    return
  }
  busy.value = true
  try {
    const team = await teamsStore.createTeam(name)
    teamName.value = ''
    emit('created', team)
  } catch (err: unknown) {
    errorMessage.value = describeApiError(err, { fallback: 'Failed to create team.' })
  } finally {
    busy.value = false
  }
}
</script>
