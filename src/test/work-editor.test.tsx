import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { WorkEditor } from '@/components/admin/WorkEditor'

const mocks = vi.hoisted(() => ({
  push: vi.fn(),
  replace: vi.fn(),
  refresh: vi.fn(),
  back: vi.fn(),
  pathname: '/admin/works/new',
  fetchWithCsrf: vi.fn(),
  extractVideoFrameThumbnailBlob: vi.fn(),
  fetchRemoteImageBlob: vi.fn(),
  toast: {
    error: vi.fn(),
    success: vi.fn(),
  },
}))

vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: mocks.push, replace: mocks.replace, refresh: mocks.refresh, back: mocks.back }),
  usePathname: () => mocks.pathname,
  useSearchParams: () => new URLSearchParams('returnTo=%2Fadmin%2Fworks'),
}))

vi.mock('next/image', () => ({
  default: ({ src, alt, ...props }: { src: string; alt: string }) => <img src={src} alt={alt} {...props} />,
}))

vi.mock('sonner', () => ({ toast: mocks.toast }))

vi.mock('@/lib/api/browser', () => ({
  getBrowserApiBaseUrl: () => '/api',
}))

vi.mock('@/lib/api/auth', () => ({
  fetchWithCsrf: mocks.fetchWithCsrf,
}))

vi.mock('@/lib/content/work-auto-thumbnail', () => ({
  extractVideoFrameThumbnailBlob: mocks.extractVideoFrameThumbnailBlob,
  fetchRemoteImageBlob: mocks.fetchRemoteImageBlob,
}))

vi.mock('@/components/content/WorkVideoPlayer', () => ({
  WorkVideoPlayer: ({ video }: { video: { sourceType?: string } }) => (
    <div
      data-testid="mock-work-video-player"
      title={video.sourceType === 'youtube' ? 'YouTube video' : 'Uploaded video'}
    />
  ),
}))

vi.mock('@/components/admin/TiptapEditor', async () => {
  const React = await import('react')

  return {
    TiptapEditor: ({
      content,
      onChange,
      workVideos = [],
      insertVideoEmbedRequest,
      onVideoInsertHandled,
    }: {
      content: string
      onChange: (value: string) => void
      workVideos?: Array<{ id: string }>
      insertVideoEmbedRequest?: { videoId: string; nonce: number } | null
      onVideoInsertHandled?: (result: { inserted: boolean; reason?: 'duplicate' | 'missing' }) => void
    }) => {
      React.useEffect(() => {
        if (!insertVideoEmbedRequest) {
          return
        }

        const videoExists = workVideos.some((video) => video.id === insertVideoEmbedRequest.videoId)
        if (!videoExists) {
          onVideoInsertHandled?.({ inserted: false, reason: 'missing' })
          return
        }

        if (content.includes(`data-video-id="${insertVideoEmbedRequest.videoId}"`)) {
          onVideoInsertHandled?.({ inserted: false, reason: 'duplicate' })
          return
        }

        onChange(`${content}<work-video-embed data-video-id="${insertVideoEmbedRequest.videoId}"></work-video-embed>`)
        onVideoInsertHandled?.({ inserted: true })
      }, [content, insertVideoEmbedRequest, onChange, onVideoInsertHandled, workVideos])

      return (
        <textarea
          aria-label="Mock work content"
          value={content}
          onChange={(event) => onChange(event.target.value)}
        />
      )
    },
  }
})

describe('WorkEditor', () => {
  const changeContent = (value: string) => {
    fireEvent.change(screen.getByLabelText('Mock work content'), {
      target: { value },
    })
  }

  const addMetadataField = (key: string, value: string) => {
    fireEvent.click(screen.getByRole('button', { name: /Add Field/i }))
    fireEvent.change(screen.getAllByLabelText('Key')[0], { target: { value: key } })
    fireEvent.change(screen.getAllByLabelText('Value')[0], { target: { value } })
  }

  beforeEach(() => {
    vi.clearAllMocks()
    mocks.pathname = '/admin/works/new'
    vi.stubGlobal('fetch', vi.fn())
    mocks.extractVideoFrameThumbnailBlob.mockResolvedValue(new Blob(['thumb'], { type: 'image/jpeg' }))
    mocks.fetchRemoteImageBlob.mockResolvedValue(new Blob(['thumb'], { type: 'image/jpeg' }))
    mocks.fetchWithCsrf.mockResolvedValue({
      ok: true,
      headers: new Headers({ 'content-type': 'application/json' }),
      json: async () => ({}),
      text: async () => '',
    })
  })

  afterEach(() => {
    cleanup()
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

  it('accepts flexible metadata through structured key/value inputs', async () => {
    render(<WorkEditor />)

    expect(screen.queryByLabelText('Flexible Metadata (JSON)')).not.toBeInTheDocument()
    addMetadataField('role', 'Lead Frontend Engineer')
    expect(screen.getAllByLabelText('Key')[0]).toHaveValue('role')
    expect(screen.getAllByLabelText('Value')[0]).toHaveValue('Lead Frontend Engineer')
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
    addMetadataField('score', '1')
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
          allPropertiesJson: JSON.stringify({ score: '1' }),
          thumbnailAssetId: null,
          iconAssetId: null,
        }),
      }),
    )
    expect(mocks.push).toHaveBeenCalledWith('/admin/works')
  })

  it('allows save completion after video-only edits on an existing work', async () => {
    mocks.fetchWithCsrf
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
        json: async () => ({
          videos_version: 1,
          videos: [{
            id: 'video-1',
            sourceType: 'r2',
            sourceKey: 'videos/work-1/demo.mp4',
            playbackUrl: 'https://example.com/demo.mp4',
            mimeType: 'video/mp4',
            sortOrder: 0,
          }],
        }),
        text: async () => '',
      })
      .mockResolvedValueOnce({
        ok: true,
        headers: new Headers({ 'content-type': 'application/json' }),
        json: async () => ({ id: 'thumb-1', url: '/media/work-thumbnails/thumb-1.jpg' }),
        text: async () => '',
      })
      .mockResolvedValueOnce({
        ok: true,
        headers: new Headers({ 'content-type': 'application/json' }),
        json: async () => ({ id: 'work-1', slug: 'existing-work' }),
        text: async () => '',
      })

    render(
      <WorkEditor
        initialWork={{
          id: 'work-1',
          title: 'Existing work',
          slug: 'existing-work',
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

    const saveButton = screen.getByRole('button', { name: /Update Work/i })
    expect(saveButton).toBeDisabled()

    const fileInput = screen.getByLabelText('Upload MP4 Video')
    const file = new File(['\x00\x00\x00\x18ftypmp42'], 'demo.mp4', { type: 'video/mp4' })
    fireEvent.change(fileInput, { target: { files: [file] } })

    await waitFor(() => {
      expect(saveButton).toBeEnabled()
    })

    fireEvent.click(saveButton)

    await waitFor(() => {
      expect(mocks.push).toHaveBeenCalledWith('/admin/works')
    })
    expect(mocks.toast.success).toHaveBeenCalledWith('Work updated successfully')
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
      .mockResolvedValueOnce({
        ok: true,
        headers: new Headers({ 'content-type': 'application/json' }),
        json: async () => ({ id: 'thumb-1', url: '/media/work-thumbnails/thumb-1.jpg' }),
        text: async () => '',
      })
      .mockResolvedValueOnce({
        ok: true,
        headers: new Headers({ 'content-type': 'application/json' }),
        json: async () => ({ id: 'work-1', slug: 'work-title' }),
        text: async () => '',
      })

    render(<WorkEditor />)

    fireEvent.change(screen.getByLabelText('Title'), { target: { value: 'Work title' } })
    changeContent('<p>Hello</p>')

    const fileInput = screen.getByLabelText('Upload MP4 Video')
    const file = new File(['\x00\x00\x00\x18ftypmp42'], 'demo.mp4', { type: 'video/mp4' })
    fireEvent.change(fileInput, { target: { files: [file] } })

    expect(screen.getByText('demo.mp4')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /Create Work/i })).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: /Create with Videos/i })).toBeEnabled()
    fireEvent.click(screen.getByRole('button', { name: /Create with Videos/i }))

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
    expect(mocks.fetchWithCsrf).toHaveBeenNthCalledWith(
      5,
      '/api/uploads',
      expect.objectContaining({ method: 'POST', body: expect.any(FormData) }),
    )
    expect(mocks.fetchWithCsrf).toHaveBeenNthCalledWith(
      6,
      '/api/admin/works/work-1',
      expect.objectContaining({ method: 'PUT' }),
    )
    expect(mocks.toast.success).toHaveBeenCalledWith('Work and videos created successfully')
    expect(mocks.push).toHaveBeenCalledWith('/admin/works/work-1?videoInline=1')
  })

  it('uses onSaved instead of redirecting for public inline text-only create', async () => {
    const onSaved = vi.fn()

    mocks.fetchWithCsrf.mockResolvedValueOnce({
      ok: true,
      headers: new Headers({ 'content-type': 'application/json' }),
      json: async () => ({ id: 'work-1', slug: 'inline-work-title' }),
      text: async () => '',
    })

    render(<WorkEditor inlineMode onSaved={onSaved} />)

    fireEvent.change(screen.getByLabelText('Title'), { target: { value: 'Inline Work Title' } })
    changeContent('<p>Hello</p>')
    fireEvent.click(screen.getByRole('button', { name: /Create Work/i }))

    await waitFor(() => {
      expect(onSaved).toHaveBeenCalledWith({
        id: 'work-1',
        slug: 'inline-work-title',
        isEditing: false,
      })
    })

    expect(mocks.push).not.toHaveBeenCalled()
    expect(mocks.refresh).not.toHaveBeenCalled()
  })

  it('uses onSaved instead of redirecting for public inline create with staged videos', async () => {
    const onSaved = vi.fn()

    mocks.fetchWithCsrf
      .mockResolvedValueOnce({
        ok: true,
        headers: new Headers({ 'content-type': 'application/json' }),
        json: async () => ({ id: 'work-1', slug: 'inline-video-work' }),
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
      .mockResolvedValueOnce({
        ok: true,
        headers: new Headers({ 'content-type': 'application/json' }),
        json: async () => ({ id: 'thumb-1', url: '/media/work-thumbnails/thumb-1.jpg' }),
        text: async () => '',
      })
      .mockResolvedValueOnce({
        ok: true,
        headers: new Headers({ 'content-type': 'application/json' }),
        json: async () => ({ id: 'work-1', slug: 'inline-video-work' }),
        text: async () => '',
      })

    render(<WorkEditor inlineMode onSaved={onSaved} />)

    fireEvent.change(screen.getByLabelText('Title'), { target: { value: 'Inline video work' } })
    changeContent('<p>Hello</p>')
    const fileInput = screen.getByLabelText('Upload MP4 Video')
    const file = new File(['\x00\x00\x00\x18ftypmp42'], 'demo.mp4', { type: 'video/mp4' })
    fireEvent.change(fileInput, { target: { files: [file] } })

    fireEvent.click(screen.getByRole('button', { name: /Create with Videos/i }))

    await waitFor(() => {
      expect(onSaved).toHaveBeenCalledWith({
        id: 'work-1',
        slug: 'inline-video-work',
        isEditing: false,
      })
    })

    expect(mocks.push).not.toHaveBeenCalled()
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
    expect(screen.getByRole('img', { name: 'Work thumbnail preview' })).toBeInTheDocument()
    })

    expect(mocks.fetchWithCsrf).toHaveBeenCalledWith(
      '/api/uploads',
      expect.objectContaining({ method: 'POST', body: expect.any(FormData) }),
    )

    fireEvent.click(screen.getByRole('button', { name: /Remove Thumbnail/i }))
    expect(screen.queryByRole('img', { name: 'Work thumbnail preview' })).not.toBeInTheDocument()
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
    await waitFor(() => {
      expect(screen.getByTestId('mock-work-video-player')).toHaveAttribute('title', 'YouTube video')
    })
  })

  it('auto-generates a thumbnail from an uploaded video when there is no explicit thumbnail', async () => {
    mocks.fetchWithCsrf
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
        json: async () => ({
          videos_version: 1,
          videos: [{
            id: 'video-1',
            sourceType: 'r2',
            sourceKey: 'videos/work-1/demo.mp4',
            playbackUrl: 'https://example.com/demo.mp4',
            mimeType: 'video/mp4',
            sortOrder: 0,
          }],
        }),
        text: async () => '',
      })
      .mockResolvedValueOnce({
        ok: true,
        headers: new Headers({ 'content-type': 'application/json' }),
        json: async () => ({ id: 'thumb-1', url: '/media/work-thumbnails/thumb-1.jpg' }),
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
          videos_version: 0,
          videos: [],
        }}
      />,
    )

    const fileInput = screen.getByLabelText('Upload MP4 Video')
    const file = new File(['\x00\x00\x00\x18ftypmp42'], 'demo.mp4', { type: 'video/mp4' })
    fireEvent.change(fileInput, { target: { files: [file] } })

    await waitFor(() => {
      expect(mocks.fetchWithCsrf).toHaveBeenNthCalledWith(
        4,
        '/api/uploads',
        expect.objectContaining({ method: 'POST', body: expect.any(FormData) }),
      )
    })

    expect(mocks.extractVideoFrameThumbnailBlob).toHaveBeenCalledWith(file)
    expect(screen.getByTestId('work-thumbnail-source')).toHaveTextContent('uploaded video')
    await waitFor(() => {
      expect(screen.getByRole('img', { name: 'Work thumbnail preview' })).toHaveAttribute('src', '/media/work-thumbnails/thumb-1.jpg')
    })
  })

  it('persists auto-generated uploaded-video thumbnails immediately for existing works', async () => {
    mocks.fetchWithCsrf
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
        json: async () => ({
          videos_version: 1,
          videos: [{
            id: 'video-1',
            sourceType: 'r2',
            sourceKey: 'videos/work-1/demo.mp4',
            playbackUrl: 'https://example.com/demo.mp4',
            mimeType: 'video/mp4',
            sortOrder: 0,
          }],
        }),
        text: async () => '',
      })
      .mockResolvedValueOnce({
        ok: true,
        headers: new Headers({ 'content-type': 'application/json' }),
        json: async () => ({ id: 'thumb-1', url: '/media/work-thumbnails/thumb-1.jpg' }),
        text: async () => '',
      })
      .mockResolvedValueOnce({
        ok: true,
        headers: new Headers({ 'content-type': 'application/json' }),
        json: async () => ({ id: 'work-1', slug: 'existing-work' }),
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
          videos_version: 0,
          videos: [],
        }}
      />,
    )

    const fileInput = screen.getByLabelText('Upload MP4 Video')
    const file = new File(['\x00\x00\x00\x18ftypmp42'], 'demo.mp4', { type: 'video/mp4' })
    fireEvent.change(fileInput, { target: { files: [file] } })

    await waitFor(() => {
      expect(mocks.fetchWithCsrf).toHaveBeenNthCalledWith(
        5,
        '/api/admin/works/work-1',
        expect.objectContaining({ method: 'PUT' }),
      )
    })
  })

  it('renders explicit copy that videos save immediately in edit mode', () => {
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

    expect(screen.getByText(/Videos save immediately\./i)).toBeInTheDocument()
  })

  it('does not auto-generate a thumbnail when an explicit thumbnail already exists', async () => {
    mocks.fetchWithCsrf.mockResolvedValueOnce({
      ok: true,
      headers: new Headers({ 'content-type': 'application/json' }),
      json: async () => ({
        videos_version: 3,
        videos: [{
          id: 'video-1',
          sourceType: 'youtube',
          sourceKey: 'dQw4w9WgXcQ',
          sortOrder: 0,
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
          thumbnail_asset_id: 'thumb-manual',
          thumbnail_url: '/media/work-thumbnails/manual.jpg',
          videos_version: 2,
          videos: [],
        }}
      />,
    )

    fireEvent.change(screen.getByLabelText('YouTube URL or ID'), { target: { value: 'https://youtu.be/dQw4w9WgXcQ' } })
    fireEvent.click(screen.getByRole('button', { name: /Add YouTube Video/i }))

    await waitFor(() => {
      expect(mocks.toast.success).toHaveBeenCalledWith('YouTube video added.')
    })

    expect(mocks.fetchRemoteImageBlob).not.toHaveBeenCalled()
    expect(screen.getByTestId('work-thumbnail-source')).toHaveTextContent('manual')
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

    fireEvent.click(screen.getByRole('button', { name: /Move YouTube dQw4w9WgXcQ down/i }))

    await waitFor(() => {
      expect(mocks.fetchWithCsrf).toHaveBeenCalledWith(
        '/api/admin/works/work-1/videos/order',
        expect.objectContaining({ method: 'PUT' }),
      )
      expect(mocks.toast.error).toHaveBeenCalledWith('Videos changed. Refresh and retry.')
    })
  })

  it('inserts a saved video into the body and marks it as placed', async () => {
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
          ],
        }}
      />,
    )

    fireEvent.click(screen.getByRole('button', { name: 'Insert Into Body' }))

    await waitFor(() => {
      expect(mocks.toast.success).toHaveBeenCalledWith('Video inserted into the body.')
    })

    expect(screen.getByLabelText('Mock work content')).toHaveValue('<p>Existing</p><work-video-embed data-video-id="video-1"></work-video-embed>')
    expect(screen.getByText(/Placed in body\. Remove it from the body before deleting the saved video\./i)).toBeInTheDocument()
  })

  it('blocks deleting a video that is still placed in the body', async () => {
    render(
      <WorkEditor
        initialWork={{
          id: 'work-1',
          title: 'Existing work',
          category: 'platform',
          tags: [],
          published: true,
          content: { html: '<p>Existing</p><work-video-embed data-video-id="video-1"></work-video-embed>' },
          all_properties: {},
          videos_version: 2,
          videos: [
            {
              id: 'video-1',
              sourceType: 'youtube',
              sourceKey: 'dQw4w9WgXcQ',
              sortOrder: 0,
            },
          ],
        }}
      />,
    )

    fireEvent.click(screen.getByRole('button', { name: /Remove YouTube dQw4w9WgXcQ/i }))

    expect(mocks.toast.error).toHaveBeenCalledWith('Remove this video from the body before deleting it.')
    expect(mocks.fetchWithCsrf).not.toHaveBeenCalled()
  })
})
