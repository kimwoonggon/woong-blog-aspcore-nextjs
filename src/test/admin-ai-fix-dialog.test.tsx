import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { AIFixDialog } from '@/components/admin/AIFixDialog'

const mocks = vi.hoisted(() => ({
  fetchWithCsrf: vi.fn(),
  fetchAdminAiRuntimeConfigBrowser: vi.fn(),
  toast: {
    error: vi.fn(),
    success: vi.fn(),
  },
}))

vi.mock('@/lib/api/auth', () => ({
  fetchWithCsrf: mocks.fetchWithCsrf,
}))

vi.mock('@/lib/api/admin-ai', () => ({
  fetchAdminAiRuntimeConfigBrowser: mocks.fetchAdminAiRuntimeConfigBrowser,
}))

vi.mock('@/components/admin/TiptapEditor', () => ({
  TiptapEditor: ({ content }: { content: string }) => <div data-testid="mock-tiptap">{content}</div>,
}))

vi.mock('sonner', () => ({ toast: mocks.toast }))

function makeJsonResponse(payload: unknown, ok = true) {
  return new Response(JSON.stringify(payload), {
    status: ok ? 200 : 400,
    headers: { 'content-type': 'application/json' },
  })
}

describe('AIFixDialog', () => {
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
      defaultSystemPrompt: 'Default blog system prompt',
      defaultBlogFixPrompt: 'Default blog system prompt',
      defaultWorkEnrichPrompt: 'Work prompt for {title}',
    })
    mocks.fetchWithCsrf.mockResolvedValue(makeJsonResponse({ fixedHtml: '<p>fixed</p>' }))
  })

  it('prefills and sends the editable custom system prompt', async () => {
    render(<AIFixDialog content="<p>draft</p>" onApply={vi.fn()} />)

    fireEvent.click(screen.getByRole('button', { name: 'AI Content Fixer' }))

    await waitFor(() => {
      expect(screen.getByLabelText('AI system prompt')).toHaveValue('Default blog system prompt')
    })

    fireEvent.change(screen.getByLabelText('AI system prompt'), {
      target: { value: 'Apply this single-fix prompt.' },
    })
    fireEvent.click(screen.getByRole('button', { name: 'Save prompt' }))
    fireEvent.click(screen.getByRole('button', { name: 'Start AI Fix' }))

    await waitFor(() => {
      expect(mocks.fetchWithCsrf).toHaveBeenCalled()
    })

    const [, request] = mocks.fetchWithCsrf.mock.calls[0] as [string, { body: string }]
    expect(JSON.parse(request.body)).toMatchObject({
      html: '<p>draft</p>',
      provider: 'codex',
      codexModel: 'gpt-5.4',
      codexReasoningEffort: 'medium',
      customPrompt: 'Apply this single-fix prompt.',
    })
    expect(window.localStorage.getItem('admin-ai-blog-fix-system-prompt')).toBe('Apply this single-fix prompt.')
  })

  it('requires saving prompt edits before generating', async () => {
    render(<AIFixDialog content="<p>draft</p>" onApply={vi.fn()} />)

    fireEvent.click(screen.getByRole('button', { name: 'AI Content Fixer' }))

    await waitFor(() => {
      expect(screen.getByLabelText('AI system prompt')).toHaveValue('Default blog system prompt')
    })

    fireEvent.change(screen.getByLabelText('AI system prompt'), {
      target: { value: 'Unsaved prompt.' },
    })
    fireEvent.click(screen.getByRole('button', { name: 'Start AI Fix' }))

    expect(mocks.toast.error).toHaveBeenCalledWith('Save the system prompt before generating an AI fix.')
    expect(mocks.fetchWithCsrf).not.toHaveBeenCalled()
  })

  it('shows OpenAI and Codex provider options when the runtime config allows both', async () => {
    mocks.fetchAdminAiRuntimeConfigBrowser.mockResolvedValueOnce({
      provider: 'openai',
      availableProviders: ['openai', 'codex'],
      defaultModel: 'gpt-4.1',
      codexModel: 'gpt-5.4',
      codexReasoningEffort: 'medium',
      allowedCodexModels: ['gpt-5.4'],
      allowedCodexReasoningEfforts: ['low', 'medium', 'high'],
      batchConcurrency: 2,
      batchCompletedRetentionDays: 14,
      defaultSystemPrompt: 'Default blog system prompt',
      defaultBlogFixPrompt: 'Default blog system prompt',
      defaultWorkEnrichPrompt: 'Work prompt for {title}',
    })

    render(<AIFixDialog content="<p>draft</p>" onApply={vi.fn()} />)

    fireEvent.click(screen.getByRole('button', { name: 'AI Content Fixer' }))

    const providerSelect = await screen.findByLabelText('AI provider')
    expect(providerSelect).toHaveValue('openai')
    expect(screen.getByRole('option', { name: 'OPENAI' })).toBeInTheDocument()
    expect(screen.getByRole('option', { name: 'CODEX' })).toBeInTheDocument()
  })

  it('saves and restores the custom system prompt', async () => {
    render(<AIFixDialog content="<p>draft</p>" onApply={vi.fn()} />)

    fireEvent.click(screen.getByRole('button', { name: 'AI Content Fixer' }))

    await waitFor(() => {
      expect(screen.getByLabelText('AI system prompt')).toHaveValue('Default blog system prompt')
    })

    fireEvent.change(screen.getByLabelText('AI system prompt'), {
      target: { value: 'Saved single prompt.' },
    })
    fireEvent.click(screen.getByRole('button', { name: 'Save prompt' }))

    expect(window.localStorage.getItem('admin-ai-blog-fix-system-prompt')).toBe('Saved single prompt.')
    expect(mocks.toast.success).toHaveBeenCalledWith('System prompt saved')

    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))
    fireEvent.click(screen.getByRole('button', { name: 'AI Content Fixer' }))

    await waitFor(() => {
      expect(screen.getByLabelText('AI system prompt')).toHaveValue('Saved single prompt.')
    })
  })

  it('keeps the edited prompt when AI Enrich generates a fix', async () => {
    render(
      <AIFixDialog
        content="<p>work draft</p>"
        onApply={vi.fn()}
        apiEndpoint="/api/admin/ai/work-enrich"
        title="AI Enrich"
        extraBodyParams={{ title: 'Work title' }}
      />,
    )

    fireEvent.click(screen.getByRole('button', { name: 'AI Enrich' }))

    await waitFor(() => {
      expect(screen.getByLabelText('AI system prompt')).toHaveValue('Work prompt for Work title')
    })

    fireEvent.change(screen.getByLabelText('AI system prompt'), {
      target: { value: 'Saved enrich prompt.' },
    })
    fireEvent.click(screen.getByRole('button', { name: 'Save prompt' }))
    fireEvent.click(screen.getByRole('button', { name: 'Start AI Fix' }))

    await waitFor(() => {
      expect(mocks.fetchWithCsrf).toHaveBeenCalled()
    })

    const [, request] = mocks.fetchWithCsrf.mock.calls[0] as [string, { body: string }]
    expect(JSON.parse(request.body)).toMatchObject({
      html: '<p>work draft</p>',
      title: 'Work title',
      customPrompt: 'Saved enrich prompt.',
    })
    expect(window.localStorage.getItem('admin-ai-work-enrich-system-prompt')).toBe('Saved enrich prompt.')
    expect(screen.getByLabelText('AI system prompt')).toHaveValue('Saved enrich prompt.')
  })
})
