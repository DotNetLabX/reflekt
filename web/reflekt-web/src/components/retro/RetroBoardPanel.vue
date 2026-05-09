<template>
  <div class="flex flex-col gap-5 flex-1 min-h-0">
    <RetroMobileTabBar v-model="activeTab" :show-action-items="showActionItems" />

    <div :class="boardGridClasses">
      <!-- Went Well column -->
      <RetroColumnBody
        title="Went Well"
        column-color="went-well"
        :header-icon="SmilePlus"
        badge-variant="success"
        :count="showGroups ? wentWellGroups.length : wentWell.length"
        :visible="isColumnVisible('WentWell')"
        @drop="onColumnDrop($event, 'WentWell')"
      >
        <template #empty>
          <div v-if="showGroups && wentWellGroups.length === 0" class="text-gray-400 text-sm text-center py-4">No groups yet</div>
          <div v-else-if="!showGroups && wentWell.length === 0" class="text-gray-400 text-sm text-center py-4">No cards yet</div>
        </template>
        <template v-if="showGroups">
          <RetroGroupItem v-for="group in wentWellGroups" :key="'g' + group.id" v-bind="groupItemProps(group, 'went-well')"
            @vote="castGroupVote" @retract-vote="retractGroupVote" @dissolve="dissolveGroup"
            @update-title="updateGroupTitle" @merge="mergeGroups" @remove-card="removeCardFromGroup"
            @focus="focusGroup" @toggle-discussed="toggleDiscussed" />
        </template>
        <template v-else>
          <RetroCardItem v-for="card in wentWell" :key="card.id" v-bind="cardItemProps(card, 'went-well')"
            @edit="saveEdit" @delete="requestDelete" @vote="castVote" @retract-vote="retractVote"
            @attach-evidence-link="attachEvidenceLink" @detach-evidence-link="detachEvidenceLink" />
        </template>
        <template #input>
          <div v-if="policyStore.isAllowed(currentPhase, 'CreateNote')" class="flex gap-2">
            <BaseInput v-model="newCardText.WentWell" :maxlength="2000" placeholder="Add a note..." class="flex-1" @keydown.enter="onAddNote('WentWell')" />
            <BaseButton size="sm" variant="primary" :disabled="!newCardText.WentWell.trim()" @click="onAddNote('WentWell')">Add</BaseButton>
          </div>
        </template>
      </RetroColumnBody>

      <!-- Didn't Go Well column -->
      <RetroColumnBody
        title="Didn't Go Well"
        column-color="didnt-go-well"
        :header-icon="Frown"
        badge-variant="danger"
        :count="showGroups ? didntGoWellGroups.length : didntGoWell.length"
        :visible="isColumnVisible('DidntGoWell')"
        @drop="onColumnDrop($event, 'DidntGoWell')"
      >
        <template #empty>
          <div v-if="showGroups && didntGoWellGroups.length === 0" class="text-gray-400 text-sm text-center py-4">No groups yet</div>
          <div v-else-if="!showGroups && didntGoWell.length === 0" class="text-gray-400 text-sm text-center py-4">No cards yet</div>
        </template>
        <template v-if="showGroups">
          <RetroGroupItem v-for="group in didntGoWellGroups" :key="'g' + group.id" v-bind="groupItemProps(group, 'didnt-go-well')"
            @vote="castGroupVote" @retract-vote="retractGroupVote" @dissolve="dissolveGroup"
            @update-title="updateGroupTitle" @merge="mergeGroups" @remove-card="removeCardFromGroup"
            @focus="focusGroup" @toggle-discussed="toggleDiscussed" />
        </template>
        <template v-else>
          <RetroCardItem v-for="card in didntGoWell" :key="card.id" v-bind="cardItemProps(card, 'didnt-go-well')"
            @edit="saveEdit" @delete="requestDelete" @vote="castVote" @retract-vote="retractVote"
            @attach-evidence-link="attachEvidenceLink" @detach-evidence-link="detachEvidenceLink" />
        </template>
        <template #input>
          <div v-if="policyStore.isAllowed(currentPhase, 'CreateNote')" class="flex gap-2">
            <BaseInput v-model="newCardText.DidntGoWell" :maxlength="2000" placeholder="Add a note..." class="flex-1" @keydown.enter="onAddNote('DidntGoWell')" />
            <BaseButton size="sm" variant="primary" :disabled="!newCardText.DidntGoWell.trim()" @click="onAddNote('DidntGoWell')">Add</BaseButton>
          </div>
        </template>
      </RetroColumnBody>

      <!-- Action Items column -->
      <RetroColumnBody
        v-if="showActionItems"
        title="Action Items"
        column-color="action-items"
        :header-icon="ListChecks"
        badge-variant="info"
        :count="actionItems.length"
        :visible="isColumnVisible('ActionItems')"
        extra-class="sm:col-span-2 lg:col-span-1"
        @drop="() => {}"
      >
        <template #empty>
          <div v-if="actionItems.length === 0" class="text-gray-400 text-sm text-center py-4">No action items yet</div>
        </template>
        <RetroCardItem v-for="card in actionItems" :key="card.id"
          :card="card" :can-edit="canMutateCard(card)"
          :blurred="isWritingPhase && card.participantId !== currentParticipantId"
          :can-attach-link="policyStore.isAllowed(currentPhase, 'AttachEvidenceLink')"
          :participants="participants" column-color="action-items"
          @edit="saveEdit" @delete="requestDelete"
          @attach-evidence-link="attachEvidenceLink" @detach-evidence-link="detachEvidenceLink" />
        <template #input>
          <div v-if="policyStore.isAllowed(currentPhase, 'CreateActionItem')" class="flex flex-col gap-2">
            <BaseInput v-model="newActionText" :maxlength="2000" placeholder="Action item..." @keydown.enter="onAddActionItem" />
            <select v-model="newActionAssignee" class="border border-gray-300 rounded-[--radius-md] px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500">
              <option value="">Assign to...</option>
              <option v-for="p in participants" :key="p.id" :value="p.displayName">{{ p.displayName }}</option>
            </select>
            <BaseButton size="sm" variant="primary" :disabled="!newActionText.trim() || !newActionAssignee" @click="onAddActionItem">Add</BaseButton>
          </div>
        </template>
      </RetroColumnBody>
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
import { ref, computed, watch, onMounted, onUnmounted } from 'vue'
import { SmilePlus, Frown, ListChecks } from 'lucide-vue-next'
import { type Card } from '../../stores/card'
import { useParticipantStore } from '../../stores/participant'
import { usePolicyStore } from '../../stores/policy'
import { useToast } from '../../composables/useToast'
import { useRetroBoardActions } from '../../composables/useRetroBoardActions'
import RetroCardItem from './RetroCardItem.vue'
import RetroGroupItem from './RetroGroupItem.vue'
import RetroColumnBody from './RetroColumnBody.vue'
import RetroMobileTabBar from './RetroMobileTabBar.vue'
import BaseInput from '../ui/BaseInput.vue'
import BaseButton from '../ui/BaseButton.vue'
import BaseModal from '../ui/BaseModal.vue'

const props = defineProps<{ sprintId: number }>()

const participantStore = useParticipantStore()
const policyStore = usePolicyStore()
const { addToast } = useToast()

const newCardText = ref({ WentWell: '', DidntGoWell: '' })
const newActionText = ref('')
const newActionAssignee = ref('')
const deleteTarget = ref<Card | null>(null)
const activeTab = ref<'WentWell' | 'DidntGoWell' | 'ActionItems'>('WentWell')
const isMobile = ref(typeof window !== 'undefined' ? window.innerWidth < 640 : false)

function onResize() { isMobile.value = window.innerWidth < 640 }
onMounted(() => window.addEventListener('resize', onResize))
onUnmounted(() => window.removeEventListener('resize', onResize))

const {
  participants, wentWell, didntGoWell, actionItems, currentPhase, isWritingPhase, showGroups,
  wentWellGroups, didntGoWellGroups, showActionItems, boardGridClasses, cardItemProps, groupItemProps, canMutateCard,
  addNote, addActionItem, saveEdit, deleteCard, castVote, retractVote, castGroupVote, retractGroupVote,
  attachEvidenceLink, detachEvidenceLink, focusGroup, toggleDiscussed, dissolveGroup, updateGroupTitle, mergeGroups, removeCardFromGroup,
} = useRetroBoardActions(props.sprintId, (msg) => addToast(msg, 'error'))

const currentParticipantId = computed(() => participantStore.currentParticipant?.id ?? 0)

watch(showActionItems, (visible) => { if (!visible && activeTab.value === 'ActionItems') activeTab.value = 'WentWell' })

function isColumnVisible(column: string): boolean { return !isMobile.value || activeTab.value === column }

async function onAddNote(column: 'WentWell' | 'DidntGoWell') {
  const text = newCardText.value[column].trim()
  if (!text) return
  newCardText.value[column] = ''
  await addNote(column, text)
}

async function onAddActionItem() {
  const text = newActionText.value.trim()
  const assigneeName = newActionAssignee.value
  if (!text || !assigneeName) return
  newActionText.value = ''; newActionAssignee.value = ''
  await addActionItem(text, assigneeName)
}

function requestDelete(card: Card) { deleteTarget.value = card }

async function executeDelete() {
  const card = deleteTarget.value
  if (!card) return
  deleteTarget.value = null
  await deleteCard(card)
}

function onColumnDrop(e: DragEvent, column: string) {
  e.preventDefault()
  const raw = e.dataTransfer?.getData('application/json')
  if (!raw) return
  let payload: { type: string; columnType: string; sourceGroupId?: number; cardId?: number }
  try { payload = JSON.parse(raw) } catch { return }
  if (payload.type !== 'card' || payload.columnType !== column) return
  if (!payload.sourceGroupId || !payload.cardId) return
  removeCardFromGroup(payload.sourceGroupId, payload.cardId)
}
</script>
