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
    vi.stubGlobal('fetch', vi.fn())
    mocks.fetchWithCsrf.mockResolvedValue({
      ok: true,
      headers: new Headers({ 'content-type': 'application/json' }),
      json: async () => ({}),
      text: async () => '',
    })
  })

  it('surfaces a cloudflare cors hint when direct object upload throws', async () => {
    mocks.fetchWithCsrf
      .mockResolvedValueOnce({
        ok: true,
        headers: new Headers({ 'content-type': 'application/json' }),
        json: async () => ({
          uploadSessionId: 'session-1',
          uploadMethod: 'PUT',
          uploadUrl: 'https://example.r2.cloudflarestorage.com/bucket/video.mp4',
          storageKey: 'videos/work-1/video.mp4',
        }),
        text: async () => '',
      })

    vi.stubGlobal('fetch', vi.fn(async () => {
      throw new TypeError('Failed to fetch')
    }) as typeof fetch)

    render(
      <WorkEditor
        initialWork={{
          id: 'work-1',
          title: 'Existing work',
          category: 'platform',
          tags: [],
          published: true,
          content: { html: '<p>Existing</p>' },
          all_properties: {},
          videos_version: 0,
          videos: [],
        }}
      />,
    )

    const fileInput = screen.getByLabelText('Upload MP4 Video')
    const file = new File(['\x00\x00\x00\x18ftypmp42'], 'demo.mp4', { type: 'video/mp4' })
    fireEvent.change(fileInput, { target: { files: [file] } })

    await waitFor(() => {
      expect(mocks.toast.error).toHaveBeenCalledWith('Browser upload to Cloudflare R2 failed. Check bucket CORS for Origin, PUT, and Content-Type.')
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

  it('creates a work and normalizes tags and metadata before returning to the list', async () => {
    mocks.fetchWithCsrf.mockResolvedValueOnce({
      ok: true,
      headers: new Headers({ 'content-type': 'application/json' }),
      json: async () => ({ id: 'work-1', slug: 'work-title' }),
      text: async () => '',
    })

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
  })

  it('stages create-time videos and runs create-plus-attach flow', async () => {
    mocks.fetchWithCsrf
      .mockResolvedValueOnce({
        ok: true,
        headers: new Headers({ 'content-type': 'application/json' }),
        json: async () => ({ id: 'work-1', slug: 'work-title' }),
        text: async () => '',
      })
      .mockResolvedValueOnce({
        ok: true,
        headers: new Headers({ 'content-type': 'application/json' }),
        json: async () => ({
          uploadSessionId: 'session-1',
          uploadMethod: 'POST',
          uploadUrl: '/api/admin/works/work-1/videos/upload?uploadSessionId=session-1',
          storageKey: 'videos/work-1/session-1.mp4',
        }),
        text: async () => '',
      })
      .mockResolvedValueOnce({
        ok: true,
        headers: new Headers({ 'content-type': 'application/json' }),
        json: async () => ({ success: true }),
        text: async () => '',
      })
      .mockResolvedValueOnce({
        ok: true,
        headers: new Headers({ 'content-type': 'application/json' }),
        json: async () => ({ videos_version: 1, videos: [] }),
        text: async () => '',
      })

    render(<WorkEditor />)

    fireEvent.change(screen.getByLabelText('Title'), { target: { value: 'Work title' } })
    changeContent('<p>Hello</p>')

    const fileInput = screen.getByLabelText('Upload MP4 Video')
    const file = new File(['\x00\x00\x00\x18ftypmp42'], 'demo.mp4', { type: 'video/mp4' })
    fireEvent.change(fileInput, { target: { files: [file] } })

    expect(screen.getByText('demo.mp4')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /Create Work/i })).toBeDisabled()

    fireEvent.click(screen.getByRole('button', { name: /Create And Add Videos/i }))

    await waitFor(() => {
      expect(mocks.fetchWithCsrf).toHaveBeenNthCalledWith(
        2,
        '/api/admin/works/work-1/videos/upload-url',
        expect.objectContaining({ method: 'POST' }),
      )
    })

    expect(mocks.fetchWithCsrf).toHaveBeenNthCalledWith(
      3,
      '/api/admin/works/work-1/videos/upload?uploadSessionId=session-1',
      expect.objectContaining({ method: 'POST', body: expect.any(FormData) }),
    )
    expect(mocks.fetchWithCsrf).toHaveBeenNthCalledWith(
      4,
      '/api/admin/works/work-1/videos/confirm',
      expect.objectContaining({ method: 'POST' }),
    )
    expect(mocks.toast.success).toHaveBeenCalledWith('Work and videos created successfully')
    expect(mocks.push).toHaveBeenCalledWith('/admin/works')
  })

  it('uploads a thumbnail preview and lets the user remove it', async () => {
    mocks.fetchWithCsrf.mockResolvedValueOnce({
      ok: true,
      headers: new Headers({ 'content-type': 'application/json' }),
      json: async () => ({ id: 'thumb-1', url: '/media/thumb-1.png' }),
      text: async () => '',
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

  it('adds a YouTube video for an existing work', async () => {
    mocks.fetchWithCsrf.mockResolvedValueOnce({
      ok: true,
      headers: new Headers({ 'content-type': 'application/json' }),
      json: async () => ({
        videos_version: 3,
        videos: [{
          id: 'video-1',
          sourceType: 'youtube',
          sourceKey: 'dQw4w9WgXcQ',
          playbackUrl: null,
          originalFileName: null,
          mimeType: null,
          fileSize: null,
          sortOrder: 0,
          createdAt: '2026-04-10T00:00:00.000Z',
        }],
      }),
      text: async () => '',
    })

    render(
      <WorkEditor
        initialWork={{
          id: 'work-1',
          title: 'Existing work',
          category: 'platform',
          tags: [],
          published: true,
          content: { html: '<p>Existing</p>' },
          all_properties: {},
          videos_version: 2,
          videos: [],
        }}
      />,
    )

    fireEvent.change(screen.getByLabelText('YouTube URL or ID'), { target: { value: 'https://youtu.be/dQw4w9WgXcQ' } })
    fireEvent.click(screen.getByRole('button', { name: /Add YouTube Video/i }))

    await waitFor(() => {
      expect(mocks.fetchWithCsrf).toHaveBeenCalledWith(
        '/api/admin/works/work-1/videos/youtube',
        expect.objectContaining({
          method: 'POST',
          body: JSON.stringify({
            youtubeUrlOrId: 'https://youtu.be/dQw4w9WgXcQ',
            expectedVideosVersion: 2,
          }),
        }),
      )
    })

    expect(mocks.toast.success).toHaveBeenCalledWith('YouTube video added.')
    expect(screen.getByTitle(/YouTube video/i)).toBeInTheDocument()
  })

  it('surfaces reorder conflicts for existing videos', async () => {
    mocks.fetchWithCsrf.mockResolvedValueOnce({
      ok: false,
      headers: new Headers({ 'content-type': 'application/json' }),
      json: async () => ({ error: 'Videos changed. Refresh and retry.' }),
      text: async () => '',
    })

    render(
      <WorkEditor
        initialWork={{
          id: 'work-1',
          title: 'Existing work',
          category: 'platform',
          tags: [],
          published: true,
          content: { html: '<p>Existing</p>' },
          all_properties: {},
          videos_version: 2,
          videos: [
            {
              id: 'video-1',
              sourceType: 'youtube',
              sourceKey: 'dQw4w9WgXcQ',
              sortOrder: 0,
            },
            {
              id: 'video-2',
              sourceType: 'youtube',
              sourceKey: '9bZkp7q19f0',
              sortOrder: 1,
            },
          ],
        }}
      />,
    )

    fireEvent.click(screen.getAllByRole('button', { name: 'Move Down' })[0])

    await waitFor(() => {
      expect(mocks.toast.error).toHaveBeenCalledWith('Videos changed. Refresh and retry.')
    })
  })
})
