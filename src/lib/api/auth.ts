import { getApiBaseUrl } from '@/lib/api/base'

export function getLoginUrl(returnUrl = '/admin') {
  return `${getApiBaseUrl()}/auth/login?returnUrl=${encodeURIComponent(returnUrl)}`
}

export function getLocalAdminLoginUrl(returnUrl = '/admin', email = 'admin@example.com') {
  return `${getApiBaseUrl()}/auth/test-login?email=${encodeURIComponent(email)}&returnUrl=${encodeURIComponent(returnUrl)}`
}

let csrfTokenCache: string | null = null
let csrfHeaderNameCache = 'X-CSRF-TOKEN'

function isMutationMethod(method?: string) {
  const normalizedMethod = (method ?? 'GET').toUpperCase()
  return ['POST', 'PUT', 'PATCH', 'DELETE'].includes(normalizedMethod)
}

export async function getCsrfToken(forceRefresh = false) {
  if (csrfTokenCache && !forceRefresh) {
    return { requestToken: csrfTokenCache, headerName: csrfHeaderNameCache }
  }

  const response = await fetch(`${getApiBaseUrl()}/auth/csrf`, {
    credentials: 'include',
    cache: 'no-store',
  })

  if (!response.ok) {
    throw new Error('Failed to obtain CSRF token.')
  }

  const payload = await response.json() as { requestToken?: string; headerName?: string }
  csrfTokenCache = payload.requestToken ?? null
  csrfHeaderNameCache = payload.headerName ?? 'X-CSRF-TOKEN'

  if (!csrfTokenCache) {
    throw new Error('CSRF token response was empty.')
  }

  return { requestToken: csrfTokenCache, headerName: csrfHeaderNameCache }
}

export async function fetchWithCsrf(input: RequestInfo | URL, init: RequestInit = {}, retry = true) {
  const headers = new Headers(init.headers)
  const requestInit: RequestInit = {
    ...init,
    credentials: 'include',
    headers,
  }

  if (isMutationMethod(init.method)) {
    const csrf = await getCsrfToken()
    headers.set(csrf.headerName, csrf.requestToken)
  }

  const response = await fetch(input, requestInit)

  if (retry && response.status === 400 && isMutationMethod(init.method)) {
    const csrf = await getCsrfToken(true)
    headers.set(csrf.headerName, csrf.requestToken)
    return fetch(input, requestInit)
  }

  return response
}

export async function logoutWithCsrf(returnUrl = '/') {
  const response = await fetchWithCsrf(
    `${getApiBaseUrl()}/auth/logout?returnUrl=${encodeURIComponent(returnUrl)}`,
    { method: 'POST' },
  )

  if (!response.ok) {
    throw new Error('Failed to sign out.')
  }

  csrfTokenCache = null

  const payload = await response.json().catch(() => ({ redirectUrl: returnUrl })) as { redirectUrl?: string }
  return payload.redirectUrl ?? returnUrl
}
