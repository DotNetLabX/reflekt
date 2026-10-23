import { ref, computed, watch, onMounted, onUnmounted } from 'vue'
import { useTimerStore } from '@/stores/timer'
import { formatMmSs } from '@/format/format'

let audioContext: AudioContext | null = null

function unlockChime() {
  if (typeof window.AudioContext === 'undefined') return
  audioContext ??= new window.AudioContext()
  if (audioContext.state === 'suspended') audioContext.resume().catch(() => {})
}

// Browsers only start audio from a user gesture; any gesture on the page, before the timer is shown included, unlocks the chime.
const UNLOCK_EVENTS = ['pointerdown', 'click', 'keydown'] as const

export function useChimeUnlock() {
  onMounted(() => UNLOCK_EVENTS.forEach((e) => document.addEventListener(e, unlockChime)))
  onUnmounted(() => UNLOCK_EVENTS.forEach((e) => document.removeEventListener(e, unlockChime)))
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

  const formattedTime = computed(() => formatMmSs(displayRemainingSeconds.value))

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
    toggleMute,
  }
}
