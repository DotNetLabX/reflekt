import { ref, computed } from 'vue'
import { readOneOf, STORAGE_KEYS, writeString } from '@/storage/storage'

// Module-level refs so all consumers share the same reactive state
const theme = ref(readOneOf(STORAGE_KEYS.theme, ['dark', 'light'] as const, 'dark'))

export function useTheme() {
  const isDark = computed(() => theme.value === 'dark')

  function toggleTheme() {
    theme.value = theme.value === 'dark' ? 'light' : 'dark'
    // dark: no class on <html> (matches :root default)
    // light: add 'light' class (matches .light override block)
    document.documentElement.classList.toggle('light', theme.value === 'light')
    writeString(STORAGE_KEYS.theme, theme.value)
  }

  return { theme, isDark, toggleTheme }
}
