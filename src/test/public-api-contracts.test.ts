import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('@/lib/api/server', () => ({
  getServerApiBaseUrl: vi.fn(async () => 'http://localhost/api'),
  getServerCookieHeader: vi.fn(async () => 'auth=1'),
}))

describe('public API helper contracts', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.unstubAllGlobals()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('fetchPublicPageBySlug encodes slugs and returns null on non-ok responses', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce({ ok: false })
      .mockResolvedValueOnce({ ok: true, json: async () => ({ slug: '한글 slug', title: 'Title' }) })

    vi.stubGlobal('fetch', fetchMock)
    const { fetchPublicPageBySlug } = await import('@/lib/api/pages')

    await expect(fetchPublicPageBySlug('missing page')).resolves.toBeNull()
    await expect(fetchPublicPageBySlug('한글 slug')).resolves.toMatchObject({ title: 'Title' })
    expect(fetchMock).toHaveBeenLastCalledWith('http://localhost/api/public/pages/%ED%95%9C%EA%B8%80%20slug', { cache: 'no-store' })
  })

  it('site settings helpers return null on failure and surface parsed payload on success', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce({ ok: false })
      .mockResolvedValueOnce({ ok: true, json: async () => ({ ownerName: 'Woong', tagline: 'Creative Technologist' }) })
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          id: 'resume-1',
          publicUrl: '/media/public-resume/resume.pdf',
          fileName: 'resume.pdf',
          path: 'public-resume/resume.pdf',
        }),
      })

    vi.stubGlobal('fetch', fetchMock)
    const { fetchPublicSiteSettings, fetchResume } = await import('@/lib/api/site-settings')

    await expect(fetchPublicSiteSettings()).resolves.toBeNull()
    await expect(fetchPublicSiteSettings()).resolves.toMatchObject({ ownerName: 'Woong' })
    await expect(fetchResume()).resolves.toMatchObject({
      id: 'resume-1',
      fileName: 'resume.pdf',
      path: 'public-resume/resume.pdf',
    })
  })

  it('blog and work detail helpers preserve encoded paths and null semantics', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce({ ok: true, json: async () => ({ slug: 'seeded-work', title: 'Work' }) })
      .mockResolvedValueOnce({ ok: false })
      .mockResolvedValueOnce({ ok: true, json: async () => ({ slug: 'seeded-blog', title: 'Blog' }) })

    vi.stubGlobal('fetch', fetchMock)
    const { fetchPublicWorkBySlug } = await import('@/lib/api/works')
    const { fetchPublicBlogBySlug } = await import('@/lib/api/blogs')

    await expect(fetchPublicWorkBySlug('seeded-work')).resolves.toMatchObject({ title: 'Work' })
    await expect(fetchPublicBlogBySlug('missing-blog')).resolves.toBeNull()
    await expect(fetchPublicBlogBySlug('seeded-blog')).resolves.toMatchObject({ title: 'Blog' })

    expect(fetchMock).toHaveBeenNthCalledWith(1, 'http://localhost/api/public/works/seeded-work', { cache: 'no-store' })
    expect(fetchMock).toHaveBeenNthCalledWith(2, 'http://localhost/api/public/blogs/missing-blog', { cache: 'no-store' })
    expect(fetchMock).toHaveBeenNthCalledWith(3, 'http://localhost/api/public/blogs/seeded-blog', { cache: 'no-store' })
  })
})
