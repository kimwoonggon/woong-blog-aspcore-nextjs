import { render, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ResponsivePageSizeSync } from '@/components/layout/ResponsivePageSizeSync'

const navigationMocks = vi.hoisted(() => ({
  replace: vi.fn(),
  pathname: '/blog',
  search: '',
}))

vi.mock('next/navigation', () => ({
  useRouter: () => ({ replace: navigationMocks.replace }),
  usePathname: () => navigationMocks.pathname,
  useSearchParams: () => new URLSearchParams(navigationMocks.search),
}))

describe('ResponsivePageSizeSync', () => {
  beforeEach(() => {
    navigationMocks.replace.mockClear()
    navigationMocks.pathname = '/blog'
    navigationMocks.search = ''
    Object.defineProperty(window, 'innerWidth', { configurable: true, value: 1280 })
    Object.defineProperty(window, 'innerHeight', { configurable: true, value: 960 })
  })

  it('adds the responsive page size when the route has no explicit pageSize', async () => {
    render(<ResponsivePageSizeSync desktopPageSize={12} tabletPageSize={8} mobilePageSize={4} />)

    await waitFor(() => {
      expect(navigationMocks.replace).toHaveBeenCalledWith('/blog?pageSize=12&page=1', { scroll: false })
    })
  })

  it('preserves an explicit pageSize so pagination tests and deep links do not get rewritten', async () => {
    navigationMocks.search = 'page=1&pageSize=2&__qaTagged=1'

    render(<ResponsivePageSizeSync desktopPageSize={12} tabletPageSize={8} mobilePageSize={4} />)

    await new Promise((resolve) => setTimeout(resolve, 0))

    expect(navigationMocks.replace).not.toHaveBeenCalled()
  })

  it('rewrites an explicit non-responsive pageSize back to the current viewport default', async () => {
    navigationMocks.pathname = '/works'
    navigationMocks.search = 'page=2&pageSize=6'

    render(<ResponsivePageSizeSync desktopPageSize={8} tabletPageSize={6} mobilePageSize={4} />)

    await waitFor(() => {
      expect(navigationMocks.replace).toHaveBeenCalledWith('/works?page=2&pageSize=8', { scroll: false })
    })
  })
})
