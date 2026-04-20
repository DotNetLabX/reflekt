<template>
  <AppHeader />
  <RouterView />
</template>

<script setup lang="ts">
import { onMounted } from 'vue';
import AppHeader from './components/AppHeader.vue';
import { useAuthStore } from './stores/auth';

const authStore = useAuthStore();

onMounted(() => {
  authStore.fetchMe();
});
</script>
