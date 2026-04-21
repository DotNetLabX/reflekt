<template>
  <div class="flex items-center justify-center min-h-screen bg-gray-50">
    <div v-if="!sprint" class="text-gray-500">Loading sprint…</div>

    <div v-else class="w-full max-w-lg p-8 bg-white rounded shadow flex flex-col gap-4">
      <router-link
        :to="{ name: 'welcome' }"
        class="text-sm text-gray-500 hover:text-gray-700 self-start"
      >
        ← Back to sprints
      </router-link>

      <div class="flex items-center justify-between">
        <h1 class="text-2xl font-bold text-gray-800">{{ sprint.name }}</h1>
        <span :class="stateBadgeClass">{{ sprint.state }}</span>
      </div>

      <div class="grid grid-cols-2 gap-2 text-sm text-gray-600">
        <span class="font-medium">Phase</span>
        <span>{{ sprint.currentPhase }}</span>
        <span class="font-medium">Votes per participant</span>
        <span>{{ sprint.votesPerParticipant }}</span>
        <span class="font-medium">Created</span>
        <span>{{ new Date(sprint.createdAt).toLocaleString() }}</span>
        <template v-if="sprint.closedAt">
          <span class="font-medium">Closed</span>
          <span>{{ new Date(sprint.closedAt).toLocaleString() }}</span>
        </template>
      </div>

      <div class="flex flex-col gap-2 pt-2">
        <button
          v-if="sprint.state === 'Draft'"
          class="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 text-sm disabled:opacity-50"
          :disabled="busy"
          @click="handleStart"
        >
          Start Sprint
        </button>

        <router-link
          v-if="sprint.state === 'Active'"
          :to="{ name: 'retro-room', params: { sprintId: sprint.id } }"
          class="px-4 py-2 bg-green-600 text-white rounded hover:bg-green-700 text-sm text-center"
        >
          Open Retro Room
        </router-link>

        <button
          v-if="sprint.state === 'Active'"
          class="px-4 py-2 bg-gray-200 text-gray-800 rounded hover:bg-gray-300 text-sm"
          @click="copyInviteLink"
        >
          {{ copied ? 'Invite link copied!' : 'Copy invite link' }}
        </button>

        <button
          v-if="sprint.state === 'Active' && sprint.currentPhase !== 'Closed'"
          class="px-4 py-2 bg-indigo-600 text-white rounded hover:bg-indigo-700 text-sm disabled:opacity-50"
          :disabled="busy"
          @click="handleAdvancePhase"
        >
          Advance to {{ nextPhaseLabel }}
        </button>

        <button
          v-if="sprint.state === 'Active'"
          class="px-4 py-2 bg-red-600 text-white rounded hover:bg-red-700 text-sm disabled:opacity-50"
          :disabled="busy"
          @click="handleClose"
        >
          Close Sprint
        </button>

        <router-link
          v-if="sprint.state === 'Closed'"
          :to="{ name: 'report', params: { teamId, sprintId: sprint.id } }"
          class="px-4 py-2 bg-indigo-600 text-white rounded hover:bg-indigo-700 text-sm text-center"
        >
          View Report
        </router-link>
      </div>

      <p v-if="errorMessage" class="text-red-600 text-sm">{{ errorMessage }}</p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useSprintStore } from '../stores/sprint'

const route = useRoute()
const sprintStore = useSprintStore()

const teamId = Number(route.params.teamId)
const sprintId = Number(route.params.sprintId)

const busy = ref(false)
const errorMessage = ref('')
const copied = ref(false)
let copiedTimer: ReturnType<typeof setTimeout> | null = null

const sprint = computed(() => sprintStore.currentSprint)

const phaseOrder = ['Writing', 'Voting', 'Discussing', 'Closed']
const nextPhaseLabel = computed(() => {
  if (!sprint.value) return ''
  const idx = phaseOrder.indexOf(sprint.value.currentPhase)
  return phaseOrder[idx + 1] ?? ''
})

const stateBadgeClass = computed(() => {
  const base = 'px-2 py-1 rounded text-xs font-semibold'
  switch (sprint.value?.state) {
    case 'Draft': return `${base} bg-gray-100 text-gray-700`
    case 'Active': return `${base} bg-green-100 text-green-700`
    case 'Closed': return `${base} bg-red-100 text-red-700`
    default: return base
  }
})

onMounted(() => sprintStore.fetchSprint(teamId, sprintId))

async function handleStart() {
  errorMessage.value = ''
  busy.value = true
  try { await sprintStore.startSprint(teamId, sprintId) }
  catch { errorMessage.value = 'Failed to start sprint.' }
  finally { busy.value = false }
}

async function handleAdvancePhase() {
  errorMessage.value = ''
  busy.value = true
  try { await sprintStore.advancePhase(teamId, sprintId) }
  catch { errorMessage.value = 'Failed to advance phase.' }
  finally { busy.value = false }
}

async function handleClose() {
  errorMessage.value = ''
  busy.value = true
  try { await sprintStore.closeSprint(teamId, sprintId) }
  catch { errorMessage.value = 'Failed to close sprint.' }
  finally { busy.value = false }
}

async function copyInviteLink() {
  const url = `${window.location.origin}/sprint/${sprintId}`
  try {
    await navigator.clipboard.writeText(url)
  } catch {
    errorMessage.value = 'Failed to copy link. Copy manually: ' + url
    return
  }
  copied.value = true
  if (copiedTimer) clearTimeout(copiedTimer)
  copiedTimer = setTimeout(() => (copied.value = false), 2000)
}
</script>
