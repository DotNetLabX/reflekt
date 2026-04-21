<template>
  <div class="flex items-center justify-center min-h-screen bg-gray-50">
    <!-- Name picker -->
    <div v-if="!joined" class="w-full max-w-sm p-8 bg-white rounded shadow flex flex-col gap-4">
      <h1 class="text-2xl font-bold text-gray-800">Join Retro</h1>
      <p class="text-sm text-gray-500">{{ sprintName || `Sprint #${sprintId}` }}</p>

      <p v-if="errorMessage" class="text-sm text-red-600">{{ errorMessage }}</p>

      <form class="flex flex-col gap-3" @submit.prevent="handleJoin">
        <input
          v-model="displayName"
          type="text"
          maxlength="50"
          placeholder="Your display name"
          class="border rounded px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          :disabled="busy"
          required
        />
        <button
          type="submit"
          class="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 text-sm disabled:opacity-50"
          :disabled="busy || !displayName.trim()"
        >
          Join
        </button>
      </form>
    </div>

    <!-- Retro room -->
    <div v-else class="w-full max-w-6xl p-6 flex flex-col gap-6">
      <div class="flex items-center justify-between">
        <h1 class="text-2xl font-bold text-gray-800">{{ sprintName || `Sprint #${sprintId}` }}</h1>
        <div class="flex items-center gap-4">
          <span class="text-sm font-medium text-gray-700">
            Votes: {{ voteStore.remainingBudget }}/{{ voteStore.votesPerParticipant }} remaining
          </span>
          <span class="text-sm text-gray-500">Retro Room</span>
        </div>
      </div>

      <!-- Participants -->
      <div class="bg-white rounded shadow p-4 flex flex-col gap-2">
        <h2 class="text-sm font-semibold text-gray-600 uppercase tracking-wide">Participants</h2>
        <div class="flex flex-wrap gap-2">
          <span
            v-for="p in participants"
            :key="p.id"
            class="px-3 py-1 bg-blue-100 text-blue-800 rounded-full text-sm"
          >
            {{ p.displayName }}
          </span>
        </div>
      </div>

      <!-- Three-column board -->
      <div class="grid grid-cols-3 gap-4">
        <!-- Went Well -->
        <div class="bg-white rounded shadow flex flex-col">
          <div class="px-4 py-3 border-b bg-green-50 rounded-t">
            <h2 class="font-semibold text-green-800 text-sm">Went Well</h2>
          </div>
          <div class="flex-1 flex flex-col gap-2 p-3 overflow-y-auto max-h-[60vh]">
            <div v-if="wentWell.length === 0" class="text-gray-400 text-sm text-center py-4">
              No cards yet
            </div>
            <CardItem
              v-for="card in wentWell"
              :key="card.id"
              :card="card"
              :can-edit="canEditCard(card)"
              :show-voting="true"
              :vote-count="voteStore.voteCountForCard(card.id)"
              :my-vote-count="voteStore.myVotesOnCard(card.id)"
              :remaining-budget="voteStore.remainingBudget"
              @edit="saveEdit"
              @delete="confirmDelete"
              @vote="castVote"
              @retract-vote="retractVote"
              @attach-evidence-link="attachEvidenceLink"
              @detach-evidence-link="detachEvidenceLink"
            />
          </div>
          <div class="p-3 border-t flex gap-2">
            <input
              v-model="newCardText.WentWell"
              type="text"
              maxlength="2000"
              placeholder="Add a note…"
              class="flex-1 border rounded px-2 py-1 text-sm focus:outline-none focus:ring-2 focus:ring-green-400"
              @keydown.enter="addNote('WentWell')"
            />
            <button
              class="px-3 py-1 bg-green-600 text-white rounded text-sm hover:bg-green-700 disabled:opacity-50"
              :disabled="!newCardText.WentWell.trim()"
              @click="addNote('WentWell')"
            >
              Add
            </button>
          </div>
        </div>

        <!-- Didn't Go Well -->
        <div class="bg-white rounded shadow flex flex-col">
          <div class="px-4 py-3 border-b bg-red-50 rounded-t">
            <h2 class="font-semibold text-red-800 text-sm">Didn't Go Well</h2>
          </div>
          <div class="flex-1 flex flex-col gap-2 p-3 overflow-y-auto max-h-[60vh]">
            <div v-if="didntGoWell.length === 0" class="text-gray-400 text-sm text-center py-4">
              No cards yet
            </div>
            <CardItem
              v-for="card in didntGoWell"
              :key="card.id"
              :card="card"
              :can-edit="canEditCard(card)"
              :show-voting="true"
              :vote-count="voteStore.voteCountForCard(card.id)"
              :my-vote-count="voteStore.myVotesOnCard(card.id)"
              :remaining-budget="voteStore.remainingBudget"
              @edit="saveEdit"
              @delete="confirmDelete"
              @vote="castVote"
              @retract-vote="retractVote"
              @attach-evidence-link="attachEvidenceLink"
              @detach-evidence-link="detachEvidenceLink"
            />
          </div>
          <div class="p-3 border-t flex gap-2">
            <input
              v-model="newCardText.DidntGoWell"
              type="text"
              maxlength="2000"
              placeholder="Add a note…"
              class="flex-1 border rounded px-2 py-1 text-sm focus:outline-none focus:ring-2 focus:ring-red-400"
              @keydown.enter="addNote('DidntGoWell')"
            />
            <button
              class="px-3 py-1 bg-red-600 text-white rounded text-sm hover:bg-red-700 disabled:opacity-50"
              :disabled="!newCardText.DidntGoWell.trim()"
              @click="addNote('DidntGoWell')"
            >
              Add
            </button>
          </div>
        </div>

        <!-- Action Items -->
        <div class="bg-white rounded shadow flex flex-col">
          <div class="px-4 py-3 border-b bg-blue-50 rounded-t">
            <h2 class="font-semibold text-blue-800 text-sm">Action Items</h2>
          </div>
          <div class="flex-1 flex flex-col gap-2 p-3 overflow-y-auto max-h-[60vh]">
            <div v-if="actionItems.length === 0" class="text-gray-400 text-sm text-center py-4">
              No action items yet
            </div>
            <CardItem
              v-for="card in actionItems"
              :key="card.id"
              :card="card"
              :can-edit="canEditCard(card)"
              @edit="saveEdit"
              @delete="confirmDelete"
              @attach-evidence-link="attachEvidenceLink"
              @detach-evidence-link="detachEvidenceLink"
            />
          </div>
          <div class="p-3 border-t flex flex-col gap-2">
            <input
              v-model="newActionText"
              type="text"
              maxlength="2000"
              placeholder="Action item…"
              class="border rounded px-2 py-1 text-sm focus:outline-none focus:ring-2 focus:ring-blue-400"
              @keydown.enter="addActionItem"
            />
            <select
              v-model="newActionAssignee"
              class="border rounded px-2 py-1 text-sm focus:outline-none focus:ring-2 focus:ring-blue-400"
            >
              <option value="">Assign to…</option>
              <option v-for="p in participants" :key="p.id" :value="p.displayName">
                {{ p.displayName }}
              </option>
            </select>
            <button
              class="px-3 py-1 bg-blue-600 text-white rounded text-sm hover:bg-blue-700 disabled:opacity-50"
              :disabled="!newActionText.trim() || !newActionAssignee"
              @click="addActionItem"
            >
              Add
            </button>
          </div>
        </div>
      </div>

      <p v-if="toastMessage" class="fixed bottom-4 right-4 bg-red-600 text-white px-4 py-2 rounded shadow text-sm">
        {{ toastMessage }}
      </p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRoute } from 'vue-router'
import { useGuestStore } from '../stores/guest'
import { useParticipantStore } from '../stores/participant'
import { useCardStore, type Card, type EvidenceLink } from '../stores/card'
import { useAuthStore } from '../stores/auth'
import { useSprintHub } from '../composables/useSprintHub'
import { useVoteStore } from '../stores/vote'
import { useSprintStore } from '../stores/sprint'
import CardItem from '../components/CardItem.vue'

const route = useRoute()
const guestStore = useGuestStore()
const participantStore = useParticipantStore()
const cardStore = useCardStore()
const authStore = useAuthStore()
const voteStore = useVoteStore()
const sprintStore = useSprintStore()
const { connect, disconnect } = useSprintHub()

const sprintId = Number(route.params.sprintId)
const displayName = ref('')
const busy = ref(false)
const errorMessage = ref('')
const joined = ref(false)
const toastMessage = ref('')

const newCardText = ref({ WentWell: '', DidntGoWell: '' })
const newActionText = ref('')
const newActionAssignee = ref('')

const participants = computed(() => participantStore.participants)
const wentWell = computed(() => cardStore.wentWell)
const didntGoWell = computed(() => cardStore.didntGoWell)
const actionItems = computed(() => cardStore.actionItems)
const sprintName = computed(() => sprintStore.currentSummary?.name ?? '')

function canEditCard(card: Card): boolean {
  return card.participantId === participantStore.currentParticipant?.id || authStore.user !== null
}

let toastTimer: ReturnType<typeof setTimeout> | null = null
function showToast(msg: string) {
  toastMessage.value = msg
  if (toastTimer) clearTimeout(toastTimer)
  toastTimer = setTimeout(() => (toastMessage.value = ''), 3000)
}

async function addNote(column: 'WentWell' | 'DidntGoWell') {
  const text = newCardText.value[column].trim()
  if (!text) return
  newCardText.value[column] = ''
  const optimistic: Card = {
    id: -Date.now(),
    sprintId,
    participantId: participantStore.currentParticipant?.id ?? 0,
    columnType: column,
    cardType: 'Note',
    text,
    assigneeName: null,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    voteCount: 0,
    evidenceLinks: [],
  }
  cardStore.cards.unshift(optimistic)
  try {
    const created = await cardStore.createCard(sprintId, { text, columnType: column, cardType: 'Note' })
    const idx = cardStore.cards.findIndex((c) => c.id === optimistic.id)
    if (idx !== -1) cardStore.cards[idx] = created
  } catch {
    cardStore.cards = cardStore.cards.filter((c) => c.id !== optimistic.id)
    showToast('Failed to add card.')
  }
}

async function addActionItem() {
  const text = newActionText.value.trim()
  const assigneeName = newActionAssignee.value
  if (!text || !assigneeName) return
  newActionText.value = ''
  newActionAssignee.value = ''
  const optimistic: Card = {
    id: -Date.now(),
    sprintId,
    participantId: participantStore.currentParticipant?.id ?? 0,
    columnType: 'ActionItems',
    cardType: 'ActionItem',
    text,
    assigneeName,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    voteCount: 0,
    evidenceLinks: [],
  }
  cardStore.cards.unshift(optimistic)
  try {
    const created = await cardStore.createCard(sprintId, { text, columnType: 'ActionItems', cardType: 'ActionItem', assigneeName })
    const idx = cardStore.cards.findIndex((c) => c.id === optimistic.id)
    if (idx !== -1) cardStore.cards[idx] = created
  } catch {
    cardStore.cards = cardStore.cards.filter((c) => c.id !== optimistic.id)
    showToast('Failed to add action item.')
  }
}

async function saveEdit(card: Card, text: string, assigneeName: string | null) {
  const prev = { ...card }
  const idx = cardStore.cards.findIndex((c) => c.id === card.id)
  if (idx !== -1) cardStore.cards[idx] = { ...card, text, assigneeName, updatedAt: new Date().toISOString() }
  try {
    await cardStore.editCard(sprintId, card.id, { text, assigneeName })
  } catch {
    if (idx !== -1) cardStore.cards[idx] = prev
    showToast('Failed to update card.')
  }
}

async function confirmDelete(card: Card) {
  const snapshot = [...cardStore.cards]
  cardStore.cards = cardStore.cards.filter((c) => c.id !== card.id)
  try {
    await cardStore.deleteCard(sprintId, card.id)
  } catch {
    cardStore.cards = snapshot
    showToast('Failed to delete card.')
  }
}

onMounted(async () => {
  sprintStore.fetchSprintSummary(sprintId)
  guestStore.loadToken(sprintId)
  if (guestStore.token) {
    try {
      await participantStore.fetchMe(sprintId)
      await enterRoom()
    } catch (err: unknown) {
      const status = (err as { status?: number })?.status
      if (status === 401) guestStore.clearToken(sprintId)
    }
  }
})

onUnmounted(() => disconnect())

async function enterRoom() {
  await Promise.all([
    participantStore.fetchParticipants(sprintId),
    cardStore.fetchCards(sprintId),
  ])
  await voteStore.fetchMyVotes(sprintId)
  await connect(sprintId)
  joined.value = true
}

async function castVote(card: Card) {
  try {
    await voteStore.castVote(sprintId, card.id)
  } catch {
    showToast('Failed to cast vote.')
  }
}

async function retractVote(card: Card) {
  try {
    await voteStore.retractVote(sprintId, card.id)
  } catch {
    showToast('Failed to retract vote.')
  }
}

async function attachEvidenceLink(cardId: number, url: string) {
  const card = cardStore.cards.find((c) => c.id === cardId)
  if (!card) return
  const optimistic: EvidenceLink = {
    id: -Date.now(),
    cardId,
    url,
    source: 'Generic',
    title: null,
    description: null,
    iconUrl: null,
    imageUrl: null,
    externalKey: null,
    fetchedAt: null,
    fetchStatus: 'Pending',
  }
  card.evidenceLinks.push(optimistic)
  try {
    const real = await cardStore.attachEvidenceLink(sprintId, cardId, url)
    const idx = card.evidenceLinks.findIndex((e) => e.id === optimistic.id)
    if (idx !== -1) card.evidenceLinks[idx] = real
  } catch {
    card.evidenceLinks = card.evidenceLinks.filter((e) => e.id !== optimistic.id)
    showToast('Failed to attach link.')
  }
}

async function detachEvidenceLink(cardId: number, evidenceLinkId: number) {
  const card = cardStore.cards.find((c) => c.id === cardId)
  if (!card) return
  const snapshot = [...card.evidenceLinks]
  card.evidenceLinks = card.evidenceLinks.filter((e) => e.id !== evidenceLinkId)
  try {
    await cardStore.detachEvidenceLink(sprintId, cardId, evidenceLinkId)
  } catch {
    card.evidenceLinks = snapshot
    showToast('Failed to remove link.')
  }
}

async function handleJoin() {
  if (!displayName.value.trim()) return
  busy.value = true
  errorMessage.value = ''
  try {
    const response = await participantStore.join(sprintId, displayName.value.trim())
    guestStore.saveToken(sprintId, response.guestToken, response.displayName)
    await enterRoom()
  } catch (err: unknown) {
    const status = (err as { status?: number })?.status
    if (status === 409) errorMessage.value = 'Sprint is not accepting participants.'
    else if (status === 404) errorMessage.value = 'Sprint not found.'
    else errorMessage.value = 'Failed to join. Please try again.'
  } finally {
    busy.value = false
  }
}
</script>
