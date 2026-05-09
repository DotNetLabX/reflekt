<template>
  <div class="w-full max-w-lg p-8 bg-white rounded shadow flex flex-col gap-4">
    <router-link :to="{ name: 'welcome' }" class="text-sm text-gray-500 hover:text-gray-700 self-start">
      ← Back to sprints
    </router-link>

    <div class="flex items-center justify-between">
      <h1 class="text-2xl font-bold text-gray-800">{{ sprint.name }}</h1>
      <span :class="stateBadgeClass">{{ sprint.state }}</span>
    </div>

    <div class="grid grid-cols-2 gap-2 text-sm text-gray-600">
      <span class="font-medium">Phase</span><span>{{ sprint.currentPhase }}</span>
      <span class="font-medium">Votes per participant</span><span>{{ sprint.votesPerParticipant }}</span>
      <span class="font-medium">Created</span><span>{{ new Date(sprint.createdAt).toLocaleString() }}</span>
      <template v-if="sprint.closedAt">
        <span class="font-medium">Closed</span><span>{{ new Date(sprint.closedAt).toLocaleString() }}</span>
      </template>
    </div>

    <div class="flex flex-col gap-2 pt-2">
      <button v-if="sprint.state === 'Draft'" class="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 text-sm disabled:opacity-50" :disabled="busy" @click="$emit('start')">
        Start Sprint
      </button>
      <router-link v-if="sprint.state === 'Active'" :to="{ name: 'retro-room', params: { sprintId: sprint.id } }" class="px-4 py-2 bg-green-600 text-white rounded hover:bg-green-700 text-sm text-center">
        Open Retro Room
      </router-link>
      <button v-if="sprint.state === 'Active'" class="px-4 py-2 bg-gray-200 text-gray-800 rounded hover:bg-gray-300 text-sm" @click="$emit('copy-invite')">
        {{ copied ? 'Invite link copied!' : 'Copy invite link' }}
      </button>
      <button v-if="sprint.state === 'Active' && sprint.currentPhase !== 'Reporting'" class="px-4 py-2 bg-indigo-600 text-white rounded hover:bg-indigo-700 text-sm disabled:opacity-50" :disabled="busy" @click="$emit('advance-phase')">
        Advance to {{ nextPhaseLabel }}
      </button>
      <button v-if="sprint.state === 'Active'" class="px-4 py-2 bg-red-600 text-white rounded hover:bg-red-700 text-sm disabled:opacity-50" :disabled="busy" @click="$emit('close')">
        Close Sprint
      </button>
      <router-link v-if="sprint.state === 'Closed'" :to="{ name: 'report', params: { teamId, sprintId: sprint.id } }" class="px-4 py-2 bg-indigo-600 text-white rounded hover:bg-indigo-700 text-sm text-center">
        View Report
      </router-link>
    </div>

    <!-- Participants -->
    <div v-if="sprint.state === 'Active' && participants.length > 0" class="flex flex-col gap-2 pt-2 border-t">
      <h2 class="text-sm font-semibold text-gray-600 uppercase tracking-wide">Participants</h2>
      <div class="flex flex-col gap-1">
        <div v-for="p in participants" :key="p.id" class="flex items-center justify-between px-3 py-2 bg-gray-50 rounded text-sm">
          <span class="text-gray-800">{{ p.displayName }}</span>
          <button class="text-red-600 hover:text-red-800 text-xs font-medium disabled:opacity-50" :disabled="busy" @click="$emit('kick', p)">Kick</button>
        </div>
      </div>
    </div>

    <p v-if="errorMessage" class="text-red-600 text-sm">{{ errorMessage }}</p>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{
  sprint: {
    id: number
    name: string
    state: string
    currentPhase: string
    votesPerParticipant: number
    createdAt: string
    closedAt: string | null
  }
  teamId: number
  participants: { id: number; displayName: string }[]
  busy: boolean
  copied: boolean
  errorMessage: string
}>()

defineEmits<{
  start: []
  'advance-phase': []
  close: []
  'copy-invite': []
  kick: [p: { id: number; displayName: string }]
}>()

const phaseOrder = ['Writing', 'Voting', 'Discussing', 'Reporting']
const nextPhaseLabel = computed(() => {
  const idx = phaseOrder.indexOf(props.sprint.currentPhase)
  return phaseOrder[idx + 1] ?? ''
})

const stateBadgeClass = computed(() => {
  const base = 'px-2 py-1 rounded text-xs font-semibold'
  switch (props.sprint.state) {
    case 'Draft': return `${base} bg-gray-100 text-gray-700`
    case 'Active': return `${base} bg-green-100 text-green-700`
    case 'Closed': return `${base} bg-red-100 text-red-700`
    default: return base
  }
})
</script>
