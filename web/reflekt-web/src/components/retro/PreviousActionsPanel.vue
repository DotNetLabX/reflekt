<template>
  <div class="bg-white border border-amber-200 rounded-[--radius-md] shadow-sm">
    <!-- Header row -->
    <div
      class="flex items-center justify-between px-4 py-3 cursor-pointer select-none bg-amber-50 rounded-t-[--radius-md]"
      :title="sectionTooltip"
      @click="toggleCollapsed"
    >
      <h2 class="text-sm font-semibold text-amber-800">
        Actions from last sprint ({{ totalCount }})
      </h2>
      <span class="text-amber-600 text-xs">{{ isCollapsed ? '▶ Expand' : '▼ Collapse' }}</span>
    </div>

    <!-- Auto-collapse summary line -->
    <div
      v-if="isCollapsed && allReviewed"
      class="px-4 py-2 text-sm text-gray-600 italic cursor-pointer"
      :title="summaryTooltip"
      @click="toggleCollapsed"
    >
      All {{ totalCount }} actions reviewed ({{ summaryCounts.done }} done, {{ summaryCounts.carriedOver }} carried over, {{ summaryCounts.dropped }} dropped)
    </div>

    <!-- Items list -->
    <div v-if="!isCollapsed" class="divide-y divide-gray-100">
      <div v-for="item in items" :key="item.cardId" class="flex items-start justify-between gap-4 px-4 py-3">
        <!-- Text + assignee -->
        <div class="flex-1 min-w-0">
          <p class="text-sm text-gray-800">{{ item.text }}</p>
          <p v-if="item.assigneeName" class="text-xs text-gray-500 mt-0.5">@{{ item.assigneeName }}</p>
        </div>

        <!-- SM: status buttons -->
        <div v-if="isScrumMaster" class="flex gap-1 shrink-0">
          <button
            :class="statusButtonClass(item, 'Done')"
            :title="doneTooltip"
            @click="setStatus(item, 'Done')"
          >Done</button>
          <button
            :class="statusButtonClass(item, 'CarriedOver')"
            :title="carriedOverTooltip"
            @click="setStatus(item, 'CarriedOver')"
          >Carried Over</button>
          <button
            :class="statusButtonClass(item, 'Dropped')"
            :title="droppedTooltip"
            @click="setStatus(item, 'Dropped')"
          >Dropped</button>
        </div>

        <!-- Participant: read-only badge -->
        <div v-else class="shrink-0">
          <span v-if="item.reviewStatus !== 'None'" :class="statusBadgeClass(item.reviewStatus)" class="px-2 py-0.5 rounded text-xs font-medium">
            {{ formatStatus(item.reviewStatus) }}
          </span>
          <span v-else class="px-2 py-0.5 rounded text-xs font-medium bg-gray-100 text-gray-500">
            Pending
          </span>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import { storeToRefs } from 'pinia'
import { usePreviousActionsStore, type PreviousActionItem } from '../../stores/previousActions'
import { useGuestStore } from '../../stores/guest'

const props = defineProps<{
  sprintId: number
  teamId: number
  isScrumMaster: boolean
}>()

const store = usePreviousActionsStore()
const { items, totalCount, allReviewed, summaryCounts } = storeToRefs(store)

const guestStore = useGuestStore()

const isCollapsed = ref(false)

// Auto-collapse when all reviewed
watch(allReviewed, (val) => {
  if (val) isCollapsed.value = true
})

function toggleCollapsed() {
  isCollapsed.value = !isCollapsed.value
}

// Tooltip text from help.tooltips.md
const sectionTooltip = "Review action items from your team's previous retro. See what was committed and track follow-through."
const doneTooltip = "Mark this action item as completed since the last retro."
const carriedOverTooltip = "Mark this action item as still in progress. Create a new action item later if needed."
const droppedTooltip = "Mark this action item as no longer relevant or needed."
const summaryTooltip = "All previous action items have been reviewed. Expand to revisit or change any status."

async function setStatus(item: PreviousActionItem, status: string) {
  const previousStatus = item.reviewStatus

  // Optimistic update
  item.reviewStatus = status

  const headers: Record<string, string> = { 'Content-Type': 'application/json' }
  if (guestStore.token) headers['X-Guest-Token'] = guestStore.token

  const res = await fetch(
    `/api/teams/${props.teamId}/sprints/${props.sprintId}/previous-actions/${item.cardId}`,
    {
      method: 'PATCH',
      headers,
      credentials: 'include',
      body: JSON.stringify({ reviewStatus: status }),
    },
  )

  if (!res.ok) {
    // Revert on failure
    item.reviewStatus = previousStatus
  }
}

function statusButtonClass(item: PreviousActionItem, status: string): string {
  const isActive = item.reviewStatus === status
  const base = 'px-2 py-1 text-xs font-medium rounded border cursor-pointer transition-colors'
  if (isActive) {
    if (status === 'Done') return `${base} bg-emerald-600 border-emerald-600 text-white`
    if (status === 'CarriedOver') return `${base} bg-amber-500 border-amber-500 text-white`
    if (status === 'Dropped') return `${base} bg-gray-500 border-gray-500 text-white`
  }
  return `${base} bg-white border-gray-300 text-gray-600 hover:bg-gray-50`
}

function statusBadgeClass(status: string): string {
  if (status === 'Done') return 'bg-emerald-100 text-emerald-700'
  if (status === 'CarriedOver') return 'bg-amber-100 text-amber-700'
  if (status === 'Dropped') return 'bg-gray-100 text-gray-600'
  return 'bg-gray-100 text-gray-500'
}

function formatStatus(status: string): string {
  if (status === 'CarriedOver') return 'Carried Over'
  return status
}
</script>
