import { ref } from 'vue'
import type { Toast, ToastVariant } from '@/types/toast'

let nextId = 0
const toasts = ref<Toast[]>([])

export function useToast() {
  function addToast(message: string, variant: ToastVariant = 'info', duration = 4000) {
    const id = ++nextId
    toasts.value.push({ id, variant, message, duration })
    setTimeout(() => removeToast(id), duration)
  }

  function removeToast(id: number) {
    toasts.value = toasts.value.filter((t) => t.id !== id)
  }

  return { toasts, addToast, removeToast }
}
