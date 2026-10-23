export interface ApiFieldError {
  field: string
  message: string
}

interface ApiErrorBody {
  message: string
  code: string | null
  errors: ApiFieldError[]
}

export class ApiError extends Error {
  readonly status: number
  readonly code: string | null
  readonly errors: ApiFieldError[]

  constructor(status: number, body: ApiErrorBody, options?: ErrorOptions) {
    super(body.message, options)
    this.name = 'ApiError'
    this.status = status
    this.code = body.code
    this.errors = body.errors
  }
}

export interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE'
  body?: unknown
  guest?: boolean
}

const NETWORK_ERROR_MESSAGE = "Can't reach the server"
const GENERIC_SERVER_MESSAGE = 'One or more errors occurred!'
const KNOWN_CODES = ['duplicate_name', 'limit_reached'] as const

let guestTokenSource: () => string | null = () => null

export function setGuestTokenSource(getter: () => string | null): void {
  guestTokenSource = getter
}

export async function apiFetch<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const { method = 'GET', body, guest = false } = options
  const headers: Record<string, string> = {}
  if (body !== undefined) headers['Content-Type'] = 'application/json'
  if (guest) {
    const token = guestTokenSource()
    if (token) headers['X-Guest-Token'] = token
  }

  let res: Response
  try {
    res = await fetch(path, {
      method,
      headers,
      credentials: 'include',
      body: body === undefined ? undefined : JSON.stringify(body),
    })
  } catch (cause) {
    throw new ApiError(0, { message: NETWORK_ERROR_MESSAGE, code: null, errors: [] }, { cause })
  }

  const data = parseBody(await res.text().catch(() => ''))
  if (!res.ok) throw new ApiError(res.status, readErrorBody(data))
  return data as T
}

function parseBody(text: string): unknown {
  if (!text) return undefined
  try {
    return JSON.parse(text)
  } catch {
    return undefined
  }
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}

function serverMessage(value: unknown): string {
  return typeof value === 'string' && value !== GENERIC_SERVER_MESSAGE ? value : ''
}

function readErrorBody(data: unknown): ApiErrorBody {
  if (!isRecord(data)) return { message: '', code: null, errors: [] }
  return readF38ErrorBody(data) ?? readPreF38ErrorBody(data)
}

function readF38ErrorBody(data: Record<string, unknown>): ApiErrorBody | null {
  if (isRecord(data.errors)) return null
  const errors = Array.isArray(data.errors)
    ? data.errors.filter(isRecord).map((e) => ({
        field: typeof e.field === 'string' ? e.field : '',
        message: typeof e.message === 'string' ? e.message : '',
      }))
    : []
  return {
    message: serverMessage(data.message),
    code: typeof data.code === 'string' ? data.code : null,
    errors,
  }
}

// Pre-F38 reader for FastEndpoints' default error body. Remove once F38 is merged
// (docs/specs/F39-FrontendConventions/delivery/change-notes.md, Note 3).
function readPreF38ErrorBody(data: Record<string, unknown>): ApiErrorBody {
  const fields = isRecord(data.errors) ? data.errors : {}
  let message = ''
  let code: string | null = null
  const errors: ApiFieldError[] = []
  for (const [key, value] of Object.entries(fields)) {
    const messages = Array.isArray(value)
      ? value.filter((m): m is string => typeof m === 'string')
      : []
    if (key === 'generalErrors') {
      message = messages[0] ?? ''
      continue
    }
    for (const m of messages) errors.push({ field: key, message: m })
  }
  for (const known of KNOWN_CODES) {
    const prefix = `${known}: `
    if (message.startsWith(prefix)) {
      code = known
      message = message.slice(prefix.length)
      break
    }
  }
  return { message: serverMessage(message), code, errors }
}

export function isHttpError(err: unknown): err is ApiError {
  return err instanceof ApiError && err.status !== 0
}

export interface DescribeErrorOptions {
  statusMessages?: Partial<Record<number, string>>
  fallback: string
}

export function describeApiError(err: unknown, options: DescribeErrorOptions): string {
  if (!(err instanceof ApiError)) return options.fallback
  const known = KNOWN_CODES.some((c) => c === err.code)
  if (known && err.message) return err.message
  const statusMessage = options.statusMessages?.[err.status]
  if (statusMessage) return statusMessage
  if (err.status !== 0 && err.message) return err.message
  return options.fallback
}
