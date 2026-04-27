import { describe, expect, it } from 'vitest'
import { buildWorkDetailMetadata } from '@/app/(public)/works/[slug]/work-detail-metadata'

describe('work detail metadata description priority', () => {
  it('prefers socialShareMessage over excerpt when building metadata', () => {
    const metadata = buildWorkDetailMetadata({
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
    })

    expect(metadata.description).toBe('Use this share message')
    expect(metadata.openGraph?.description).toBe('Use this share message')
    expect(metadata.twitter?.description).toBe('Use this share message')
  })

  it('falls back to excerpt when socialShareMessage is missing', () => {
    const metadata = buildWorkDetailMetadata({
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
    })

    expect(metadata.description).toBe('Excerpt fallback only')
    expect(metadata.openGraph?.description).toBe('Excerpt fallback only')
    expect(metadata.twitter?.description).toBe('Excerpt fallback only')
  })
})
