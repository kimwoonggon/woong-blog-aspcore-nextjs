import { cookies, headers } from 'next/headers'

export interface ServerSession {
  authenticated: boolean
  name?: string
  email?: string
  role?: string
  profileId?: string
}

function normalizeOrigin(origin: string) {
  return origin.replace(/\/$/, '')
}

function getServerOriginFromHeaders(host: string | null, proto: string | null) {
  const resolvedHost = host ?? 'localhost'
  const resolvedProto = proto ?? 'http'
  return `${resolvedProto}://${resolvedHost}`
}

function shouldProxyToComposeHost(origin: string) {
  return /^(https?:\/\/)(localhost|127\.0\.0\.1):3000$/i.test(normalizeOrigin(origin))
}

export async function getServerApiBaseUrl() {
  if (process.env.INTERNAL_API_ORIGIN) {
    return `${normalizeOrigin(process.env.INTERNAL_API_ORIGIN)}/api`
  }

  const headerStore = await headers()
  const resolvedOrigin = getServerOriginFromHeaders(
    headerStore.get('x-forwarded-host') ?? headerStore.get('host'),
    headerStore.get('x-forwarded-proto')
  )

  if (shouldProxyToComposeHost(resolvedOrigin)) {
    return 'http://localhost/api'
  }

  return `${normalizeOrigin(resolvedOrigin)}/api`
}

export async function fetchServerSession() {
  const apiBaseUrl = await getServerApiBaseUrl()
  const cookieHeader = await getServerCookieHeader()

  const response = await fetch(`${apiBaseUrl}/auth/session`, {
    headers: cookieHeader ? { cookie: cookieHeader } : {},
    cache: 'no-store',
  })

  if (!response.ok) {
    return { authenticated: false } satisfies ServerSession
  }

  return response.json() as Promise<ServerSession>
}

export async function getServerCookieHeader() {
  const cookieStore = await cookies()
  return cookieStore.getAll().map(({ name, value }) => `${name}=${value}`).join('; ')
}
