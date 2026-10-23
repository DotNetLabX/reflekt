<template>
  <div class="flex items-center gap-2">
    <!-- Timer area — clickable for SM to open controls; the trigger button's click bubbles here -->
    <div
      class="relative flex items-center gap-1.5 rounded px-1.5 py-0.5 transition-colors"
      :class="isScrumMaster ? 'cursor-pointer hover:bg-surface-raised' : ''"
      @click="isScrumMaster ? (showControls = !showControls) : undefined"
    >
      <component
        :is="isScrumMaster ? 'button' : 'div'"
        class="flex items-center gap-1.5"
        v-bind="isScrumMaster ? controlsTrigger : {}"
      >
        <!-- Timer text -->
        <span
          class="font-mono text-sm font-semibold tabular-nums select-none"
          :class="[
            timerStatus === 'expired'
              ? 'text-danger animate-blink'
              : timerStatus === 'running'
                ? 'text-text-primary'
                : 'text-text-muted',
          ]"
        >
          {{ timerStatus === 'stopped' ? phaseDefaultFormatted : formattedTime }}
        </span>
        <!-- SM dropdown indicator -->
        <ChevronDown v-if="isScrumMaster" :size="12" class="text-text-muted" />
      </component>

      <!-- Mute toggle (visible to all when timer is not stopped) -->
      <button
        v-if="timerStatus !== 'stopped'"
        class="text-text-muted hover:text-text-secondary transition-colors cursor-pointer"
        :title="isMuted ? 'Unmute timer' : 'Mute timer'"
        :aria-label="isMuted ? 'Unmute timer' : 'Mute timer'"
        @click.stop="toggleMute"
      >
        <VolumeX v-if="isMuted" :size="16" />
        <Volume2 v-else :size="16" />
      </button>

      <!-- SM controls popover -->
      <TimerControls
        v-if="isScrumMaster && showControls"
        :sprint-id="sprintId"
        :timer-status="timerStatus"
        @close="showControls = false"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { ChevronDown, Volume2, VolumeX } from 'lucide-vue-next'
import { useRetroPhase } from '@/composables/useRetroPhase'
import { useTimer } from '@/composables/useTimer'
import { formatMmSs } from '@/format/format'
import TimerControls from './TimerControls.vue'

defineProps<{
  sprintId: number
}>()

const showControls = ref(false)

const { isScrumMaster, timerDefaultMinutes } = useRetroPhase()
const { timerStatus, formattedTime, isMuted, toggleMute } = useTimer()

const phaseDefaultFormatted = computed(() => formatMmSs(timerDefaultMinutes.value * 60))

const controlsTrigger = computed(() => ({
  type: 'button',
  class: 'cursor-pointer',
  'aria-label': `Timer controls, ${timerStatus.value === 'stopped' ? phaseDefaultFormatted.value : formattedTime.value}`,
  'aria-haspopup': 'true',
  'aria-expanded': showControls.value,
}))
</script>
