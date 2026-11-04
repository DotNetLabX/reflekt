export function formatMmSs(totalSeconds: number): string {
  const total = Math.max(0, totalSeconds)
  const mins = Math.floor(total / 60)
  const secs = total % 60
  return `${String(mins).padStart(2, '0')}:${String(secs).padStart(2, '0')}`
}

export function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString()
}

export function formatDateTime(iso: string): string {
  return new Date(iso).toLocaleString()
}

export function formatShortDate(iso: string): string {
  return new Date(iso).toLocaleDateString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  })
}

export function truncateUrl(url: string): string {
  try {
    const u = new URL(url)
    const path = u.pathname.length > 20 ? u.pathname.slice(0, 20) + '...' : u.pathname
    return u.hostname + path
  } catch {
    return url.length > 40 ? url.slice(0, 40) + '...' : url
  }
}

export function formatStatus(status: string): string {
  return status === 'CarriedOver' ? 'Carried Over' : status
}

export function formatVotes(count: number): string {
  return count === 1 ? '1 vote' : `${count} votes`
}
