import { cleanup, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'

vi.mock('next/navigation', () => ({
  notFound: vi.fn(() => {
    throw new Error('NEXT_NOT_FOUND')
  }),
}))

vi.mock('@/components/content/InteractiveRenderer', () => ({
  InteractiveRenderer: () => <div data-testid="rendered-content" />,
}))

vi.mock('@/components/content/TableOfContents', () => ({
  TableOfContents: () => <nav data-testid="toc" />,
}))

vi.mock('@/components/content/RelatedContentList', () => ({
  RelatedContentList: ({ items }: { items: Array<{ id: string }> }) => (
    <div data-testid="related-count">{items.length}</div>
  ),
}))

vi.mock('@/components/content/WorkVideoPlayer', () => ({
  WorkVideoPlayer: () => <div data-testid="work-video" />,
}))

vi.mock('@/components/admin/PublicBlogDetailAdminActions', () => ({
  PublicBlogDetailAdminActions: ({ blogId }: { blogId: string }) => (
    <div data-testid="blog-admin-actions">{blogId}</div>
  ),
}))

vi.mock('@/components/admin/PublicWorkDetailAdminActions', () => ({
  PublicWorkDetailAdminActions: ({ workId }: { workId: string }) => (
    <div data-testid="work-admin-actions">{workId}</div>
  ),
}))

describe('public detail route admin boundary', () => {
  afterEach(() => {
    cleanup()
    vi.resetModules()
    vi.clearAllMocks()
  })

  it('renders blog public content without a server session or admin detail fetch', async () => {
    const fetchServerSession = vi.fn(async () => ({ authenticated: true, role: 'admin' }))
    const fetchAdminBlogById = vi.fn(async () => ({ id: 'blog-1', title: 'Admin Blog' }))

    vi.doMock('@/lib/api/server', () => ({
      fetchServerSession,
    }))
    vi.doMock('@/lib/api/blogs', () => ({
      fetchPublicBlogBySlug: vi.fn(async () => ({
        id: 'blog-1',
        slug: 'public-blog',
        title: 'Public Blog',
        excerpt: 'Public excerpt',
        tags: [],
        publishedAt: '2026-04-01T00:00:00.000Z',
        contentJson: JSON.stringify({ html: '<h2>Public content</h2>' }),
      })),
      fetchAllPublicBlogs: vi.fn(async () => [
        { id: 'blog-1', slug: 'public-blog', title: 'Public Blog', excerpt: 'Public excerpt', tags: [], publishedAt: '2026-04-01T00:00:00.000Z' },
      ]),
      fetchAdminBlogById,
    }))

    const BlogDetailPage = (await import('@/app/(public)/blog/[slug]/page')).default
    render(await BlogDetailPage({ params: Promise.resolve({ slug: 'public-blog' }) }))

    expect(screen.getByRole('heading', { name: 'Public Blog' })).toBeInTheDocument()
    expect(screen.getByTestId('blog-admin-actions')).toHaveTextContent('blog-1')
    expect(fetchServerSession).not.toHaveBeenCalled()
    expect(fetchAdminBlogById).not.toHaveBeenCalled()
  }, 15_000)

  it('renders work public content without a server session or admin detail fetch', async () => {
    const fetchServerSession = vi.fn(async () => ({ authenticated: true, role: 'admin' }))
    const fetchAdminWorkById = vi.fn(async () => ({ id: 'work-1', title: 'Admin Work' }))

    vi.doMock('@/lib/api/server', () => ({
      fetchServerSession,
    }))
    vi.doMock('@/lib/api/works', () => ({
      fetchPublicWorkBySlug: vi.fn(async () => ({
        id: 'work-1',
        slug: 'public-work',
        title: 'Public Work',
        excerpt: 'Public excerpt',
        category: 'Platform',
        period: null,
        tags: [],
        thumbnailUrl: null,
        iconUrl: null,
        publishedAt: '2026-04-01T00:00:00.000Z',
        contentJson: JSON.stringify({ html: '<h2>Public work content</h2>' }),
        videosVersion: 0,
        videos: [],
      })),
      fetchAllPublicWorks: vi.fn(async () => [
        { id: 'work-1', slug: 'public-work', title: 'Public Work', excerpt: 'Public excerpt', category: 'Platform', tags: [], publishedAt: '2026-04-01T00:00:00.000Z' },
      ]),
      fetchAdminWorkById,
    }))

    const WorkDetailPage = (await import('@/app/(public)/works/[slug]/page')).default
    render(await WorkDetailPage({
      params: Promise.resolve({ slug: 'public-work' }),
    }))

    expect(screen.getByRole('heading', { name: 'Public Work' })).toBeInTheDocument()
    expect(screen.getByTestId('work-admin-actions')).toHaveTextContent('work-1')
    expect(fetchServerSession).not.toHaveBeenCalled()
    expect(fetchAdminWorkById).not.toHaveBeenCalled()
  })
})
