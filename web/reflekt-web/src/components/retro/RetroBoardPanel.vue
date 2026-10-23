<template>
  <div class="flex flex-col gap-5 flex-1 min-h-0">
    <RetroReadyButton v-if="showReadiness" :sprint-id="sprintId" />

    <!-- Previous actions review panel (Writing phase only) -->
    <PreviousActionsPanel
      v-if="showPreviousActions"
      :sprint-id="sprintId"
      :team-id="teamId"
      :is-scrum-master="isScrumMaster"
    />

    <RetroMobileTabBar
      v-model="activeTab"
      :show-action-items="showActionItems"
      :column1-name="noteColumns[0].name"
      :column1-color="noteColumns[0].color"
      :column2-name="noteColumns[1].name"
      :column2-color="noteColumns[1].color"
      :column3-name="noteColumns[2].name"
      :column3-color="noteColumns[2].color"
    />

    <div :class="boardGridClasses">
      <RetroColumnPanel
        v-for="column in noteColumns"
        :key="column.type"
        :column="column.type"
        :title="column.name"
        :column-color="column.color"
        :badge-variant="column.badge"
        :count="showGroups ? column.groups.length : column.cards.length"
        :mobile-hidden="activeTab !== column.type"
        :is-scrum-master="isScrumMaster"
      >
        <template #empty>
          <div
            v-if="showGroups && column.groups.length === 0"
            class="text-text-muted text-sm text-center py-4"
          >
            No groups yet
          </div>
          <div
            v-else-if="!showGroups && column.cards.length === 0"
            class="text-text-muted text-sm text-center py-4"
          >
            No cards yet
          </div>
        </template>
        <template v-if="showGroups">
          <RetroGroupItem
            v-for="group in column.groups"
            :key="'g' + group.id"
            v-bind="groupItemProps(group, column.color)"
          />
        </template>
        <template v-else>
          <RetroCardItem
            v-for="card in column.cards"
            :key="card.id"
            v-bind="cardItemProps(card, column.color)"
          />
        </template>
        <template #input>
          <RetroNoteInput v-if="canCreateNote" :column="column.type" />
        </template>
      </RetroColumnPanel>

      <!-- Action Items column -->
      <RetroColumnPanel
        v-if="showActionItems"
        column="ActionItems"
        title="Action Items"
        column-color="blue"
        badge-variant="info"
        :count="actionItems.length"
        :mobile-hidden="activeTab !== 'ActionItems'"
        :is-scrum-master="false"
      >
        <template #empty>
          <div v-if="actionItems.length === 0" class="text-text-muted text-sm text-center py-4">
            No action items yet
          </div>
        </template>
        <RetroCardItem
          v-for="card in actionItems"
          :key="card.id"
          v-bind="cardItemProps(card, 'blue')"
          :participants="participants"
        />
        <template #input>
          <RetroActionItemInput v-if="canCreateActionItem" :participants="participants" />
        </template>
      </RetroColumnPanel>
    </div>

    <!-- Delete confirmation modal -->
    <BaseModal :open="!!deleteTarget" title="Delete Card" @close="cancelDelete">
      <p class="text-sm text-text-secondary">
        Are you sure you want to delete this card? This action cannot be undone.
      </p>
      <template #footer>
        <div class="flex justify-end gap-3">
          <BaseButton variant="secondary" @click="cancelDelete">Cancel</BaseButton>
          <BaseButton variant="danger" @click="confirmDelete">Delete</BaseButton>
        </div>
      </template>
    </BaseModal>
  </div>
</template>

<script setup lang="ts">
import { ref, watch, provide } from 'vue'
import type { ColumnType } from '@/types/domain'
import { useToast } from '@/composables/useToast'
import { boardActionsKey, useRetroBoardActions } from '@/composables/useRetroBoardActions'
import RetroCardItem from './RetroCardItem.vue'
import RetroGroupItem from './RetroGroupItem.vue'
import RetroColumnPanel from './RetroColumnPanel.vue'
import RetroMobileTabBar from './RetroMobileTabBar.vue'
import RetroNoteInput from './RetroNoteInput.vue'
import RetroActionItemInput from './RetroActionItemInput.vue'
import RetroReadyButton from './RetroReadyButton.vue'
import PreviousActionsPanel from './PreviousActionsPanel.vue'
import BaseButton from '@/components/ui/BaseButton.vue'
import BaseModal from '@/components/ui/BaseModal.vue'

const props = defineProps<{
  sprintId: number
}>()

const { addToast } = useToast()

const activeTab = ref<ColumnType>('Column1')

const {
  participants,
  actionItems,
  noteColumns,
  showGroups,
  isScrumMaster,
  canCreateNote,
  canCreateActionItem,
  showActionItems,
  showPreviousActions,
  showReadiness,
  teamId,
  boardGridClasses,
  deleteTarget,
  cardItemProps,
  groupItemProps,
  actions,
  cancelDelete,
  confirmDelete,
} = useRetroBoardActions(props.sprintId, (msg) => addToast(msg, 'error'))

provide(boardActionsKey, actions)

watch(showActionItems, (visible) => {
  if (!visible && activeTab.value === 'ActionItems') activeTab.value = 'Column1'
})
</script>
