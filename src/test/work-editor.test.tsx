import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { WorkEditor } from '@/components/admin/WorkEditor'

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
  useSearchParams: () => new URLSearchParams('returnTo=%2Fadmin%2Fworks'),
}))

vi.mock('sonner', () => ({ toast: mocks.toast }))

vi.mock('@/lib/api/browser', () => ({
  getBrowserApiBaseUrl: () => '/api',
}))

vi.mock('@/lib/api/auth', () => ({
  fetchWithCsrf: mocks.fetchWithCsrf,
}))

describe('WorkEditor', () => {
  const changeContent = (value: string) => {
    fireEvent.change(document.querySelector('#content') as HTMLTextAreaElement, {
      target: { value },
    })
  }

  beforeEach(() => {
    vi.clearAllMocks()
    mocks.fetchWithCsrf.mockResolvedValue({
      ok: true,
      json: async () => ({}),
      text: async () => '',
    })
  })

  it('blocks save when flexible metadata is invalid json', async () => {
    render(<WorkEditor />)

    fireEvent.change(screen.getByLabelText('Title'), { target: { value: 'Work title' } })
    fireEvent.change(screen.getByLabelText('Flexible Metadata (JSON)'), { target: { value: '{broken-json' } })

    fireEvent.click(screen.getByRole('button', { name: /Create Work/i }))

    expect(mocks.toast.error).toHaveBeenCalledWith('Invalid JSON in Flexible Metadata field')
    expect(mocks.fetchWithCsrf).not.toHaveBeenCalled()
  })

  it('shows backend save failure and does not navigate away', async () => {
    mocks.fetchWithCsrf.mockResolvedValueOnce({
      ok: false,
      text: async () => 'Save failed from backend',
    })

    render(<WorkEditor />)

    fireEvent.change(screen.getByLabelText('Title'), { target: { value: 'Work title' } })
    fireEvent.change(screen.getByLabelText('Flexible Metadata (JSON)'), { target: { value: '{}' } })
    changeContent('<p>Hello</p>')

    fireEvent.click(screen.getByRole('button', { name: /Create Work/i }))

    await waitFor(() => {
      expect(mocks.toast.error).toHaveBeenCalledWith('Save failed from backend')
    })
    expect(mocks.push).not.toHaveBeenCalled()
  })

  it('creates a work and normalizes tags and metadata before returning to the previous list flow', async () => {
    render(<WorkEditor />)

    fireEvent.change(screen.getByLabelText('Title'), { target: { value: 'Work title' } })
    fireEvent.change(screen.getByLabelText('Project Period'), { target: { value: '2024.01 - 2024.03' } })
    fireEvent.change(screen.getByLabelText('Tags (comma separated)'), {
      target: { value: 'alpha, beta ,, gamma ' },
    })
    fireEvent.change(screen.getByLabelText('Flexible Metadata (JSON)'), {
      target: { value: '{\n  "score": 1\n}' },
    })
    changeContent('<p>Hello</p>')

    fireEvent.click(screen.getByRole('button', { name: /Create Work/i }))

    await waitFor(() => {
      expect(mocks.toast.success).toHaveBeenCalledWith('Work created successfully')
    })

    expect(mocks.fetchWithCsrf).toHaveBeenCalledWith(
      '/api/admin/works',
      expect.objectContaining({
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          title: 'Work title',
          category: 'Uncategorized',
          period: '2024.01 - 2024.03',
          tags: ['alpha', 'beta', 'gamma'],
          published: true,
          contentJson: JSON.stringify({ html: '<p>Hello</p>' }),
          allPropertiesJson: JSON.stringify({ score: 1 }),
          thumbnailAssetId: null,
          iconAssetId: null,
        }),
      }),
    )
    expect(mocks.push).toHaveBeenCalledWith('/admin/works')
    expect(mocks.back).not.toHaveBeenCalled()
  })

  it('updates an existing work without navigating when inline mode is enabled', async () => {
    render(
      <WorkEditor
        inlineMode
        initialWork={{
          id: 'work-1',
          title: 'Existing work',
          category: 'platform',
          tags: ['alpha'],
          published: true,
          publishedAt: '2024-01-01T00:00:00.000Z',
          updatedAt: '2024-01-02T00:00:00.000Z',
          content: { html: '<p>Existing</p>' },
          all_properties: { score: 1 },
          thumbnail_url: '/thumb.png',
          icon_url: '/icon.png',
          thumbnail_asset_id: 'thumb-1',
          icon_asset_id: 'icon-1',
        }}
      />,
    )

    fireEvent.change(screen.getByLabelText('Title'), { target: { value: 'Updated work' } })
    const updateButton = screen.getByRole('button', { name: /Update Work/i })
    expect(updateButton).not.toBeDisabled()
    fireEvent.click(updateButton)

    await waitFor(() => {
      expect(mocks.fetchWithCsrf).toHaveBeenCalledWith(
        '/api/admin/works/work-1',
        expect.objectContaining({ method: 'PUT' }),
      )
      expect(mocks.toast.success).toHaveBeenCalledWith('Work updated successfully')
    })

    expect(screen.queryByRole('button', { name: /Cancel/i })).not.toBeInTheDocument()
    expect(screen.getAllByText(/January/i)).toHaveLength(2)
    expect(mocks.push).not.toHaveBeenCalled()
    expect(mocks.refresh).toHaveBeenCalled()
  })

  it('uploads a thumbnail preview and lets the user remove it', async () => {
    mocks.fetchWithCsrf.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ id: 'thumb-1', url: '/media/thumb-1.png' }),
    })

    render(<WorkEditor />)

    const thumbnailInput = screen.getByLabelText('Thumbnail Image')
    const file = new File(['thumb'], 'thumb.png', { type: 'image/png' })
    fireEvent.change(thumbnailInput, { target: { files: [file] } })

    await waitFor(() => {
      expect(screen.getByAltText('Work thumbnail preview')).toBeInTheDocument()
    })

    expect(mocks.fetchWithCsrf).toHaveBeenCalledWith(
      '/api/uploads',
      expect.objectContaining({ method: 'POST', body: expect.any(FormData) }),
    )

    fireEvent.click(screen.getByRole('button', { name: /Remove Thumbnail/i }))
    expect(screen.queryByAltText('Work thumbnail preview')).not.toBeInTheDocument()
  })

  it('shows a toast when icon upload fails', async () => {
    mocks.fetchWithCsrf.mockResolvedValueOnce({
      ok: false,
      json: async () => ({ error: 'icon upload failed' }),
    })

    render(<WorkEditor />)

    const iconInput = screen.getByLabelText('Icon Image')
    const file = new File(['icon'], 'icon.png', { type: 'image/png' })
    fireEvent.change(iconInput, { target: { files: [file] } })

    await waitFor(() => {
      expect(mocks.toast.error).toHaveBeenCalledWith('Failed to upload icon: icon upload failed')
    })
  })

  it('uploads and removes an icon preview', async () => {
    mocks.fetchWithCsrf.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ id: 'icon-1', url: '/media/icon-1.png' }),
    })

    render(<WorkEditor />)

    const iconInput = screen.getByLabelText('Icon Image')
    const file = new File(['icon'], 'icon.png', { type: 'image/png' })
    fireEvent.change(iconInput, { target: { files: [file] } })

    await waitFor(() => {
      expect(screen.getByAltText('Work icon preview')).toBeInTheDocument()
    })

    fireEvent.click(screen.getByRole('button', { name: /Remove Icon/i }))
    expect(screen.queryByAltText('Work icon preview')).not.toBeInTheDocument()
  })

  it('creates new work as published by default and returns to the works list when cancel is pressed', async () => {
    render(<WorkEditor />)

    fireEvent.change(screen.getByLabelText('Title'), { target: { value: 'Work title' } })
    expect(screen.getByLabelText('Category')).toHaveValue('Uncategorized')
    expect(screen.queryByLabelText('Published')).not.toBeInTheDocument()
    expect(screen.getByText(/New works publish immediately when you save/i)).toBeInTheDocument()
    changeContent('<p>Hello</p>')
    fireEvent.click(screen.getByRole('button', { name: /Create Work/i }))

    await waitFor(() => {
      expect(mocks.fetchWithCsrf).toHaveBeenCalledWith(
        '/api/admin/works',
        expect.objectContaining({
          body: expect.stringContaining('"published":true'),
        }),
      )
    })

    fireEvent.click(screen.getByRole('button', { name: /Cancel/i }))
    expect(mocks.push).toHaveBeenCalledWith('/admin/works')
  })

  it('falls back to the default category when the user clears it', async () => {
    render(<WorkEditor />)

    fireEvent.change(screen.getByLabelText('Title'), { target: { value: 'Work title' } })
    fireEvent.change(screen.getByLabelText('Category'), { target: { value: '' } })
    changeContent('<p>Hello</p>')

    fireEvent.click(screen.getByRole('button', { name: /Create Work/i }))

    await waitFor(() => {
      expect(mocks.fetchWithCsrf).toHaveBeenCalledWith(
        '/api/admin/works',
        expect.objectContaining({
          body: expect.stringContaining('"category":"Uncategorized"'),
        }),
      )
    })
  })
})
