import { afterEach, describe, expect, it, vi } from 'vitest'
import type { BlogListItem } from '@/lib/api/blogs'
import type { WorkListItem } from '@/lib/api/works'
import robots from '@/app/robots'
import { generateStaticParams as generateBlogStaticParams } from '@/app/(public)/blog/[slug]/page'
import { generateStaticParams as generateWorkStaticParams } from '@/app/(public)/works/[slug]/page'
import { fetchAllPublicBlogs } from '@/lib/api/blogs'
import { fetchAllPublicWorks } from '@/lib/api/works'

vi.mock('@/lib/api/blogs', () => ({
  fetchAllPublicBlogs: vi.fn(),
  fetchPublicBlogBySlug: vi.fn(),
}))

vi.mock('@/lib/api/works', () => ({
  fetchAllPublicWorks: vi.fn(),
  fetchPublicWorkBySlug: vi.fn(),
}))

const mockFetchAllPublicBlogs = vi.mocked(fetchAllPublicBlogs)
const mockFetchAllPublicWorks = vi.mocked(fetchAllPublicWorks)

function blog(slug: unknown): BlogListItem {
  return {
    id: `blog-${String(slug)}`,
    slug: slug as string,
    title: 'Blog post',
    excerpt: '',
    tags: [],
    publishedAt: null,
  }
}

function work(slug: unknown): WorkListItem {
  return {
    id: `work-${String(slug)}`,
    slug: slug as string,
    title: 'Work item',
    excerpt: '',
    category: 'Case study',
    tags: [],
    publishedAt: null,
  }
}

describe('public static route helpers', () => {
  afterEach(() => {
    vi.clearAllMocks()
    vi.unstubAllEnvs()
  })

  it('builds robots rules with a normalized sitemap URL', () => {
    vi.stubEnv('NEXT_PUBLIC_SITE_URL', 'https://example.test/')

    expect(robots()).toEqual({
      rules: {
        userAgent: '*',
        allow: '/',
        disallow: ['/admin/'],
      },
      sitemap: 'https://example.test/sitemap.xml',
    })
  })

  it('filters malformed blog slugs from generated static params', async () => {
    mockFetchAllPublicBlogs.mockResolvedValue([
      blog('valid-post'),
      blog('  한글 slug  '),
      blog('post<script>alert(1)'),
      blog(''),
      blog(null),
      blog(undefined),
      blog('/admin'),
      blog('nested/slug'),
      blog('post?draft=1'),
      blog('post#section'),
    ])

    await expect(generateBlogStaticParams()).resolves.toEqual([
      { slug: 'valid-post' },
      { slug: '한글 slug' },
      { slug: 'post<script>alert(1)' },
    ])
  })

  it('filters malformed work slugs from generated static params', async () => {
    mockFetchAllPublicWorks.mockResolvedValue([
      work('valid-work'),
      work('  작업 slug  '),
      work('work<script>alert(1)'),
      work(''),
      work(null),
      work(undefined),
      work('/admin'),
      work('nested/slug'),
      work('work?draft=1'),
      work('work#section'),
    ])

    await expect(generateWorkStaticParams()).resolves.toEqual([
      { slug: 'valid-work' },
      { slug: '작업 slug' },
      { slug: 'work<script>alert(1)' },
    ])
  })
})
