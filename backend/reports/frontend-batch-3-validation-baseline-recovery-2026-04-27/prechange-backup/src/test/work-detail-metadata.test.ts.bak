import { afterEach, describe, expect, it, vi } from 'vitest'

describe('work detail metadata description priority', () => {
  afterEach(() => {
    vi.resetModules()
    vi.clearAllMocks()
  })

  it('prefers socialShareMessage over excerpt when building metadata', async () => {
    vi.doMock('@/lib/api/works', () => ({
      fetchPublicWorkBySlug: vi.fn(async () => ({
        id: 'work-1',
        slug: 'work-1',
        title: 'Work 1',
        excerpt: 'Excerpt fallback',
        socialShareMessage: 'Use this share message',
        category: 'platform',
        period: null,
        tags: [],
        thumbnailUrl: '/media/thumb.jpg',
        iconUrl: '',
        publishedAt: '2026-04-01T00:00:00.000Z',
        contentJson: JSON.stringify({ html: '<p>body</p>' }),
        videosVersion: 0,
        videos: [],
      })),
      fetchAllPublicWorks: vi.fn(async () => []),
    }))

    const { generateMetadata } = await import('@/app/(public)/works/[slug]/page')
    const metadata = await generateMetadata({ params: Promise.resolve({ slug: 'work-1' }) })

    expect(metadata.description).toBe('Use this share message')
    expect(metadata.openGraph?.description).toBe('Use this share message')
    expect(metadata.twitter?.description).toBe('Use this share message')
  }, 30_000)

  it('falls back to excerpt when socialShareMessage is missing', async () => {
    vi.doMock('@/lib/api/works', () => ({
      fetchPublicWorkBySlug: vi.fn(async () => ({
        id: 'work-2',
        slug: 'work-2',
        title: 'Work 2',
        excerpt: 'Excerpt fallback only',
        socialShareMessage: null,
        category: 'platform',
        period: null,
        tags: [],
        thumbnailUrl: '/media/thumb.jpg',
        iconUrl: '',
        publishedAt: '2026-04-01T00:00:00.000Z',
        contentJson: JSON.stringify({ html: '<p>body</p>' }),
        videosVersion: 0,
        videos: [],
      })),
      fetchAllPublicWorks: vi.fn(async () => []),
    }))

    const { generateMetadata } = await import('@/app/(public)/works/[slug]/page')
    const metadata = await generateMetadata({ params: Promise.resolve({ slug: 'work-2' }) })

    expect(metadata.description).toBe('Excerpt fallback only')
    expect(metadata.openGraph?.description).toBe('Excerpt fallback only')
    expect(metadata.twitter?.description).toBe('Excerpt fallback only')
  }, 30_000)
})
