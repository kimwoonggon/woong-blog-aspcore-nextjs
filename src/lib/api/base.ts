function normalizeOrigin(origin: string) {
  return origin.replace(/\/$/, '')
}

function shouldProxyToComposeHost(origin: string) {
  return /^(https?:\/\/)(localhost|127\.0\.0\.1):3000$/i.test(normalizeOrigin(origin))
}

export function getApiBaseUrl() {
  if (process.env.NEXT_PUBLIC_API_BASE_URL) {
    return process.env.NEXT_PUBLIC_API_BASE_URL
  }

  if (typeof window !== 'undefined') {
    const origin = window.location.origin
    if (shouldProxyToComposeHost(origin)) {
      return 'http://localhost/api'
    }
  }

  return '/api'
}
