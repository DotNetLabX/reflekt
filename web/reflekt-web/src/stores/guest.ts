import { defineStore } from 'pinia'

// Populated by feature plans.
// localStorage key format locked at Foundation: reflekt:sprint:{sprintId}:token
export const guestTokenKey = (sprintId: string) => `reflekt:sprint:${sprintId}:token`

export const useGuestStore = defineStore('guest', {
  state: () => ({}),
})
