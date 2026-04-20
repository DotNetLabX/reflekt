<template>
  <header v-if="authStore.isSignedIn" class="flex items-center justify-between px-4 py-2 border-b border-gray-200">
    <span class="text-sm text-gray-700">{{ authStore.user!.email }}</span>
    <button
      class="text-sm text-gray-500 hover:text-gray-900"
      @click="authStore.signOut()"
    >
      Sign out
    </button>
  </header>
</template>

<script setup lang="ts">
import { useAuthStore } from '../stores/auth';

const authStore = useAuthStore();
</script>
