import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { authApi } from '@/api/auth'
import { isHttpError } from '@/api/client'
import type { User } from '@/types/auth'

export const useAuthStore = defineStore('auth', () => {
  const user = ref<User | null>(null)
  const loaded = ref(false)
  const error = ref<string | null>(null)

  const isSignedIn = computed(() => user.value !== null)

  // The router guard and the retro room can both call fetchMe on a cold load and each reads
  // the result after its own await, so concurrent calls share one request.
  let fetchMeInFlight: Promise<void> | null = null

  function fetchMe(): Promise<void> {
    fetchMeInFlight ??= loadMe().finally(() => {
      fetchMeInFlight = null
    })
    return fetchMeInFlight
  }

  async function loadMe() {
    try {
      user.value = (await authApi.me()) ?? null
      error.value = null
    } catch (e) {
      user.value = null
      error.value = isHttpError(e) ? null : "Can't reach the server"
    } finally {
      loaded.value = true
    }
  }

  function signIn() {
    window.location.href = '/auth/google/login'
  }

  async function signOut() {
    try {
      await authApi.logout()
    } catch (e) {
      if (!isHttpError(e)) throw e
    }
    user.value = null
    window.location.href = '/'
  }

  return { user, loaded, error, isSignedIn, fetchMe, signIn, signOut }
})
