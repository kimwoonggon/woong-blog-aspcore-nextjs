import { act, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { AdminBlogBatchAiPanel } from '@/components/admin/AdminBlogBatchAiPanel'

const mocks = vi.hoisted(() => ({
  fetchWithCsrf: vi.fn(),
  fetchAdminAiRuntimeConfigBrowser: vi.fn(),
  listBlogAiBatchJobsBrowser: vi.fn(),
  getBlogAiBatchJobBrowser: vi.fn(),
  toast: {
    error: vi.fn(),
    success: vi.fn(),
  },
}))

vi.mock('@/lib/api/auth', () => ({
  fetchWithCsrf: mocks.fetchWithCsrf,
}))

vi.mock('@/lib/api/browser', () => ({
  getBrowserApiBaseUrl: () => '/api',
}))

vi.mock('@/lib/api/admin-ai', () => ({
  fetchAdminAiRuntimeConfigBrowser: mocks.fetchAdminAiRuntimeConfigBrowser,
  listBlogAiBatchJobsBrowser: mocks.listBlogAiBatchJobsBrowser,
  getBlogAiBatchJobBrowser: mocks.getBlogAiBatchJobBrowser,
}))

vi.mock('sonner', () => ({ toast: mocks.toast }))

function makeTextResponse(payload: unknown, ok = true) {
  return {
    ok,
    text: async () => JSON.stringify(payload),
  }
}

describe('AdminBlogBatchAiPanel', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    window.localStorage.clear()

    mocks.fetchAdminAiRuntimeConfigBrowser.mockResolvedValue({
      provider: 'codex',
      availableProviders: ['openai', 'codex'],
      defaultModel: 'gpt-5.4',
      codexModel: 'gpt-5.4',
      codexReasoningEffort: 'medium',
      allowedCodexModels: ['gpt-5.4'],
      allowedCodexReasoningEfforts: ['low', 'medium', 'high'],
      batchConcurrency: 2,
      batchCompletedRetentionDays: 14,
    })
    mocks.listBlogAiBatchJobsBrowser.mockResolvedValue({
      jobs: [],
      runningCount: 0,
      queuedCount: 0,
      completedCount: 0,
      failedCount: 0,
      cancelledCount: 0,
    })
    mocks.getBlogAiBatchJobBrowser.mockResolvedValue({
      jobId: 'job-1',
      status: 'completed',
      selectionMode: 'selected',
      selectionLabel: '1 selected',
      selectionKey: 'selected:blog-1',
      autoApply: false,
      workerCount: 2,
      totalCount: 1,
      processedCount: 1,
      succeededCount: 1,
      failedCount: 0,
      provider: 'codex',
      model: 'gpt-5.4',
      reasoningEffort: 'medium',
      createdAt: '2026-04-10T00:00:00.000Z',
      cancelRequested: false,
      items: [],
    })
    mocks.fetchWithCsrf.mockResolvedValue(makeTextResponse({ jobId: 'job-1' }))
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  function renderPanel() {
    return render(
      <AdminBlogBatchAiPanel
        isOpen
        selectedBlogIds={['blog-1']}
        selectedBlogTitles={['First blog']}
        availableBlogs={[
          { id: 'blog-1', title: 'First blog', publishedAt: '2026-04-01T00:00:00.000Z' },
          { id: 'blog-2', title: 'Second blog', updatedAt: '2026-04-05T00:00:00.000Z' },
          { id: 'blog-3', title: 'Third blog', publishedAt: '2026-04-11T00:00:00.000Z' },
        ]}
      />,
    )
  }

  it('creates a range batch job using the computed selection ids', async () => {
    renderPanel()

    await waitFor(() => {
      expect(mocks.fetchAdminAiRuntimeConfigBrowser).toHaveBeenCalled()
    })

    fireEvent.change(screen.getByLabelText('Mode'), { target: { value: 'range' } })
    fireEvent.change(screen.getByLabelText('Batch range start'), { target: { value: '2' } })
    fireEvent.change(screen.getByLabelText('Batch range count'), { target: { value: '1' } })
    fireEvent.click(screen.getByRole('button', { name: /Generate AI Fix job/i }))

    await waitFor(() => {
      expect(mocks.fetchWithCsrf).toHaveBeenCalled()
    })

    const [, request] = mocks.fetchWithCsrf.mock.calls[0] as [string, { body: string }]
    expect(JSON.parse(request.body)).toMatchObject({
      blogIds: ['blog-2'],
      selectionMode: 'range',
      selectionLabel: 'range 2-2',
      provider: 'codex',
      workerCount: 2,
      codexModel: 'gpt-5.4',
      codexReasoningEffort: 'medium',
    })
  })

  it('shows provider selection when multiple providers are available', async () => {
    renderPanel()

    await waitFor(() => {
      expect(mocks.fetchAdminAiRuntimeConfigBrowser).toHaveBeenCalled()
    })

    const providerSelect = screen.getByLabelText('Batch AI provider')
    expect(providerSelect).toBeInTheDocument()
    expect(screen.getByRole('option', { name: 'OPENAI' })).toBeInTheDocument()
    expect(screen.getByRole('option', { name: 'CODEX' })).toBeInTheDocument()
  })

  it('restores a saved provider from localStorage when it is still allowed', async () => {
    window.localStorage.setItem('admin-ai-provider', 'codex')

    renderPanel()

    await waitFor(() => {
      expect(mocks.fetchAdminAiRuntimeConfigBrowser).toHaveBeenCalled()
    })

    expect(screen.getByLabelText('Batch AI provider')).toHaveValue('codex')
    expect(screen.getByLabelText('Batch AI provider')).toBeInTheDocument()
    expect(screen.getByLabelText('Codex model')).toBeInTheDocument()
    expect(screen.getByLabelText('Blog batch codex reasoning')).toBeInTheDocument()
    expect(screen.getByLabelText('Workers')).toBeInTheDocument()
  })

  it('falls back to the first allowed provider when localStorage contains a stale value', async () => {
    window.localStorage.setItem('admin-ai-provider', 'azure')

    renderPanel()

    await waitFor(() => {
      expect(mocks.fetchAdminAiRuntimeConfigBrowser).toHaveBeenCalled()
    })

    expect(screen.getByLabelText('Batch AI provider')).toHaveValue('openai')
    expect(screen.queryByLabelText('Codex model')).not.toBeInTheDocument()
    expect(screen.queryByLabelText('Blog batch codex reasoning')).not.toBeInTheDocument()
    expect(screen.queryByLabelText('Workers')).not.toBeInTheDocument()
  })

  it('uses the selected provider in the batch job payload and hides codex-only controls for openai', async () => {
    renderPanel()

    await waitFor(() => {
      expect(mocks.fetchAdminAiRuntimeConfigBrowser).toHaveBeenCalled()
    })

    fireEvent.change(screen.getByLabelText('Batch AI provider'), { target: { value: 'openai' } })

    expect(screen.queryByLabelText('Codex model')).not.toBeInTheDocument()
    expect(screen.queryByLabelText('Blog batch codex reasoning')).not.toBeInTheDocument()
    expect(screen.queryByLabelText('Workers')).not.toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: /Generate AI Fix job/i }))

    await waitFor(() => {
      expect(mocks.fetchWithCsrf).toHaveBeenCalled()
    })

    const [, request] = mocks.fetchWithCsrf.mock.calls[0] as [string, { body: string }]
    expect(JSON.parse(request.body)).toMatchObject({
      provider: 'openai',
    })
    expect(window.localStorage.getItem('admin-ai-provider')).toBe('openai')
  })

  it('blocks date batch creation when no date bounds are set', async () => {
    renderPanel()

    await waitFor(() => {
      expect(mocks.fetchAdminAiRuntimeConfigBrowser).toHaveBeenCalled()
    })

    fireEvent.change(screen.getByLabelText('Mode'), { target: { value: 'date' } })
    fireEvent.click(screen.getByRole('button', { name: /Generate AI Fix job/i }))

    expect(mocks.toast.error).toHaveBeenCalledWith('Set a start date or end date before creating a date-range batch job')
    expect(mocks.fetchWithCsrf).not.toHaveBeenCalled()
  })

  it('polls running jobs and clears the interval on unmount', async () => {
    vi.useFakeTimers()

    mocks.listBlogAiBatchJobsBrowser.mockResolvedValue({
      jobs: [{
        jobId: 'job-1',
        status: 'running',
        selectionMode: 'selected',
        selectionLabel: '1 selected',
        selectionKey: 'selected:blog-1',
        autoApply: false,
        workerCount: 2,
        totalCount: 1,
        processedCount: 0,
        succeededCount: 0,
        failedCount: 0,
        provider: 'codex',
        model: 'gpt-5.4',
        reasoningEffort: 'medium',
        createdAt: '2026-04-10T00:00:00.000Z',
        cancelRequested: false,
      }],
      runningCount: 1,
      queuedCount: 0,
      completedCount: 0,
      failedCount: 0,
      cancelledCount: 0,
    })
    mocks.getBlogAiBatchJobBrowser.mockResolvedValue({
      jobId: 'job-1',
      status: 'running',
      selectionMode: 'selected',
      selectionLabel: '1 selected',
      selectionKey: 'selected:blog-1',
      autoApply: false,
      workerCount: 2,
      totalCount: 1,
      processedCount: 0,
      succeededCount: 0,
      failedCount: 0,
      provider: 'codex',
      model: 'gpt-5.4',
      reasoningEffort: 'medium',
      createdAt: '2026-04-10T00:00:00.000Z',
      cancelRequested: false,
      items: [],
    })

    const view = renderPanel()

    await act(async () => {
      await Promise.resolve()
      await Promise.resolve()
    })

    const initialListCalls = mocks.listBlogAiBatchJobsBrowser.mock.calls.length
    const initialDetailCalls = mocks.getBlogAiBatchJobBrowser.mock.calls.length

    expect(initialListCalls).toBeGreaterThanOrEqual(1)
    expect(initialDetailCalls).toBeGreaterThanOrEqual(1)

    await act(async () => {
      vi.advanceTimersByTime(2000)
      await Promise.resolve()
      await Promise.resolve()
    })

    const postPollListCalls = mocks.listBlogAiBatchJobsBrowser.mock.calls.length
    const postPollDetailCalls = mocks.getBlogAiBatchJobBrowser.mock.calls.length

    expect(postPollListCalls).toBeGreaterThan(initialListCalls)
    expect(postPollDetailCalls).toBeGreaterThan(initialDetailCalls)

    view.unmount()

    await act(async () => {
      vi.advanceTimersByTime(4000)
      await Promise.resolve()
      await Promise.resolve()
    })

    expect(mocks.listBlogAiBatchJobsBrowser).toHaveBeenCalledTimes(postPollListCalls)
    expect(mocks.getBlogAiBatchJobBrowser).toHaveBeenCalledTimes(postPollDetailCalls)
  })
})
