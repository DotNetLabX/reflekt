<template>
  <div
    class="absolute right-0 top-full mt-1 z-30 bg-white border border-gray-200 rounded-[--radius-sm] shadow-lg p-2 flex flex-col gap-1.5 min-w-[140px]"
    @click.stop
  >
    <!-- Stopped: Start -->
    <template v-if="timerStatus === 'stopped'">
      <button class="timer-btn timer-btn-primary" @click="invoke('StartTimer')">Start</button>
    </template>

    <!-- Running: Pause, Reset, +time -->
    <template v-else-if="timerStatus === 'running'">
      <button class="timer-btn timer-btn-secondary" @click="invoke('PauseTimer')">Pause</button>
      <button class="timer-btn timer-btn-secondary" @click="invoke('ResetTimer')">Reset</button>
      <div class="flex gap-1 mt-0.5">
        <button class="timer-btn-add" @click="invokeAddTime(1)">+1m</button>
        <button class="timer-btn-add" @click="invokeAddTime(2)">+2m</button>
        <button class="timer-btn-add" @click="invokeAddTime(5)">+5m</button>
      </div>
    </template>

    <!-- Paused: Resume, Reset, +time -->
    <template v-else-if="timerStatus === 'paused'">
      <button class="timer-btn timer-btn-primary" @click="invoke('ResumeTimer')">Resume</button>
      <button class="timer-btn timer-btn-secondary" @click="invoke('ResetTimer')">Reset</button>
      <div class="flex gap-1 mt-0.5">
        <button class="timer-btn-add" @click="invokeAddTime(1)">+1m</button>
        <button class="timer-btn-add" @click="invokeAddTime(2)">+2m</button>
        <button class="timer-btn-add" @click="invokeAddTime(5)">+5m</button>
      </div>
    </template>

    <!-- Expired: Reset, +time -->
    <template v-else-if="timerStatus === 'expired'">
      <button class="timer-btn timer-btn-secondary" @click="invoke('ResetTimer')">Reset</button>
      <div class="flex gap-1 mt-0.5">
        <button class="timer-btn-add" @click="invokeAddTime(1)">+1m</button>
        <button class="timer-btn-add" @click="invokeAddTime(2)">+2m</button>
        <button class="timer-btn-add" @click="invokeAddTime(5)">+5m</button>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { getHubConnection } from '../../composables/useSprintHub'
import type { TimerStatus } from '../../composables/useTimer'

const props = defineProps<{
  sprintId: number
  timerStatus: TimerStatus
}>()

const emit = defineEmits<{
  close: []
}>()

async function invoke(method: string) {
  const conn = getHubConnection()
  if (!conn) return
  try {
    await conn.invoke(method, props.sprintId)
  } catch (e) {
    console.error(`Timer hub method ${method} failed:`, e)
  }
  emit('close')
}

async function invokeAddTime(minutes: number) {
  const conn = getHubConnection()
  if (!conn) return
  try {
    await conn.invoke('AddTime', props.sprintId, minutes)
  } catch (e) {
    console.error('AddTime hub method failed:', e)
  }
  emit('close')
}
</script>

<style scoped>
.timer-btn {
  @apply w-full px-3 py-1 text-xs font-medium rounded-[--radius-sm] transition-colors cursor-pointer text-center;
}
.timer-btn-primary {
  @apply bg-primary-600 hover:bg-primary-700 text-white;
}
.timer-btn-secondary {
  @apply bg-gray-100 hover:bg-gray-200 text-gray-700;
}
.timer-btn-add {
  @apply flex-1 px-1 py-1 text-xs font-medium rounded-[--radius-sm] bg-gray-100 hover:bg-gray-200 text-gray-700 transition-colors cursor-pointer text-center;
}
</style>
