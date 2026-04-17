import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { BlogEditor } from '@/components/admin/BlogEditor'

const mocks = vi.hoisted(() => ({
  push: vi.fn(),
  replace: vi.fn(),
  refresh: vi.fn(),
  back: vi.fn(),
  pathname: '/blog',
  searchParams: '',
  fetchWithCsrf: vi.fn(),
  toast: {
    error: vi.fn(),
    success: vi.fn(),
  },
}))

vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: mocks.push, replace: mocks.replace, refresh: mocks.refresh, back: mocks.back }),
  usePathname: () => mocks.pathname,
  useSearchParams: () => new URLSearchParams(mocks.searchParams),
}))

vi.mock('sonner', () => ({ toast: mocks.toast }))

vi.mock('@/lib/api/browser', () => ({
  getBrowserApiBaseUrl: () => '/api',
}))

vi.mock('@/lib/api/auth', () => ({
  fetchWithCsrf: mocks.fetchWithCsrf,
}))

vi.mock('@/components/admin/AIFixDialog', () => ({
  AIFixDialog: () => null,
}))

vi.mock('@/components/admin/AuthoringCapabilityHints', () => ({
  AuthoringCapabilityHints: () => null,
}))

vi.mock('@/components/admin/TiptapEditor', () => ({
  TiptapEditor: ({ content, onChange }: { content: string; onChange: (value: string) => void }) => (
    <textarea aria-label="Mock blog content" value={content} onChange={(event) => onChange(event.target.value)} />
  ),
}))

describe('BlogEditor', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mocks.pathname = '/blog'
    mocks.searchParams = ''
    mocks.fetchWithCsrf.mockResolvedValue({
      ok: true,
      json: async () => ({}),
      text: async () => '',
    })
  })

  it('normalizes wrapped markdown into html before create save', async () => {
    render(<BlogEditor />)

    fireEvent.change(screen.getByLabelText('Title'), { target: { value: 'Markdown Blog' } })
    fireEvent.change(screen.getByLabelText('Mock blog content'), {
      target: { value: '<p>## 저장 제목</p><p>- 첫 번째</p><p>- 두 번째</p>' },
    })

    fireEvent.click(screen.getByRole('button', { name: /Create Post/i }))

    await waitFor(() => {
      expect(mocks.fetchWithCsrf).toHaveBeenCalledWith(
        '/api/admin/blogs',
        expect.objectContaining({
          method: 'POST',
          body: JSON.stringify({
            title: 'Markdown Blog',
            excerpt: '',
            tags: [],
            published: true,
            contentJson: JSON.stringify({
              html: '<h2>저장 제목</h2>\n<ul><li>첫 번째</li><li>두 번째</li></ul>',
            }),
          }),
        }),
      )
    })
  })

  it('refreshes the router after an inline save outside the public blog routes', async () => {
    mocks.pathname = '/admin/blog/new'

    render(<BlogEditor inlineMode />)

    fireEvent.change(screen.getByLabelText('Title'), { target: { value: 'Inline Blog' } })
    fireEvent.change(screen.getByLabelText('Mock blog content'), {
      target: { value: '<p>Inline body</p>' },
    })

    fireEvent.click(screen.getByRole('button', { name: /Create Post/i }))

    await waitFor(() => {
      expect(mocks.fetchWithCsrf).toHaveBeenCalledWith(
        '/api/admin/blogs',
        expect.objectContaining({
          method: 'POST',
          body: JSON.stringify({
            title: 'Inline Blog',
            excerpt: '',
            tags: [],
            published: true,
            contentJson: JSON.stringify({
              html: '<p>Inline body</p>',
            }),
          }),
        }),
      )
    })

    expect(mocks.refresh).toHaveBeenCalled()
    expect(mocks.push).not.toHaveBeenCalled()
  })

  it('returns to a safe inline returnTo path after save', async () => {
    mocks.pathname = '/blog'
    mocks.searchParams = 'returnTo=%2Fblog%3Fpage%3D2%26pageSize%3D12&relatedPage=2'

    render(<BlogEditor inlineMode />)

    fireEvent.change(screen.getByLabelText('Title'), { target: { value: 'Inline Return' } })
    fireEvent.change(screen.getByLabelText('Mock blog content'), {
      target: { value: '<p>Inline return body</p>' },
    })

    fireEvent.click(screen.getByRole('button', { name: /Create Post/i }))

    await waitFor(() => {
      expect(mocks.fetchWithCsrf).toHaveBeenCalled()
    })

    expect(mocks.push).toHaveBeenCalledWith('/blog?page=2&pageSize=12')
    expect(mocks.refresh).not.toHaveBeenCalled()
  })

  it('returns public inline creates to the first blog page with the current page size', async () => {
    mocks.pathname = '/blog'
    mocks.searchParams = 'page=3&pageSize=2'
    const onSaved = vi.fn()

    render(<BlogEditor inlineMode onSaved={onSaved} />)

    fireEvent.change(screen.getByLabelText('Title'), { target: { value: 'Inline Create Return' } })
    fireEvent.change(screen.getByLabelText('Mock blog content'), {
      target: { value: '<p>Inline create return body</p>' },
    })

    fireEvent.click(screen.getByRole('button', { name: /Create Post/i }))

    await waitFor(() => {
      expect(mocks.fetchWithCsrf).toHaveBeenCalled()
    })

    expect(onSaved).toHaveBeenCalled()
    expect(mocks.push).toHaveBeenCalledWith('/blog?page=1&pageSize=2')
    expect(mocks.refresh).toHaveBeenCalled()
  })

  it('ignores an unsafe returnTo path and falls back to the admin list', async () => {
    mocks.pathname = '/admin/blog/123'
    mocks.searchParams = 'returnTo=%2F%2Fevil.example'

    render(
      <BlogEditor
        initialBlog={{
          id: 'blog-1',
          slug: 'safe-slug',
          title: 'Existing title',
          content: { html: '<p>Old body</p>' },
        }}
      />,
    )

    fireEvent.change(screen.getByLabelText('Title'), { target: { value: 'Updated title' } })
    fireEvent.click(screen.getByRole('button', { name: /Update Post/i }))

    await waitFor(() => {
      expect(mocks.fetchWithCsrf).toHaveBeenCalled()
    })

    expect(mocks.push).toHaveBeenCalledWith('/admin/blog')
    expect(mocks.push).not.toHaveBeenCalledWith('//evil.example')
  })
})
