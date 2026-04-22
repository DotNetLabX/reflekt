import { createRouter, createWebHistory } from 'vue-router'
import WelcomeView from '../views/WelcomeView.vue'
import SprintView from '../views/SprintView.vue'
import RetroRoomView from '../views/RetroRoomView.vue'
import ReportView from '../views/ReportView.vue'

declare module 'vue-router' {
  interface RouteMeta {
    title?: string;
  }
}

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'welcome',
      component: WelcomeView,
      meta: { title: 'Reflekt' },
    },
    {
      path: '/teams/:teamId/sprints/:sprintId',
      name: 'sprint',
      component: SprintView,
      meta: { title: 'Sprint — Reflekt' },
    },
    {
      path: '/sprint/:sprintId',
      name: 'retro-room',
      component: RetroRoomView,
      meta: { title: 'Retro Room — Reflekt' },
    },
    {
      path: '/teams/:teamId/sprints/:sprintId/report',
      name: 'report',
      component: ReportView,
      meta: { title: 'Report — Reflekt' },
    },
  ],
})

router.afterEach((to) => {
  document.title = to.meta.title || 'Reflekt';
})

export default router
