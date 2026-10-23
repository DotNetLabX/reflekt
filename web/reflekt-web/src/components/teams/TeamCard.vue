<template>
  <BaseCard
    padding="none"
    radius="lg"
    class="relative hover:shadow-card-hover hover:bg-surface-raised transition-all cursor-pointer"
    @click="$emit('click')"
  >
    <div class="p-5">
      <!-- Three-dot menu -->
      <div class="absolute top-3 right-3 z-10" @click.stop>
        <div class="relative group">
          <button
            type="button"
            class="p-1 rounded hover:bg-surface-raised text-text-muted hover:text-text-secondary"
            title="Manage this team. Options include deleting the team."
            :aria-label="`Manage team ${team.name}`"
            :aria-expanded="menuOpen"
            @click="menuOpen = !menuOpen"
          >
            <EllipsisVertical :size="20" />
          </button>
          <div
            v-if="menuOpen"
            ref="menuRef"
            class="absolute right-0 mt-1 w-36 bg-surface-raised border border-border rounded shadow-lg z-20"
          >
            <button
              type="button"
              class="w-full text-left px-3 py-2 text-sm text-danger hover:bg-badge-danger-bg"
              @click="handleDelete"
            >
              Delete team
            </button>
          </div>
        </div>
      </div>

      <!-- Team name: its overlay covers the card; its click, and the info buttons', bubble to
           the card's handler -->
      <h3 class="text-base font-semibold text-text-primary pr-8 mb-3">
        <button
          type="button"
          class="text-left cursor-pointer after:absolute after:inset-0 after:rounded-lg focus-visible:outline-none focus-visible:after:ring-2 focus-visible:after:ring-primary-500"
        >
          {{ team.name }}
        </button>
      </h3>

      <!-- Sprint count -->
      <div class="flex items-center gap-1 text-sm text-text-muted mb-1">
        <span>{{ team.sprintCount }} sprint{{ team.sprintCount !== 1 ? 's' : '' }}</span>
        <BaseTooltip
          v-slot="{ describedby }"
          class="z-10"
          text="The total number of sprints created under this team, including draft, active, and closed."
          panel-class="left-1/2 -translate-x-1/2 bottom-5 w-64 whitespace-normal"
        >
          <button
            type="button"
            class="flex text-text-muted cursor-help"
            aria-label="About the sprint count"
            :aria-describedby="describedby"
          >
            <Info :size="14" />
          </button>
        </BaseTooltip>
      </div>

      <!-- Last sprint date -->
      <div class="flex items-center gap-1 text-sm text-text-muted">
        <span v-if="team.lastSprintDate"
          >Last sprint {{ formatShortDate(team.lastSprintDate) }}</span
        >
        <span v-else>No sprints yet</span>
        <BaseTooltip
          v-if="team.lastSprintDate"
          v-slot="{ describedby }"
          class="z-10"
          text="The date when the most recent sprint was created in this team."
          panel-class="left-1/2 -translate-x-1/2 bottom-5 w-64 whitespace-normal"
        >
          <button
            type="button"
            class="flex text-text-muted cursor-help"
            aria-label="About the last sprint date"
            :aria-describedby="describedby"
          >
            <Info :size="14" />
          </button>
        </BaseTooltip>
      </div>
    </div>
  </BaseCard>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import { EllipsisVertical, Info } from 'lucide-vue-next'
import BaseCard from '@/components/ui/BaseCard.vue'
import BaseTooltip from '@/components/ui/BaseTooltip.vue'
import { formatShortDate } from '@/format/format'
import type { TeamSummary } from '@/types/team'

const props = defineProps<{ team: TeamSummary }>()
const emit = defineEmits<{
  click: []
  delete: [teamId: number]
}>()

const menuOpen = ref(false)
const menuRef = ref<HTMLElement | null>(null)

function handleDelete() {
  menuOpen.value = false
  emit('delete', props.team.id)
}

function handleClickOutside(e: MouseEvent) {
  if (menuOpen.value && menuRef.value && !menuRef.value.contains(e.target as Node)) {
    menuOpen.value = false
  }
}

onMounted(() => document.addEventListener('click', handleClickOutside))
onUnmounted(() => document.removeEventListener('click', handleClickOutside))
</script>
