import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

declare module 'vue-router' {
  interface RouteMeta {
    title?: string
    requiresAuth?: boolean
  }
}

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'welcome',
      component: () => import('@/views/WelcomeView.vue'),
      meta: { title: 'Reflekt' },
    },
    {
      path: '/teams',
      name: 'teams',
      component: () => import('@/views/TeamDashboardView.vue'),
      meta: { title: 'Teams — Reflekt', requiresAuth: true },
    },
    {
      path: '/teams/:teamId/sprints',
      name: 'team-sprints',
      component: () => import('@/views/SprintListView.vue'),
      meta: { title: 'Sprints — Reflekt', requiresAuth: true },
    },
    {
      path: '/teams/:teamId/sprints/:sprintId',
      name: 'sprint',
      component: () => import('@/views/SprintView.vue'),
      meta: { title: 'Sprint — Reflekt', requiresAuth: true },
    },
    {
      path: '/sprint/:sprintId',
      name: 'retro-room',
      component: () => import('@/views/RetroRoomView.vue'),
      meta: { title: 'Retro Room — Reflekt' },
    },
    {
      path: '/teams/:teamId/sprints/:sprintId/report',
      name: 'report',
      component: () => import('@/views/ReportView.vue'),
      meta: { title: 'Report — Reflekt' },
    },
  ],
})

router.afterEach((to) => {
  document.title = to.meta.title || 'Reflekt'
})

router.beforeEach(async (to) => {
  const authStore = useAuthStore()

  // On cold load authStore.loaded is false — wait for auth state to resolve
  // before making any redirect decision. This prevents a brief unauthorized
  // render of protected routes before the initial /api/auth/me call completes.
  if (!authStore.loaded) {
    await authStore.fetchMe()
  }

  if (to.name === 'welcome' && authStore.isSignedIn) {
    return { name: 'teams' }
  }
  if (to.meta.requiresAuth && !authStore.isSignedIn) {
    return { name: 'welcome' }
  }
})

export default router
