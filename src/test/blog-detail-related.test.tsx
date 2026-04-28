import { cleanup, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'

vi.mock('next/navigation', () => ({
  notFound: vi.fn(() => {
    throw new Error('notFound')
  }),
  useSearchParams: () => new URLSearchParams(''),
}))

describe('public blog detail related content', () => {
  afterEach(() => {
    cleanup()
    vi.resetModules()
    vi.clearAllMocks()
  })

  it('passes the full blog list including the current post to the related content section', async () => {
    const relatedItems = Array.from({ length: 37 }, (_, index) => ({
      id: `related-${index + 1}`,
      slug: `related-${index + 1}`,
      title: `Related ${index + 1}`,
      excerpt: `Excerpt ${index + 1}`,
      tags: [],
      publishedAt: '2026-03-29T00:00:00.000Z',
    }))

    vi.doMock('@/lib/api/blogs', () => ({
      fetchPublicBlogBySlug: vi.fn(async () => ({
        id: 'blog-1',
        slug: 'current-post',
        title: 'Current Post',
        excerpt: 'excerpt',
        tags: [],
        publishedAt: '2026-03-29T00:00:00.000Z',
        contentJson: JSON.stringify({ html: '<p>Hello</p>' }),
      })),
      fetchAllPublicBlogs: vi.fn(async () => [
        { id: 'blog-1', slug: 'current-post', title: 'Current Post', excerpt: 'excerpt', tags: [], publishedAt: '2026-03-29T00:00:00.000Z' },
        ...relatedItems,
      ]),
      fetchAdminBlogById: vi.fn(async () => null),
    }))

    vi.doMock('@/lib/api/server', () => ({
      fetchServerSession: vi.fn(async () => ({ authenticated: false, role: 'guest' })),
    }))

    vi.doMock('@/components/content/InteractiveRenderer', () => ({
      InteractiveRenderer: () => <div data-testid="render-html" />,
    }))

    vi.doMock('@/components/content/RelatedContentList', () => ({
      RelatedContentList: ({ items }: { items: Array<{ id: string }> }) => (
        <div data-testid="related-count">{items.length}</div>
      ),
    }))

    vi.doMock('@/components/ui/badge', () => ({
      Badge: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
    }))

    const BlogDetailPage = (await import('@/app/(public)/blog/[slug]/page')).default
    render(await BlogDetailPage({ params: Promise.resolve({ slug: 'current-post' }) }))

    expect(screen.getByTestId('related-count')).toHaveTextContent('38')
  }, 120000)

  it('orders valid dated blog relations before invalid and missing dates', async () => {
    vi.doMock('@/lib/api/blogs', () => ({
      fetchPublicBlogBySlug: vi.fn(async () => ({
        id: 'current',
        slug: 'current-post',
        title: 'Current Post',
        excerpt: 'excerpt',
        tags: [],
        publishedAt: '2026-03-20T00:00:00.000Z',
        contentJson: JSON.stringify({ html: '<p>Hello</p>' }),
      })),
      fetchAllPublicBlogs: vi.fn(async () => [
        { id: 'invalid', slug: 'invalid-post', title: 'Invalid Date Post', excerpt: '', tags: [], publishedAt: 'not-a-date' },
        { id: 'older', slug: 'older-post', title: 'Older Valid Post', excerpt: '', tags: [], publishedAt: '2026-03-10T00:00:00.000Z' },
        { id: 'missing', slug: 'missing-post', title: 'Missing Date Post', excerpt: '', tags: [], publishedAt: null },
        { id: 'newer', slug: 'newer-post', title: 'Newer Valid Post', excerpt: '', tags: [], publishedAt: '2026-03-30T00:00:00.000Z' },
        { id: 'current', slug: 'current-post', title: 'Current Post', excerpt: '', tags: [], publishedAt: '2026-03-20T00:00:00.000Z' },
      ]),
      fetchAdminBlogById: vi.fn(async () => null),
    }))

    vi.doMock('@/lib/api/server', () => ({
      fetchServerSession: vi.fn(async () => ({ authenticated: false, role: 'guest' })),
    }))

    vi.doMock('@/components/content/InteractiveRenderer', () => ({
      InteractiveRenderer: () => <div data-testid="render-html" />,
    }))

    vi.doMock('@/components/content/RelatedContentList', () => ({
      RelatedContentList: ({ items }: { items: Array<{ title: string }> }) => (
        <ol data-testid="related-order">
          {items.map((item) => <li key={item.title}>{item.title}</li>)}
        </ol>
      ),
    }))

    vi.doMock('@/components/ui/badge', () => ({
      Badge: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
    }))

    const BlogDetailPage = (await import('@/app/(public)/blog/[slug]/page')).default
    render(await BlogDetailPage({ params: Promise.resolve({ slug: 'current-post' }) }))

    expect(screen.getByTestId('related-order')).toHaveTextContent([
      'Newer Valid Post',
      'Current Post',
      'Older Valid Post',
      'Invalid Date Post',
      'Missing Date Post',
    ].join(''))
  }, 120000)
})
