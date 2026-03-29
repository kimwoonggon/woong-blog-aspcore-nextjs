import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { BlogEditor } from '@/components/admin/BlogEditor'

const mocks = vi.hoisted(() => ({
  push: vi.fn(),
  replace: vi.fn(),
  refresh: vi.fn(),
  back: vi.fn(),
  fetchWithCsrf: vi.fn(),
  toast: {
    error: vi.fn(),
    success: vi.fn(),
  },
}))

vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: mocks.push, replace: mocks.replace, refresh: mocks.refresh, back: mocks.back }),
  usePathname: () => '/blog',
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
})
