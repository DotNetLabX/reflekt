import { defineStore } from 'pinia';

interface User { email: string; displayName: string; givenName: string; }

// The router guard and App.vue both call fetchMe on a cold load and each reads the
// result after its own await, so concurrent calls share one request.
let fetchMeInFlight: Promise<void> | null = null;

export const useAuthStore = defineStore('auth', {
  state: () => ({
    user: null as User | null,
    loaded: false,
    error: null as string | null,
  }),
  getters: {
    isSignedIn: (s) => s.user !== null,
  },
  actions: {
    fetchMe(): Promise<void> {
      fetchMeInFlight ??= this._loadMe().finally(() => {
        fetchMeInFlight = null;
      });
      return fetchMeInFlight;
    },
    async _loadMe() {
      try {
        const res = await fetch('/auth/me', { credentials: 'include' });
        this.user = res.ok ? (await res.json()) as User : null;
        this.error = null;
      } catch {
        this.user = null;
        this.error = "Can't reach the server";
      } finally {
        this.loaded = true;
      }
    },
    signIn() {
      window.location.href = '/auth/google/login';
    },
    async signOut() {
      await fetch('/auth/logout', { method: 'POST', credentials: 'include' });
      this.user = null;
      window.location.href = '/';
    },
  },
});
