import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { AdminBlogBatchAiPanel } from '@/components/admin/AdminBlogBatchAiPanel'
import { getAdminAiErrorMessage } from '@/lib/api/admin-ai'

const refreshMock = vi.fn()

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

vi.mock('@/lib/api/browser', () => ({
  getBrowserApiBaseUrl: () => '/api',
}))

vi.mock('@/lib/api/auth', () => ({
  fetchWithCsrf: vi.fn(),
}))

vi.mock('@/lib/api/admin-ai', async () => {
  const actual = await vi.importActual<typeof import('@/lib/api/admin-ai')>('@/lib/api/admin-ai')
  return {
    ...actual,
    fetchAdminAiRuntimeConfigBrowser: vi.fn(async () => ({
      provider: 'codex',
      defaultModel: 'gpt-5.4',
      codexModel: 'gpt-5.4',
      codexReasoningEffort: 'medium',
      allowedCodexModels: ['gpt-5.4'],
      allowedCodexReasoningEfforts: ['low', 'medium', 'high', 'xhigh'],
      batchConcurrency: 2,
      batchCompletedRetentionDays: 3,
    })),
    listBlogAiBatchJobsBrowser: vi.fn(async () => ({
      jobs: [{
        jobId: 'job-1',
        status: 'completed',
        selectionMode: 'selected',
        selectionLabel: '2 selected',
        selectionKey: 'sha256:test',
        autoApply: false,
        workerCount: 2,
        totalCount: 2,
        processedCount: 2,
        succeededCount: 1,
        failedCount: 1,
        provider: 'codex',
        model: 'gpt-5.4',
        reasoningEffort: 'medium',
        createdAt: '2026-03-30T00:00:00.000Z',
        startedAt: null,
        finishedAt: null,
        cancelRequested: false,
      }],
      runningCount: 0,
      queuedCount: 0,
      completedCount: 1,
      failedCount: 0,
      cancelledCount: 0,
    })),
    getBlogAiBatchJobBrowser: vi.fn(async () => ({
      jobId: 'job-1',
      status: 'completed',
      selectionMode: 'selected',
      selectionLabel: '2 selected',
      selectionKey: 'sha256:test',
      autoApply: false,
      workerCount: 2,
      totalCount: 2,
      processedCount: 2,
      succeededCount: 1,
      failedCount: 1,
      provider: 'codex',
      model: 'gpt-5.4',
      reasoningEffort: 'medium',
      createdAt: '2026-03-30T00:00:00.000Z',
      startedAt: null,
      finishedAt: null,
      cancelRequested: false,
      items: [
        {
          jobItemId: 'item-1',
          blogId: 'blog-1',
          title: 'Blog 1',
          status: 'succeeded',
          fixedHtml: '<p>fixed</p>',
          error: null,
          provider: 'codex',
          model: 'gpt-5.4',
          reasoningEffort: 'medium',
          appliedAt: null,
        },
        {
          jobItemId: 'item-2',
          blogId: 'blog-2',
          title: 'Blog 2',
          status: 'failed',
          fixedHtml: null,
          error: 'boom',
          provider: null,
          model: null,
          reasoningEffort: null,
          appliedAt: null,
        },
      ],
    })),
  }
})

describe('admin AI helpers and batch panel', () => {
  afterEach(() => {
    vi.clearAllMocks()
  })

  it('prefers payload detail/title fallback when direct error is missing', () => {
    expect(getAdminAiErrorMessage({ detail: 'detail message' }, 'fallback')).toBe('detail message')
    expect(getAdminAiErrorMessage({ title: 'title message' }, 'fallback')).toBe('title message')
    expect(getAdminAiErrorMessage({}, 'fallback')).toBe('fallback')
  })

  it('shows Apply all successful when a completed job still has unapplied fixed HTML', async () => {
    render(
      <AdminBlogBatchAiPanel
        isOpen
        selectedBlogIds={['blog-1', 'blog-2']}
        selectedBlogTitles={['Blog 1', 'Blog 2']}
        availableBlogs={[
          { id: 'blog-1', title: 'Blog 1' },
          { id: 'blog-2', title: 'Blog 2' },
        ]}
      />,
    )

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'Apply all successful' })).toBeVisible()
    })
  })
})
