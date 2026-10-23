<template>
  <Teleport to="body">
    <Transition name="modal">
      <div v-if="open" class="fixed inset-0 z-50 flex items-center justify-center">
        <div class="fixed inset-0 bg-black/50" @click="$emit('close')" />
        <div
          ref="panelRef"
          class="relative z-10 w-full max-w-md bg-surface rounded-lg shadow-modal focus:outline-none"
          role="dialog"
          aria-modal="true"
          :aria-labelledby="titleId"
          tabindex="-1"
        >
          <div class="flex items-center justify-between px-5 py-4 border-b border-border">
            <h2 :id="titleId" class="text-lg font-semibold text-text-primary">{{ title }}</h2>
            <button
              ref="closeRef"
              type="button"
              class="text-text-muted hover:text-text-secondary transition-colors cursor-pointer"
              aria-label="Close"
              @click="$emit('close')"
            >
              <X :size="20" />
            </button>
          </div>
          <div ref="contentRef">
            <div class="px-5 py-4">
              <slot />
            </div>
            <div v-if="$slots.footer" class="px-5 py-4 border-t border-border">
              <slot name="footer" />
            </div>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup lang="ts">
import { nextTick, onUnmounted, ref, useId, watch } from 'vue'
import { X } from 'lucide-vue-next'

const props = defineProps<{
  open: boolean
  title: string
}>()

const emit = defineEmits<{
  close: []
}>()

const titleId = useId()
const panelRef = ref<HTMLElement | null>(null)
const closeRef = ref<HTMLButtonElement | null>(null)
const contentRef = ref<HTMLElement | null>(null)

const FOCUSABLE =
  'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])'

let returnFocusTo: HTMLElement | null = null

function focusables(): HTMLElement[] {
  return Array.from(panelRef.value?.querySelectorAll<HTMLElement>(FOCUSABLE) ?? [])
}

function onKeydown(event: KeyboardEvent) {
  if (event.key === 'Escape') {
    event.preventDefault()
    emit('close')
    return
  }
  if (event.key !== 'Tab') return
  const items = focusables()
  const first = items[0]
  const last = items[items.length - 1]
  if (!first || !last) {
    event.preventDefault()
    panelRef.value?.focus()
    return
  }
  const active = document.activeElement
  const inside = panelRef.value?.contains(active) ?? false
  if (event.shiftKey && (active === first || !inside)) {
    event.preventDefault()
    last.focus()
  } else if (!event.shiftKey && (active === last || !inside)) {
    event.preventDefault()
    first.focus()
  }
}

async function activate() {
  returnFocusTo = document.activeElement instanceof HTMLElement ? document.activeElement : null
  document.body.style.overflow = 'hidden'
  document.addEventListener('keydown', onKeydown)
  await nextTick()
  const firstInContent = contentRef.value?.querySelector<HTMLElement>(FOCUSABLE)
  const target = firstInContent ?? closeRef.value ?? panelRef.value
  target?.focus()
}

function deactivate() {
  document.body.style.overflow = ''
  document.removeEventListener('keydown', onKeydown)
  const target = returnFocusTo
  returnFocusTo = null
  if (target?.isConnected) target.focus()
}

watch(
  () => props.open,
  (isOpen, wasOpen) => {
    if (isOpen) activate()
    else if (wasOpen) deactivate()
  },
  { immediate: true },
)

onUnmounted(() => {
  if (props.open) deactivate()
})
</script>

<style scoped>
.modal-enter-active,
.modal-leave-active {
  transition: opacity 0.2s ease;
}
.modal-enter-active > .relative,
.modal-leave-active > .relative {
  transition:
    opacity 0.2s ease,
    transform 0.2s ease;
}
.modal-enter-from,
.modal-leave-to {
  opacity: 0;
}
.modal-enter-from > .relative {
  opacity: 0;
  transform: scale(0.95);
}
.modal-leave-to > .relative {
  opacity: 0;
  transform: scale(0.95);
}
</style>
