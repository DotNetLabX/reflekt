export const STORAGE_KEYS = {
  guestToken: (sprintId: number) => `reflekt:sprint:${sprintId}:token`,
  theme: 'reflekt:theme',
  timerMuted: 'reflekt:timer:muted',
} as const

// Storage can throw (blocked storage, private mode, quota); every access degrades to "nothing stored".
export function readString(key: string): string | null {
  try {
    return localStorage.getItem(key)
  } catch {
    return null
  }
}

export function readOneOf<T extends string>(key: string, allowed: readonly T[], fallback: T): T {
  const value = readString(key)
  return allowed.find((a) => a === value) ?? fallback
}

export function readFlag(key: string): boolean {
  return readString(key) === 'true'
}

export function writeString(key: string, value: string): void {
  try {
    localStorage.setItem(key, value)
  } catch {
    // not persisted; the in-memory value still applies
  }
}

export function removeKey(key: string): void {
  try {
    localStorage.removeItem(key)
  } catch {
    // nothing could be stored, so nothing to remove
  }
}
