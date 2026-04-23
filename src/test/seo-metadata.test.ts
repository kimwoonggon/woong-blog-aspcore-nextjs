import { describe, expect, it, vi } from 'vitest'
import { createPublicMetadata, getMetadataBaseUrl } from '@/lib/seo'

describe('SEO metadata helpers', () => {
  it('builds canonical, Open Graph, and Twitter metadata for public routes', () => {
    expect(createPublicMetadata({
      title: 'Study',
      description: 'Study notes.',
      path: '/blog',
    })).toMatchObject({
      title: 'Study',
      description: 'Study notes.',
      alternates: { canonical: '/blog' },
      openGraph: {
        title: 'Study',
        description: 'Study notes.',
        url: '/blog',
        type: 'website',
      },
      twitter: {
        card: 'summary',
        title: 'Study',
        description: 'Study notes.',
      },
    })
  })

  it('uses configured public site URL before falling back to localhost', () => {
    vi.stubEnv('NEXT_PUBLIC_SITE_URL', 'https://example.com/')
    expect(getMetadataBaseUrl()).toBe('https://example.com')
    vi.unstubAllEnvs()
    expect(getMetadataBaseUrl()).toBe('http://localhost:3000')
  })
})
