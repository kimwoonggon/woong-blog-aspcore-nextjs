import { cleanup, fireEvent, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { notFound } from 'next/navigation'

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

  it('uses notFound for missing blog detail slugs', async () => {
    vi.doMock('@/lib/api/blogs', () => ({
      fetchPublicBlogBySlug: vi.fn(async () => null),
      fetchAllPublicBlogs: vi.fn(async () => []),
    }))

    const BlogDetailPage = (await import('@/app/(public)/blog/[slug]/page')).default

    await expect(BlogDetailPage({ params: Promise.resolve({ slug: 'missing-blog' }) })).rejects.toThrow('NEXT_NOT_FOUND')
    expect(notFound).toHaveBeenCalled()
  })

  it('uses notFound for missing work detail slugs', async () => {
    vi.doMock('@/lib/api/works', () => ({
      fetchPublicWorkBySlug: vi.fn(async () => null),
      fetchAllPublicWorks: vi.fn(async () => []),
    }))

    const WorkDetailPage = (await import('@/app/(public)/works/[slug]/page')).default

    await expect(WorkDetailPage({
      params: Promise.resolve({ slug: 'missing-work' }),
    })).rejects.toThrow('NEXT_NOT_FOUND')
    expect(notFound).toHaveBeenCalled()
  })
})

describe('public error and loading boundaries', () => {
  afterEach(() => {
    cleanup()
    vi.resetModules()
    vi.clearAllMocks()
  })

  it('renders the public segment error without leaking technical details or admin controls', async () => {
    const PublicSegmentError = (await import('@/app/(public)/error')).default
    const reset = vi.fn()

    render(
      <PublicSegmentError
        error={new Error('SQLSTATE 42P01 stack trace at WoongBlog.Api.Infrastructure')}
        reset={reset}
      />,
    )

    expect(screen.getByText('Public pages')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'This page could not be loaded.' })).toBeInTheDocument()
    expect(screen.queryByText(/SQLSTATE|stack trace|WoongBlog\.Api/i)).not.toBeInTheDocument()
    expect(screen.queryByRole('link', { name: /admin|edit|manage|관리|수정/i })).not.toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'Retry' }))
    expect(reset).toHaveBeenCalledTimes(1)
  })

  it('renders the blog detail error without leaking API detail or stack traces', async () => {
    const PublicBlogDetailError = (await import('@/app/(public)/blog/[slug]/error')).default
    const reset = vi.fn()

    render(
      <PublicBlogDetailError
        error={new Error('Failed to load public blog. Status 500. NpgsqlException stack trace')}
        reset={reset}
      />,
    )

    expect(screen.getByText('Study detail')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'This article could not be loaded.' })).toBeInTheDocument()
    expect(screen.queryByText(/NpgsqlException|Status 500|stack trace/i)).not.toBeInTheDocument()
    expect(screen.queryByRole('link', { name: /admin|edit|manage|관리|수정/i })).not.toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'Retry' }))
    expect(reset).toHaveBeenCalledTimes(1)
  })

  it('renders public loading shells without stack traces or admin controls', async () => {
    const PublicSegmentLoading = (await import('@/app/(public)/loading')).default
    const PublicBlogDetailLoading = (await import('@/app/(public)/blog/[slug]/loading')).default

    const { container, rerender } = render(<PublicSegmentLoading />)
    expect(container.textContent).not.toMatch(/stack|admin|관리|수정/i)
    expect(container.querySelector('.animate-pulse')).toBeTruthy()

    rerender(<PublicBlogDetailLoading />)
    expect(container.textContent).not.toMatch(/stack|admin|관리|수정/i)
    expect(container.querySelector('.animate-pulse')).toBeTruthy()
  })
})
