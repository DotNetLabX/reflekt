<template>
  <AppHeader />
  <p
    v-if="authStore.error"
    role="status"
    class="px-4 py-2 text-sm text-center bg-badge-warning-bg text-badge-warning-text"
  >
    {{ authStore.error }}.
  </p>
  <!-- Keyed by path: Vue Router reuses a view on a param change, and the views load their ids once on mount. -->
  <RouterView :key="route.path" />
  <ToastContainer />
</template>

<script setup lang="ts">
import { onMounted } from 'vue';
import { useRoute } from 'vue-router';
import AppHeader from './components/AppHeader.vue';
import ToastContainer from './components/ui/ToastContainer.vue';
import { useAuthStore } from './stores/auth';

const route = useRoute();
const authStore = useAuthStore();

onMounted(() => {
  authStore.fetchMe();
});
</script>
