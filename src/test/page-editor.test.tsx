import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { PageEditor } from '@/components/admin/PageEditor'

const mocks = vi.hoisted(() => ({
  refresh: vi.fn(),
  fetchWithCsrf: vi.fn(),
  toast: {
    error: vi.fn(),
    success: vi.fn(),
    loading: vi.fn(() => 'toast-id'),
  },
}))

vi.mock('next/navigation', () => ({
  useRouter: () => ({ refresh: mocks.refresh }),
}))

vi.mock('sonner', () => ({ toast: mocks.toast }))

vi.mock('@/lib/api/auth', () => ({
  fetchWithCsrf: mocks.fetchWithCsrf,
}))

describe('PageEditor', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mocks.fetchWithCsrf.mockResolvedValue({
      ok: true,
      text: async () => '',
    })
  })

  it('renders inline mode with existing html content', () => {
    render(
      <PageEditor
        inlineMode
        page={{ id: 'page-1', title: 'Introduction', slug: 'introduction', content: { html: '<p>Hello</p>' } }}
      />,
    )

    expect(screen.getByText('introduction Inline Editor')).toBeInTheDocument()
    expect(screen.getByLabelText('Title')).toHaveValue('Introduction')
    expect(screen.getByLabelText('Content (HTML/Text)')).toHaveValue('<p>Hello</p>')
  })

  it('falls back to an empty html value when content is not an html object', () => {
    render(
      <PageEditor
        page={{ id: 'page-1', title: 'Contact', slug: 'contact', content: 'legacy-text-content' }}
      />,
    )

    expect(screen.getByText('contact Page')).toBeInTheDocument()
    expect(screen.getByLabelText('Content (HTML/Text)')).toHaveValue('')
  })

  it('also falls back to empty html when page content is null', () => {
    render(
      <PageEditor
        page={{ id: 'page-2', title: 'About', slug: 'about', content: null }}
      />,
    )

    expect(screen.getByLabelText('Content (HTML/Text)')).toHaveValue('')
  })

  it('falls back to empty html when page content is an object without an html field', () => {
    render(
      <PageEditor
        page={{ id: 'page-3', title: 'Services', slug: 'services', content: { markdown: 'legacy' } }}
      />,
    )

    expect(screen.getByLabelText('Content (HTML/Text)')).toHaveValue('')
  })

  it('saves page content and refreshes the router on success', async () => {
    const onSaved = vi.fn()

    render(
      <PageEditor
        page={{ id: 'page-1', title: 'Contact', slug: 'contact', content: { html: '<p>Old</p>' } }}
        onSaved={onSaved}
      />,
    )

    fireEvent.change(screen.getByLabelText('Title'), { target: { value: 'Updated contact' } })
    fireEvent.change(screen.getByLabelText('Content (HTML/Text)'), { target: { value: '<p>Updated</p>' } })
    fireEvent.click(screen.getByRole('button', { name: /Save Changes/i }))

    await waitFor(() => {
      expect(mocks.toast.success).toHaveBeenCalledWith('Page updated successfully!', { id: 'toast-id' })
    })

    expect(mocks.fetchWithCsrf).toHaveBeenCalledWith(
      '/api/admin/pages',
      expect.objectContaining({
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          id: 'page-1',
          title: 'Updated contact',
          contentJson: JSON.stringify({ html: '<p>Updated</p>' }),
        }),
      }),
    )
    expect(mocks.refresh).toHaveBeenCalled()
    expect(onSaved).toHaveBeenCalled()
  })

  it('shows the backend error body when the save response is not ok', async () => {
    mocks.fetchWithCsrf.mockResolvedValueOnce({
      ok: false,
      text: async () => 'validation failed',
    })

    render(
      <PageEditor
        page={{ id: 'page-1', title: 'Contact', slug: 'contact', content: { html: '<p>Old</p>' } }}
      />,
    )

    fireEvent.click(screen.getByRole('button', { name: /Save Changes/i }))

    await waitFor(() => {
      expect(mocks.toast.error).toHaveBeenCalledWith('Error saving page: validation failed', { id: 'toast-id' })
    })
  })

  it('shows a fatal save error when the request throws', async () => {
    mocks.fetchWithCsrf.mockRejectedValueOnce(new Error('network down'))

    render(
      <PageEditor
        page={{ id: 'page-1', title: 'Contact', slug: 'contact', content: { html: '<p>Old</p>' } }}
      />,
    )

    fireEvent.click(screen.getByRole('button', { name: /Save Changes/i }))

    await waitFor(() => {
      expect(mocks.toast.error).toHaveBeenCalledWith('A fatal error occurred while saving.', { id: 'toast-id' })
    })
  })
})
