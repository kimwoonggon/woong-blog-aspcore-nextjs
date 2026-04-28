import { render, screen } from '@testing-library/react'
import { createElement } from 'react'
import type { ReactNode } from 'react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import HomePage from '@/app/(public)/page'
import { fetchPublicHome } from '@/lib/api/home'

vi.mock('next/link', () => ({
  default: ({ href, children, ...props }: { href: string; children: ReactNode }) => (
    <a href={href} {...props}>{children}</a>
  ),
}))

vi.mock('next/image', () => ({
  default: ({ src, alt, ...props }: { src: string; alt: string }) => createElement('img', { src, alt, ...props }),
}))

vi.mock('@/components/content/LocalQaQueryBoundary', () => ({
  LocalQaNoImageBoundary: ({ children }: { children: ReactNode }) => <>{children}</>,
}))

vi.mock('@/lib/api/home', () => ({
  fetchPublicHome: vi.fn(),
}))

const mockFetchPublicHome = vi.mocked(fetchPublicHome)

describe('public home date fallbacks', () => {
  afterEach(() => {
    vi.clearAllMocks()
  })

  it('renders safe fallback labels for invalid featured work and recent post dates', async () => {
    mockFetchPublicHome.mockResolvedValueOnce({
      homePage: {
        title: 'Home',
        contentJson: JSON.stringify({ headline: 'Home headline', introText: 'Intro' }),
      },
      siteSettings: {
        ownerName: 'Owner',
        tagline: '',
        gitHubUrl: '',
        linkedInUrl: '',
        resumePublicUrl: '',
      },
      featuredWorks: [{
        id: 'work-1',
        slug: 'work-1',
        title: 'Work with malformed date',
        excerpt: 'Work excerpt',
        category: 'Platform',
        period: null,
        tags: [],
        thumbnailUrl: '',
        iconUrl: '',
        publishedAt: 'not-a-date',
      }],
      recentPosts: [{
        id: 'post-1',
        slug: 'post-1',
        title: 'Post with malformed date',
        excerpt: 'Post excerpt',
        tags: ['study'],
        coverUrl: '',
        publishedAt: 'not-a-date',
      }],
    })

    const { container } = render(await HomePage())

    expect(screen.getAllByText('Unknown Date')).toHaveLength(2)
    expect(container.textContent).not.toMatch(/Invalid Date|RangeError/i)
  })
})
