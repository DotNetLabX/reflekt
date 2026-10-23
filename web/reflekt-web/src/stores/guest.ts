import { defineStore } from 'pinia'
import { ref } from 'vue'
import { readString, removeKey, STORAGE_KEYS, writeString } from '@/storage/storage'

export const useGuestStore = defineStore('guest', () => {
  const token = ref<string | null>(null)

  function loadToken(sprintId: number) {
    token.value = readString(STORAGE_KEYS.guestToken(sprintId))
  }

  function saveToken(sprintId: number, newToken: string) {
    writeString(STORAGE_KEYS.guestToken(sprintId), newToken)
    token.value = newToken
  }

  function clearToken(sprintId: number) {
    removeKey(STORAGE_KEYS.guestToken(sprintId))
    token.value = null
  }

  return { token, loadToken, saveToken, clearToken }
})
