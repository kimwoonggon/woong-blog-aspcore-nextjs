import { cleanup, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'

vi.mock('next/navigation', () => ({
  notFound: vi.fn(() => {
    throw new Error('notFound')
  }),
}))

describe('public blog detail related content', () => {
  afterEach(() => {
    cleanup()
    vi.resetModules()
    vi.clearAllMocks()
  })

  it('passes the full related blog list to the related content section instead of truncating to 30 items', async () => {
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

    expect(screen.getByTestId('related-count')).toHaveTextContent('37')
  }, 90000)
})
