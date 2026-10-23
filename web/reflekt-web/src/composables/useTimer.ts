import { ref, computed, watch, onUnmounted } from 'vue'
import { useTimerStore } from '../stores/timer'

export type TimerStatus = 'running' | 'paused' | 'expired' | 'stopped'

export interface TimerPayload {
  status: TimerStatus
  remainingSeconds: number
  endTimeUtc: string | null
}

let audioContext: AudioContext | null = null

export function unlockChime() {
  if (typeof window.AudioContext === 'undefined') return
  audioContext ??= new window.AudioContext()
  if (audioContext.state === 'suspended') audioContext.resume().catch(() => {})
}

function playChime() {
  const ctx = audioContext
  if (!ctx || ctx.state !== 'running') return
  const start = ctx.currentTime
  const oscillator = ctx.createOscillator()
  const gain = ctx.createGain()
  oscillator.frequency.value = 440
  gain.gain.setValueAtTime(0, start)
  gain.gain.linearRampToValueAtTime(0.3, start + 0.02)
  gain.gain.linearRampToValueAtTime(0, start + 0.3)
  oscillator.connect(gain).connect(ctx.destination)
  oscillator.start(start)
  oscillator.stop(start + 0.3)
}

export function useTimer() {
  const timerStore = useTimerStore()

  // Local countdown display — computed from store's endTimeUtc while running
  const displayRemainingSeconds = ref(timerStore.remainingSeconds)
  const isFlashing = ref(false)

  let intervalId: ReturnType<typeof setInterval> | null = null
  let flashTimeout: ReturnType<typeof setTimeout> | null = null

  const isMuted = computed(() => timerStore.isMuted)

  const formattedTime = computed<string>(() => {
    const total = Math.max(0, displayRemainingSeconds.value)
    const mins = Math.floor(total / 60)
    const secs = total % 60
    return `${String(mins).padStart(2, '0')}:${String(secs).padStart(2, '0')}`
  })

  function startInterval() {
    stopInterval()
    intervalId = setInterval(() => {
      const endTime = timerStore.endTimeUtc
      if (!endTime || timerStore.timerStatus !== 'running') {
        stopInterval()
        return
      }
      const diff = Math.floor((new Date(endTime).getTime() - Date.now()) / 1000)
      displayRemainingSeconds.value = Math.max(0, diff)
    }, 1000)
  }

  function stopInterval() {
    if (intervalId !== null) {
      clearInterval(intervalId)
      intervalId = null
    }
  }

  function triggerExpiry() {
    isFlashing.value = true
    if (!timerStore.isMuted) playChime()
    if (flashTimeout !== null) clearTimeout(flashTimeout)
    flashTimeout = setTimeout(() => {
      isFlashing.value = false
      flashTimeout = null
    }, 3000)
  }

  // Watch store status changes to start/stop interval and trigger effects
  watch(
    () => timerStore.timerStatus,
    (status, prevStatus) => {
      if (status === 'running') {
        displayRemainingSeconds.value = timerStore.remainingSeconds
        startInterval()
      } else {
        stopInterval()
        displayRemainingSeconds.value = timerStore.remainingSeconds
      }

      if (status === 'expired' && prevStatus !== 'expired') {
        displayRemainingSeconds.value = 0
        triggerExpiry()
      }

      if (status === 'stopped') {
        isFlashing.value = false
      }
    },
    { immediate: true },
  )

  // Keep displayRemainingSeconds in sync when store updates while paused/stopped
  watch(
    () => timerStore.remainingSeconds,
    (val) => {
      if (timerStore.timerStatus !== 'running') {
        displayRemainingSeconds.value = val
      }
    },
  )

  function toggleMute() {
    unlockChime()
    timerStore.toggleMute()
  }

  onUnmounted(() => {
    stopInterval()
    if (flashTimeout !== null) clearTimeout(flashTimeout)
  })

  return {
    timerStatus: computed(() => timerStore.timerStatus),
    displayRemainingSeconds,
    isFlashing,
    isMuted,
    formattedTime,
    isRunning: computed(() => timerStore.isRunning),
    isExpired: computed(() => timerStore.isExpired),
    toggleMute,
  }
}
