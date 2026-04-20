import { createRouter, createWebHistory } from 'vue-router'
import WelcomeView from '../views/WelcomeView.vue'
import NotInvitedView from '../views/NotInvitedView.vue'
import SprintView from '../views/SprintView.vue'

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
  ],
})

export default router
