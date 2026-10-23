interface PendingChange<V> {
  prev: V
  next: V
}

// Optimistic changes to one value per key, oldest first. A change that an authoritative
// payload has replaced is settled and is never undone; an unsettled change is undone on its
// own, so a newer change of the same value keeps its value on screen.
export function createPendingValues<K, V>(apply: (key: K, value: V) => void) {
  const pending = new Map<K, PendingChange<V>[]>()

  function change(key: K, prev: V, next: V): PendingChange<V> {
    const entry = { prev, next }
    const list = pending.get(key)
    if (list) list.push(entry)
    else pending.set(key, [entry])
    apply(key, next)
    return entry
  }

  function settle(key: K): void {
    pending.delete(key)
  }

  function settleAll(): void {
    pending.clear()
  }

  // The request behind `entry` succeeded: it and every older change of the value are settled,
  // and the server's value becomes the base a newer change would be undone to.
  function confirm(key: K, entry: PendingChange<V>, serverValue: V = entry.next): void {
    const list = pending.get(key)
    const index = list?.indexOf(entry) ?? -1
    if (!list || index === -1) return
    list.splice(0, index + 1)
    const newer = list[0]
    if (newer) newer.prev = serverValue
    else {
      pending.delete(key)
      apply(key, serverValue)
    }
  }

  function undo(key: K, entry: PendingChange<V>): void {
    const list = pending.get(key)
    const index = list?.indexOf(entry) ?? -1
    if (!list || index === -1) return
    list.splice(index, 1)
    const newer = list[index]
    if (newer) newer.prev = entry.prev
    else apply(key, entry.prev)
    if (list.length === 0) pending.delete(key)
  }

  return { change, settle, settleAll, confirm, undo }
}
