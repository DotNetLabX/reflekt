import type { HubConnection } from '@microsoft/signalr'

let connectionSource: () => HubConnection | null = () => null

export function setHubConnectionSource(getter: () => HubConnection | null): void {
  connectionSource = getter
}

export function isHubConnected(): boolean {
  return connectionSource() !== null
}

export async function invoke(method: string, ...args: unknown[]): Promise<void> {
  const connection = connectionSource()
  if (!connection) throw new Error('Not connected to server.')
  await connection.invoke(method, ...args)
}
