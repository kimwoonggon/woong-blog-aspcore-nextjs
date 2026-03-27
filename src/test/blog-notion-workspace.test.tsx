import { fireEvent, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { ReactNode } from 'react'
import { BlogNotionWorkspace } from '@/components/admin/BlogNotionWorkspace'
import { fetchWithCsrf } from '@/lib/api/auth'

const refreshMock = vi.fn()

vi.mock('next/link', () => ({
  default: ({ href, children, ...props }: { href: string; children: ReactNode }) => (
    <a href={href} {...props}>
      {children}
    </a>
  ),
}))

vi.mock('next/navigation', () => ({
  useRouter: () => ({
    refresh: refreshMock,
  }),
}))

vi.mock('sonner', () => ({
  toast: {
    error: vi.fn(),
    success: vi.fn(),
  },
}))

vi.mock('@/lib/api/auth', () => ({
  fetchWithCsrf: vi.fn(),
}))

vi.mock('@/lib/api/browser', () => ({
  getBrowserApiBaseUrl: () => 'http://localhost/api',
}))

vi.mock('@/lib/api/admin-ai', () => ({
  fetchAdminAiRuntimeConfigBrowser: vi.fn(async () => ({
    provider: 'codex',
    defaultModel: 'gpt-5.4',
    codexModel: 'gpt-5.4',
    codexReasoningEffort: 'medium',
    allowedCodexModels: ['gpt-5.4'],
    allowedCodexReasoningEfforts: ['low', 'medium', 'high', 'xhigh'],
  })),
}))

vi.mock('@/components/admin/TiptapEditor', () => ({
  TiptapEditor: () => <div data-testid="mock-tiptap-editor" />,
}))

describe('BlogNotionWorkspace selection state', () => {
  afterEach(() => {
    vi.clearAllMocks()
  })

  const blogs = [
    {
      id: 'blog-1',
      title: 'First blog',
      slug: 'first-blog',
      published: true,
      publishedAt: '2024-01-01T00:00:00.000Z',
      updatedAt: '2024-01-02T00:00:00.000Z',
      tags: ['tag-a'],
      excerpt: 'excerpt',
      content: { html: '<p>First</p>' },
    },
    {
      id: 'blog-2',
      title: 'Second blog',
      slug: 'second-blog',
      published: false,
      publishedAt: null,
      updatedAt: '2024-01-03T00:00:00.000Z',
      tags: ['tag-b'],
      excerpt: 'excerpt',
      content: { html: '<p>Second</p>' },
    },
  ]

  it('tracks selection count and summary while keeping the active editor intact', () => {
    render(
      <BlogNotionWorkspace
        blogs={blogs}
        activeBlog={blogs[0]}
      />,
    )

    expect(screen.getByTestId('batch-selection-count')).toHaveTextContent('Selected 0 posts')
    expect(screen.getByTestId('batch-selection-summary')).toHaveTextContent('No posts selected yet')
    expect(screen.getByTestId('mock-tiptap-editor')).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: /select all/i }))
    expect(screen.getByTestId('batch-selection-count')).toHaveTextContent('Selected 2 posts')
    expect(screen.getByTestId('batch-selection-summary')).toHaveTextContent('Ready for future batch actions: First blog · Second blog')

    fireEvent.click(screen.getByRole('button', { name: /clear selection/i }))
    expect(screen.getByTestId('batch-selection-count')).toHaveTextContent('Selected 0 posts')
  })

  it('submits selected blog ids to the batch AI endpoint and renders the result summary', async () => {
    vi.mocked(fetchWithCsrf).mockResolvedValue({
      ok: true,
      json: async () => ({
        results: [
          { blogId: 'blog-1', status: 'fixed' },
          { blogId: 'blog-2', status: 'fixed' },
        ],
      }),
    } as Response)

    render(
      <BlogNotionWorkspace
        blogs={blogs}
        activeBlog={blogs[0]}
      />,
    )

    fireEvent.click(screen.getByRole('button', { name: /select all/i }))
    fireEvent.click(screen.getByRole('button', { name: /ai fix selected/i }))

    expect(fetchWithCsrf).toHaveBeenCalledWith(
      'http://localhost/api/admin/ai/blog-fix-batch',
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({
          blogIds: ['blog-1', 'blog-2'],
          all: false,
          apply: true,
          codexModel: 'gpt-5.4',
          codexReasoningEffort: 'medium',
        }),
      }),
    )

    expect(await screen.findByTestId('batch-ai-status')).toHaveTextContent('AI Fix applied to 2 posts.')
    expect(refreshMock).toHaveBeenCalled()
  })
})
