import { defineStore } from 'pinia'
import { ref } from 'vue'
import { readFlag, STORAGE_KEYS, writeString } from '@/storage/storage'
import type { TimerPayload, TimerStatus } from '@/types/timer'

export const useTimerStore = defineStore('timer', () => {
  const timerStatus = ref<TimerStatus>('stopped')
  const remainingSeconds = ref(0)
  const endTimeUtc = ref<string | null>(null)
  const isMuted = ref(readFlag(STORAGE_KEYS.timerMuted))

  function applyTimer(payload: TimerPayload) {
    timerStatus.value = payload.status
    remainingSeconds.value = payload.remainingSeconds
    endTimeUtc.value = payload.endTimeUtc
  }

  function toggleMute() {
    isMuted.value = !isMuted.value
    writeString(STORAGE_KEYS.timerMuted, String(isMuted.value))
  }

  return { timerStatus, remainingSeconds, endTimeUtc, isMuted, applyTimer, toggleMute }
})
