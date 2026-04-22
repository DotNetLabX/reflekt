<template>
  <header class="sticky top-0 z-10 flex items-center justify-between h-14 px-4 sm:px-6 bg-white border-b border-gray-200">
    <RouterLink to="/" class="flex items-center gap-2 text-primary-600 hover:text-primary-700 transition-colors">
      <RefreshCw :size="20" />
      <span class="text-lg font-semibold">Reflekt</span>
    </RouterLink>

    <div v-if="authStore.isSignedIn" ref="menuRef" class="relative">
      <button
        class="flex items-center justify-center w-9 h-9 rounded-[--radius-full] bg-primary-100 text-primary-700 font-semibold text-sm cursor-pointer hover:bg-primary-200 transition-colors"
        @click="menuOpen = !menuOpen"
      >
        {{ userInitial }}
      </button>

      <Transition name="dropdown">
        <div
          v-if="menuOpen"
          class="absolute right-0 mt-2 w-56 bg-surface border border-gray-200 rounded-[--radius-md] shadow-card-hover py-1"
        >
          <div class="px-3 py-2 text-sm text-gray-500 truncate border-b border-gray-100">
            {{ authStore.user!.email }}
          </div>
          <button
            class="w-full text-left px-3 py-2 text-sm text-gray-700 hover:bg-gray-50 cursor-pointer transition-colors"
            @click="handleSignOut"
          >
            Sign out
          </button>
        </div>
      </Transition>
    </div>
  </header>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { RouterLink } from 'vue-router';
import { RefreshCw } from 'lucide-vue-next';
import { useAuthStore } from '../stores/auth';

const authStore = useAuthStore();
const menuOpen = ref(false);
const menuRef = ref<HTMLElement | null>(null);

const userInitial = computed(() => {
  const user = authStore.user;
  if (!user) return '';
  const name = user.givenName || user.displayName || user.email;
  return name.charAt(0).toUpperCase();
});

function handleSignOut() {
  menuOpen.value = false;
  authStore.signOut();
}

function handleClickOutside(event: MouseEvent) {
  if (menuOpen.value && menuRef.value && !menuRef.value.contains(event.target as Node)) {
    menuOpen.value = false;
  }
}

onMounted(() => document.addEventListener('click', handleClickOutside));
onUnmounted(() => document.removeEventListener('click', handleClickOutside));
</script>

<style scoped>
.dropdown-enter-active,
.dropdown-leave-active {
  transition: opacity 0.15s ease, transform 0.15s ease;
}
.dropdown-enter-from,
.dropdown-leave-to {
  opacity: 0;
  transform: translateY(-4px);
}
</style>
