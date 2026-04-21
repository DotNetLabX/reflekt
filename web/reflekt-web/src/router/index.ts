import { createRouter, createWebHistory } from 'vue-router'
import WelcomeView from '../views/WelcomeView.vue'
import NotInvitedView from '../views/NotInvitedView.vue'
import SprintView from '../views/SprintView.vue'
import RetroRoomView from '../views/RetroRoomView.vue'
import ReportView from '../views/ReportView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'welcome',
      component: WelcomeView,
    },
    {
      path: '/not-invited',
      name: 'not-invited',
      component: NotInvitedView,
    },
    {
      path: '/teams/:teamId/sprints/:sprintId',
      name: 'sprint',
      component: SprintView,
    },
    {
      path: '/sprint/:sprintId',
      name: 'retro-room',
      component: RetroRoomView,
    },
    {
      path: '/teams/:teamId/sprints/:sprintId/report',
      name: 'report',
      component: ReportView,
    },
  ],
})

export default router
