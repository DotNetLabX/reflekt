import { defineStore } from 'pinia';

interface User { email: string; displayName: string; givenName: string; }

export const useAuthStore = defineStore('auth', {
  state: () => ({
    user: null as User | null,
    loaded: false,
  }),
  getters: {
    isSignedIn: (s) => s.user !== null,
  },
  actions: {
    async fetchMe() {
      const res = await fetch('/auth/me', { credentials: 'include' });
      this.user = res.ok ? (await res.json()) as User : null;
      this.loaded = true;
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
