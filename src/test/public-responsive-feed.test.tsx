import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it, vi, beforeEach } from 'vitest'
import { PublicResponsiveFeed } from '@/components/content/PublicResponsiveFeed'

vi.mock('next/image', () => ({
  default: ({ src, alt, ...props }: { src: string; alt: string }) => <img src={src} alt={alt} {...props} />,
}))

function buildBlogItems(prefix: string, count: number, start = 1) {
  return Array.from({ length: count }, (_, index) => {
    const number = start + index
    return {
      id: `${prefix}-${number}`,
      slug: `${prefix}-${number}`,
      title: `${prefix} title ${number}`,
      excerpt: `${prefix} excerpt ${number}`,
      tags: ['tag'],
      publishedAt: '2026-04-23T00:00:00.000Z',
    }
  })
}

describe('PublicResponsiveFeed', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
  })

  it('appends next mobile page and deduplicates appended items', async () => {
    vi.stubGlobal('matchMedia', vi.fn().mockReturnValue({
      matches: true,
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
    }))

    const fetchMock = vi.fn()
    vi.stubGlobal('fetch', fetchMock as typeof fetch)

    const initialItems = buildBlogItems('study', 10, 1)
    const appendedItems = [...buildBlogItems('study', 2, 10), ...buildBlogItems('study', 9, 12)]
    fetchMock.mockResolvedValueOnce(new Response(JSON.stringify({
      items: appendedItems,
      page: 2,
      pageSize: 10,
      totalItems: 19,
      totalPages: 2,
    }), { status: 200, headers: { 'Content-Type': 'application/json' } }))

    render(
      <PublicResponsiveFeed
        kind="blog"
        query=""
        desktopPayload={{ items: buildBlogItems('desktop', 3), page: 1, pageSize: 12, totalItems: 3, totalPages: 1 }}
        mobileInitialPayload={{ items: initialItems, page: 1, pageSize: 10, totalItems: 19, totalPages: 2 }}
        desktopReturnTo={encodeURIComponent('/blog?page=1&pageSize=12')}
      />,
    )

    expect(screen.getAllByTestId('blog-card')).toHaveLength(10)
    fireEvent.click(screen.getByTestId('blog-load-more'))

    await waitFor(() => {
      expect(screen.getAllByTestId('blog-card')).toHaveLength(20)
    })
  })

  it('does not auto-fetch the next page on mobile until load more is clicked', () => {
    vi.stubGlobal('matchMedia', vi.fn().mockReturnValue({
      matches: true,
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
    }))

    const fetchMock = vi.fn()
    vi.stubGlobal('fetch', fetchMock as typeof fetch)

    render(
      <PublicResponsiveFeed
        kind="blog"
        query=""
        desktopPayload={{ items: buildBlogItems('desktop', 3), page: 1, pageSize: 12, totalItems: 3, totalPages: 1 }}
        mobileInitialPayload={{ items: buildBlogItems('mobile', 10), page: 1, pageSize: 10, totalItems: 20, totalPages: 2 }}
        desktopReturnTo={encodeURIComponent('/blog?page=1&pageSize=12')}
      />,
    )

    expect(screen.getByTestId('blog-load-more')).toBeInTheDocument()
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('resets to page-1 items when query changes', async () => {
    vi.stubGlobal('matchMedia', vi.fn().mockReturnValue({
      matches: true,
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
    }))

    const { rerender } = render(
      <PublicResponsiveFeed
        kind="blog"
        query="one"
        desktopPayload={{ items: buildBlogItems('desktop', 3), page: 1, pageSize: 12, totalItems: 3, totalPages: 1 }}
        mobileInitialPayload={{ items: buildBlogItems('one', 2), page: 1, pageSize: 10, totalItems: 2, totalPages: 1 }}
        desktopReturnTo={encodeURIComponent('/blog?page=1&pageSize=12&query=one')}
      />,
    )

    expect(screen.getByText('one title 1')).toBeInTheDocument()

    rerender(
      <PublicResponsiveFeed
        kind="blog"
        query="two"
        desktopPayload={{ items: buildBlogItems('desktop', 3), page: 1, pageSize: 12, totalItems: 3, totalPages: 1 }}
        mobileInitialPayload={{ items: buildBlogItems('two', 2), page: 1, pageSize: 10, totalItems: 2, totalPages: 1 }}
        desktopReturnTo={encodeURIComponent('/blog?page=1&pageSize=12&query=two')}
      />,
    )

    expect(screen.queryByText('one title 1')).not.toBeInTheDocument()
    expect(screen.getByText('two title 1')).toBeInTheDocument()
  })

  it('disables infinite controls in desktop mode and keeps desktop payload cards', () => {
    vi.stubGlobal('matchMedia', vi.fn().mockReturnValue({
      matches: false,
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
    }))

    render(
      <PublicResponsiveFeed
        kind="blog"
        query=""
        desktopPayload={{ items: buildBlogItems('desktop', 3), page: 2, pageSize: 12, totalItems: 36, totalPages: 3 }}
        mobileInitialPayload={{ items: buildBlogItems('mobile', 10), page: 1, pageSize: 10, totalItems: 10, totalPages: 1 }}
        desktopReturnTo={encodeURIComponent('/blog?page=2&pageSize=12')}
      />,
    )

    expect(screen.getAllByTestId('blog-card')).toHaveLength(3)
    expect(screen.queryByTestId('blog-load-more')).not.toBeInTheDocument()
  })
})
