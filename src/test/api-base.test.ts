import { describe, expect, it, vi } from 'vitest'

describe('getApiBaseUrl', () => {
  it('returns localhost api when running from localhost:3000', async () => {
    vi.resetModules()
    delete process.env.NEXT_PUBLIC_API_BASE_URL

    const { getApiBaseUrl } = await import('@/lib/api/base')

    expect(getApiBaseUrl()).toBe('http://localhost/api')
  })

  it('returns NEXT_PUBLIC_API_BASE_URL when configured', async () => {
    vi.resetModules()
    process.env.NEXT_PUBLIC_API_BASE_URL = 'http://localhost/api'

    const { getApiBaseUrl } = await import('@/lib/api/base')

    expect(getApiBaseUrl()).toBe('http://localhost/api')
  })

  it('returns a relative api base outside the browser when no env override exists', async () => {
    vi.resetModules()
    delete process.env.NEXT_PUBLIC_API_BASE_URL

    const originalWindow = globalThis.window
    // @ts-expect-error test-only window removal
    delete globalThis.window

    const { getApiBaseUrl } = await import('@/lib/api/base')

    expect(getApiBaseUrl()).toBe('/api')

    globalThis.window = originalWindow
  })

  it('returns a relative api base in the browser when the origin is not the compose proxy host', async () => {
    vi.resetModules()
    delete process.env.NEXT_PUBLIC_API_BASE_URL

    const originalLocation = window.location
    Object.defineProperty(window, 'location', {
      configurable: true,
      value: { origin: 'https://example.com' },
    })

    const { getApiBaseUrl } = await import('@/lib/api/base')

    expect(getApiBaseUrl()).toBe('/api')

    Object.defineProperty(window, 'location', {
      configurable: true,
      value: originalLocation,
    })
  })
})
