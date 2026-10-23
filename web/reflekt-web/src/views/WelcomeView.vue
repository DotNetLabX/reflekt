<template>
  <div class="flex items-center justify-center min-h-[calc(100vh-3.5rem)]">
    <div class="flex flex-col items-center gap-4">
      <h1 class="text-2xl font-semibold text-text-primary">Welcome to Reflekt</h1>
      <BaseButton @click="authStore.signIn()">Sign in with Google</BaseButton>
    </div>
  </div>
</template>

<script setup lang="ts">
import { useAuthStore } from '@/stores/auth'
import BaseButton from '@/components/ui/BaseButton.vue'

const authStore = useAuthStore()
</script>
