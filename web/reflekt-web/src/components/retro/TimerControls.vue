<template>
  <div
    class="absolute right-0 top-full mt-1 z-30 bg-surface-raised border border-border rounded-sm shadow-lg p-2 flex flex-col gap-1.5 min-w-[140px]"
    @click.stop
  >
    <button v-if="timerStatus === 'running'" :class="secondaryClass" @click="invoke('PauseTimer')">
      Pause
    </button>
    <button v-if="timerStatus === 'paused'" :class="primaryClass" @click="invoke('ResumeTimer')">
      Resume
    </button>
    <template v-if="hasControls">
      <button :class="secondaryClass" @click="invoke('ResetTimer')">Reset</button>
      <div class="flex gap-1 mt-0.5">
        <button
          v-for="minutes in ADD_MINUTES"
          :key="minutes"
          class="flex-1 px-1 py-1 text-xs font-medium rounded-sm bg-surface-raised hover:bg-border text-text-secondary transition-colors cursor-pointer text-center"
          @click="invokeAddTime(minutes)"
        >
          +{{ minutes }}m
        </button>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useTimerActions } from '@/composables/useTimerActions'
import type { TimerMethod, TimerStatus } from '@/types/timer'

const props = defineProps<{
  sprintId: number
  timerStatus: TimerStatus
}>()

const emit = defineEmits<{
  close: []
}>()

const { runTimerAction, addTime } = useTimerActions()

const ADD_MINUTES = [1, 2, 5]
const primaryClass =
  'w-full px-3 py-1 text-xs font-medium rounded-sm transition-colors cursor-pointer text-center bg-primary-600 hover:bg-primary-700 text-white'
const secondaryClass =
  'w-full px-3 py-1 text-xs font-medium rounded-sm transition-colors cursor-pointer text-center bg-surface-raised hover:bg-border text-text-secondary'

const hasControls = computed(() => props.timerStatus !== 'stopped')

async function invoke(method: TimerMethod) {
  if (await runTimerAction(method, props.sprintId)) emit('close')
}

async function invokeAddTime(minutes: number) {
  if (await addTime(props.sprintId, minutes)) emit('close')
}
</script>
