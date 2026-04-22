<template>
  <div :class="['bg-background flex', joined && !kicked && !loading ? 'h-[calc(100vh-3.5rem)] overflow-hidden flex-col' : 'min-h-screen items-center justify-center']">
    <!-- Kicked screen -->
    <div v-if="kicked" class="w-full max-w-sm p-8 bg-surface rounded-[--radius-md] shadow-card flex flex-col gap-4 items-center">
      <h1 class="text-2xl font-bold text-danger">You were removed</h1>
      <p class="text-sm text-gray-600 text-center">The Scrum Master removed you from this retro. You'll need a new invite to rejoin.</p>
    </div>

    <!-- Loading state -->
    <div v-else-if="loading" class="flex flex-col items-center gap-4">
      <LoadingSpinner size="lg">Joining retro...</LoadingSpinner>
    </div>

    <!-- Join form -->
    <div v-else-if="!joined" class="w-full max-w-sm">
      <BaseCard padding="lg">
        <div class="flex flex-col gap-4">
          <div class="flex items-center justify-center gap-2 text-primary-600">
            <RefreshCw :size="24" />
            <span class="text-xl font-semibold">Reflekt</span>
          </div>

          <h1 class="text-lg font-semibold text-gray-800 text-center">{{ sprintName || `Sprint #${sprintId}` }}</h1>

          <div v-if="summaryParticipants.length > 0" class="flex flex-col gap-2">
            <p class="text-xs font-semibold text-gray-500 uppercase tracking-wide">Already in this retro:</p>
            <div class="flex flex-wrap gap-2">
              <div
                v-for="p in summaryParticipants"
                :key="p.displayName"
                class="w-8 h-8 rounded-full bg-primary-100 text-primary-700 font-semibold text-sm flex items-center justify-center"
                :title="p.displayName"
              >
                {{ p.displayName.charAt(0).toUpperCase() }}
              </div>
            </div>
          </div>

          <p v-if="errorMessage" class="text-sm text-danger">{{ errorMessage }}</p>

          <form class="flex flex-col gap-3" @submit.prevent="handleJoin">
            <BaseInput
              v-model="displayName"
              :maxlength="50"
              placeholder="Your display name"
              :disabled="busy"
            />
            <BaseButton
              type="submit"
              variant="primary"
              size="lg"
              class="w-full"
              :disabled="busy || !displayName.trim()"
              :loading="busy"
            >
              Join
            </BaseButton>
          </form>
        </div>
      </BaseCard>
    </div>

    <!-- Retro room -->
    <div v-else class="w-full flex-1 min-h-0 px-4 sm:px-8 lg:px-12 py-6 flex flex-col gap-5">
      <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
        <h1 class="text-2xl font-bold text-gray-800">{{ sprintName || `Sprint #${sprintId}` }}</h1>
        <div class="flex items-center gap-3">
          <BaseBadge variant="info">
            Votes: {{ voteStore.remainingBudget }}/{{ voteStore.votesPerParticipant }}
          </BaseBadge>
        </div>
      </div>

      <!-- Phase indicator -->
      <PhaseIndicator :current-phase="currentPhase" />

      <!-- Participants -->
      <BaseCard padding="sm">
        <div class="flex flex-col gap-2">
          <h2 class="text-xs font-semibold text-gray-500 uppercase tracking-wide">Participants</h2>
          <div class="flex flex-wrap gap-2">
            <BaseBadge
              v-for="p in participants"
              :key="p.id"
              variant="primary"
            >
              {{ p.displayName }}
            </BaseBadge>
          </div>
        </div>
      </BaseCard>

      <!-- Mobile tab bar -->
      <div class="flex sm:hidden gap-1">
        <button
          class="flex-1 px-3 py-2 text-sm font-medium rounded-[--radius-sm] transition-colors cursor-pointer"
          :class="activeTab === 'WentWell' ? 'bg-went-well text-white' : 'bg-went-well-light text-went-well'"
          @click="activeTab = 'WentWell'"
        >
          Went Well
        </button>
        <button
          class="flex-1 px-3 py-2 text-sm font-medium rounded-[--radius-sm] transition-colors cursor-pointer"
          :class="activeTab === 'DidntGoWell' ? 'bg-didnt-go-well text-white' : 'bg-didnt-go-well-light text-didnt-go-well'"
          @click="activeTab = 'DidntGoWell'"
        >
          Didn't Go Well
        </button>
        <button
          v-if="showActionItems"
          class="flex-1 px-3 py-2 text-sm font-medium rounded-[--radius-sm] transition-colors cursor-pointer"
          :class="activeTab === 'ActionItems' ? 'bg-action-items text-white' : 'bg-action-items-light text-action-items'"
          @click="activeTab = 'ActionItems'"
        >
          Actions
        </button>
      </div>

      <!-- Board columns -->
      <div :class="boardGridClasses">
        <!-- Went Well -->
        <div v-show="isColumnVisible('WentWell')" class="bg-surface border border-gray-200 rounded-[--radius-md] shadow-card flex flex-col min-h-0">
          <div class="px-4 py-3 border-b bg-went-well-light rounded-t-[--radius-md] flex items-center gap-2">
            <SmilePlus :size="18" class="text-went-well" />
            <h2 class="font-semibold text-went-well text-base flex-1">Went Well</h2>
            <BaseBadge variant="success">{{ wentWell.length }}</BaseBadge>
          </div>
          <TransitionGroup
            name="card-list"
            tag="div"
            class="flex-1 flex flex-col gap-2 p-3 overflow-y-auto min-h-0 relative"
          >
            <div v-if="wentWell.length === 0" key="empty" class="text-gray-400 text-sm text-center py-4">
              No cards yet
            </div>
            <CardItem
              v-for="card in wentWell"
              :key="card.id"
              :card="card"
              :can-edit="canMutateCard(card)"
              :blurred="isWritingPhase && card.participantId !== currentParticipantId"
              :can-attach-link="policyStore.isAllowed(currentPhase, 'AttachEvidenceLink')"
              :show-voting="policyStore.isAllowed(currentPhase, 'CastVote')"
              :vote-count="voteStore.voteCountForCard(card.id)"
              :my-vote-count="voteStore.myVotesOnCard(card.id)"
              :remaining-budget="voteStore.remainingBudget"
              column-color="went-well"
              @edit="saveEdit"
              @delete="requestDelete"
              @vote="castVote"
              @retract-vote="retractVote"
              @attach-evidence-link="attachEvidenceLink"
              @detach-evidence-link="detachEvidenceLink"
            />
          </TransitionGroup>
          <div v-if="policyStore.isAllowed(currentPhase, 'CreateNote')" class="p-3 border-t flex gap-2">
            <BaseInput
              v-model="newCardText.WentWell"
              :maxlength="2000"
              placeholder="Add a note..."
              class="flex-1"
              @keydown.enter="addNote('WentWell')"
            />
            <BaseButton
              size="sm"
              variant="primary"
              :disabled="!newCardText.WentWell.trim()"
              @click="addNote('WentWell')"
            >
              Add
            </BaseButton>
          </div>
        </div>

        <!-- Didn't Go Well -->
        <div v-show="isColumnVisible('DidntGoWell')" class="bg-surface border border-gray-200 rounded-[--radius-md] shadow-card flex flex-col min-h-0">
          <div class="px-4 py-3 border-b bg-didnt-go-well-light rounded-t-[--radius-md] flex items-center gap-2">
            <Frown :size="18" class="text-didnt-go-well" />
            <h2 class="font-semibold text-didnt-go-well text-base flex-1">Didn't Go Well</h2>
            <BaseBadge variant="danger">{{ didntGoWell.length }}</BaseBadge>
          </div>
          <TransitionGroup
            name="card-list"
            tag="div"
            class="flex-1 flex flex-col gap-2 p-3 overflow-y-auto min-h-0 relative"
          >
            <div v-if="didntGoWell.length === 0" key="empty" class="text-gray-400 text-sm text-center py-4">
              No cards yet
            </div>
            <CardItem
              v-for="card in didntGoWell"
              :key="card.id"
              :card="card"
              :can-edit="canMutateCard(card)"
              :blurred="isWritingPhase && card.participantId !== currentParticipantId"
              :can-attach-link="policyStore.isAllowed(currentPhase, 'AttachEvidenceLink')"
              :show-voting="policyStore.isAllowed(currentPhase, 'CastVote')"
              :vote-count="voteStore.voteCountForCard(card.id)"
              :my-vote-count="voteStore.myVotesOnCard(card.id)"
              :remaining-budget="voteStore.remainingBudget"
              column-color="didnt-go-well"
              @edit="saveEdit"
              @delete="requestDelete"
              @vote="castVote"
              @retract-vote="retractVote"
              @attach-evidence-link="attachEvidenceLink"
              @detach-evidence-link="detachEvidenceLink"
            />
          </TransitionGroup>
          <div v-if="policyStore.isAllowed(currentPhase, 'CreateNote')" class="p-3 border-t flex gap-2">
            <BaseInput
              v-model="newCardText.DidntGoWell"
              :maxlength="2000"
              placeholder="Add a note..."
              class="flex-1"
              @keydown.enter="addNote('DidntGoWell')"
            />
            <BaseButton
              size="sm"
              variant="primary"
              :disabled="!newCardText.DidntGoWell.trim()"
              @click="addNote('DidntGoWell')"
            >
              Add
            </BaseButton>
          </div>
        </div>

        <!-- Action Items -->
        <div
          v-if="showActionItems"
          v-show="isColumnVisible('ActionItems')"
          :class="actionItemsColumnClasses"
          class="bg-surface border border-gray-200 rounded-[--radius-md] shadow-card flex flex-col min-h-0"
        >
          <div class="px-4 py-3 border-b bg-action-items-light rounded-t-[--radius-md] flex items-center gap-2">
            <ListChecks :size="18" class="text-action-items" />
            <h2 class="font-semibold text-action-items text-base flex-1">Action Items</h2>
            <BaseBadge variant="info">{{ actionItems.length }}</BaseBadge>
          </div>
          <TransitionGroup
            name="card-list"
            tag="div"
            class="flex-1 flex flex-col gap-2 p-3 overflow-y-auto min-h-0 relative"
          >
            <div v-if="actionItems.length === 0" key="empty" class="text-gray-400 text-sm text-center py-4">
              No action items yet
            </div>
            <CardItem
              v-for="card in actionItems"
              :key="card.id"
              :card="card"
              :can-edit="canMutateCard(card)"
              :blurred="isWritingPhase && card.participantId !== currentParticipantId"
              :can-attach-link="policyStore.isAllowed(currentPhase, 'AttachEvidenceLink')"
              column-color="action-items"
              @edit="saveEdit"
              @delete="requestDelete"
              @attach-evidence-link="attachEvidenceLink"
              @detach-evidence-link="detachEvidenceLink"
            />
          </TransitionGroup>
          <div class="p-3 border-t flex flex-col gap-2">
            <BaseInput
              v-model="newActionText"
              :maxlength="2000"
              placeholder="Action item..."
              @keydown.enter="addActionItem"
            />
            <select
              v-model="newActionAssignee"
              class="border border-gray-300 rounded-[--radius-md] px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500"
            >
              <option value="">Assign to...</option>
              <option v-for="p in participants" :key="p.id" :value="p.displayName">
                {{ p.displayName }}
              </option>
            </select>
            <BaseButton
              size="sm"
              variant="primary"
              :disabled="!newActionText.trim() || !newActionAssignee"
              @click="addActionItem"
            >
              Add
            </BaseButton>
          </div>
        </div>
      </div>
    </div>

    <!-- Delete confirmation modal -->
    <BaseModal :open="!!deleteTarget" title="Delete Card" @close="deleteTarget = null">
      <p class="text-sm text-gray-600">Are you sure you want to delete this card? This action cannot be undone.</p>
      <template #footer>
        <div class="flex justify-end gap-3">
          <BaseButton variant="secondary" @click="deleteTarget = null">Cancel</BaseButton>
          <BaseButton variant="danger" @click="executeDelete">Delete</BaseButton>
        </div>
      </template>
    </BaseModal>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch } from 'vue'
import { useRoute } from 'vue-router'
import { SmilePlus, Frown, ListChecks, RefreshCw } from 'lucide-vue-next'
import { useGuestStore } from '../stores/guest'
import { useParticipantStore } from '../stores/participant'
import { useCardStore, type Card, type EvidenceLink } from '../stores/card'
import { useAuthStore } from '../stores/auth'
import { useSprintHub } from '../composables/useSprintHub'
import { useVoteStore } from '../stores/vote'
import { useSprintStore } from '../stores/sprint'
import { usePolicyStore } from '../stores/policy'
import { useToast } from '../composables/useToast'
import CardItem from '../components/CardItem.vue'
import PhaseIndicator from '../components/PhaseIndicator.vue'
import BaseButton from '../components/ui/BaseButton.vue'
import BaseInput from '../components/ui/BaseInput.vue'
import BaseBadge from '../components/ui/BaseBadge.vue'
import BaseCard from '../components/ui/BaseCard.vue'
import BaseModal from '../components/ui/BaseModal.vue'
import LoadingSpinner from '../components/ui/LoadingSpinner.vue'

const route = useRoute()
const guestStore = useGuestStore()
const participantStore = useParticipantStore()
const cardStore = useCardStore()
const authStore = useAuthStore()
const voteStore = useVoteStore()
const sprintStore = useSprintStore()
const policyStore = usePolicyStore()
const { addToast } = useToast()
const kicked = ref(false)
const { connect, disconnect } = useSprintHub(() => {
  kicked.value = true
  guestStore.clearToken(sprintId)
  disconnect()
})

const sprintId = Number(route.params.sprintId)
const displayName = ref('')
const busy = ref(false)
const errorMessage = ref('')
const joined = ref(false)
const loading = ref(true)

const newCardText = ref({ WentWell: '', DidntGoWell: '' })
const newActionText = ref('')
const newActionAssignee = ref('')
const deleteTarget = ref<Card | null>(null)
const activeTab = ref<'WentWell' | 'DidntGoWell' | 'ActionItems'>('WentWell')
const isMobile = ref(typeof window !== 'undefined' ? window.innerWidth < 640 : false)

function onResize() {
  isMobile.value = window.innerWidth < 640
}

const participants = computed(() => participantStore.participants)
const wentWell = computed(() => cardStore.wentWell)
const didntGoWell = computed(() => cardStore.didntGoWell)
const actionItems = computed(() => cardStore.actionItems)
const sprintName = computed(() => sprintStore.currentSummary?.name ?? '')
const currentPhase = computed(() => sprintStore.currentSummary?.currentPhase ?? '')
const summaryParticipants = computed(() => sprintStore.currentSummary?.participants ?? [])
const currentParticipantId = computed(() => participantStore.currentParticipant?.id ?? 0)
const isWritingPhase = computed(() => currentPhase.value === 'Writing')
const showActionItems = computed(() => policyStore.isAllowed(currentPhase.value, 'CreateActionItem'))

const boardGridClasses = computed(() => {
  if (showActionItems.value) {
    return 'grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4 flex-1 min-h-0 grid-rows-[1fr]'
  }
  return 'grid grid-cols-1 sm:grid-cols-2 gap-4 flex-1 min-h-0 grid-rows-[1fr]'
})

const actionItemsColumnClasses = computed(() => {
  return 'sm:col-span-2 lg:col-span-1'
})

watch(showActionItems, (visible) => {
  if (!visible && activeTab.value === 'ActionItems') {
    activeTab.value = 'WentWell'
  }
})

function isColumnVisible(column: string): boolean {
  if (!isMobile.value) return true
  return activeTab.value === column
}

function canMutateCard(card: Card): boolean {
  const ownsCard = card.participantId === participantStore.currentParticipant?.id || authStore.user !== null
  if (!ownsCard) return false
  const phase = currentPhase.value
  const editAction = card.cardType === 'ActionItem' ? 'EditActionItem' : 'EditNote'
  return policyStore.isAllowed(phase, editAction)
}

function showToast(msg: string, variant: 'success' | 'error' | 'info' = 'error') {
  addToast(msg, variant)
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

function requestDelete(card: Card) {
  deleteTarget.value = card
}

async function executeDelete() {
  const card = deleteTarget.value
  if (!card) return
  deleteTarget.value = null
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
  window.addEventListener('resize', onResize)
  await policyStore.fetch()
  await sprintStore.fetchSprintSummary(sprintId)
  guestStore.loadToken(sprintId)
  if (guestStore.token) {
    try {
      await participantStore.fetchMe(sprintId)
      await enterRoom()
      return
    } catch (err: unknown) {
      const status = (err as { status?: number })?.status
      if (status === 401) guestStore.clearToken(sprintId)
    }
  }
  if (!authStore.loaded) await authStore.fetchMe()
  if (authStore.isSignedIn && authStore.user?.givenName) {
    try {
      const response = await participantStore.join(sprintId, authStore.user.givenName)
      guestStore.saveToken(sprintId, response.guestToken, response.displayName)
      await enterRoom()
      return
    } catch (err: unknown) {
      const status = (err as { status?: number })?.status
      if (status === 409) {
        errorMessage.value = `${authStore.user!.givenName} is already taken. Choose another.`
      } else {
        errorMessage.value = 'Failed to join. Please try again.'
      }
    }
  }
  loading.value = false
})

onUnmounted(() => {
  window.removeEventListener('resize', onResize)
  disconnect()
})

async function enterRoom() {
  await Promise.all([
    participantStore.fetchParticipants(sprintId),
    cardStore.fetchCards(sprintId),
  ])
  await voteStore.fetchMyVotes(sprintId)
  await connect(sprintId)
  joined.value = true
  loading.value = false
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
    if (status === 409) errorMessage.value = 'Name already taken. Choose another.'
    else if (status === 404) errorMessage.value = 'Sprint not found.'
    else errorMessage.value = 'Failed to join. Please try again.'
  } finally {
    busy.value = false
  }
}
</script>
