import { render, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { WorkVideoPlayer } from '@/components/content/WorkVideoPlayer'

const hlsMocks = vi.hoisted(() => ({
  attachMedia: vi.fn(),
  destroy: vi.fn(),
  isSupported: vi.fn(() => true),
  loadSource: vi.fn(),
  constructor: vi.fn(),
}))

vi.mock('hls.js', () => {
  hlsMocks.constructor.mockImplementation(function MockHls() {
    return {
      attachMedia: hlsMocks.attachMedia,
      destroy: hlsMocks.destroy,
      loadSource: hlsMocks.loadSource,
    }
  })

  return {
    default: Object.assign(hlsMocks.constructor, {
      isSupported: hlsMocks.isSupported,
    }),
  }
})

describe('WorkVideoPlayer', () => {
  beforeEach(() => {
    hlsMocks.attachMedia.mockClear()
    hlsMocks.constructor.mockClear()
    hlsMocks.destroy.mockClear()
    hlsMocks.isSupported.mockClear()
    hlsMocks.isSupported.mockReturnValue(true)
    hlsMocks.loadSource.mockClear()
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('renders YouTube videos with the nocookie embed domain', () => {
    render(
      <WorkVideoPlayer
        video={{
          id: 'video-1',
          sourceType: 'youtube',
          sourceKey: 'dQw4w9WgXcQ',
          sortOrder: 0,
        }}
      />,
    )

    expect(screen.getByTitle(/YouTube video/i)).toHaveAttribute('src', 'https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ')
  })

  it('renders uploaded videos with a native video source', () => {
    const { container } = render(
      <WorkVideoPlayer
        video={{
          id: 'video-2',
          sourceType: 'local',
          sourceKey: 'videos/work-1/demo.mp4',
          playbackUrl: '/media/videos/work-1/demo.mp4',
          mimeType: 'video/mp4',
          sortOrder: 0,
        }}
      />,
    )

    expect(container.querySelector('video')).toBeTruthy()
  })

  it('reduces direct download affordances on native video controls', () => {
    const { container } = render(
      <WorkVideoPlayer
        video={{
          id: 'video-guarded',
          sourceType: 'hls',
          sourceKey: 'local:videos/work-1/hls/master.m3u8',
          playbackUrl: '/media/videos/work-1/hls/master.m3u8',
          mimeType: 'application/vnd.apple.mpegurl',
          sortOrder: 0,
        }}
      />,
    )

    const video = container.querySelector('video')
    expect(video).toHaveAttribute('controlsList', 'nodownload noremoteplayback')
    expect(video).toHaveAttribute('disablePictureInPicture')
  })

  it('uses native HLS playback when the browser supports it', async () => {
    vi.spyOn(HTMLMediaElement.prototype, 'canPlayType').mockReturnValue('probably')

    const { container } = render(
      <WorkVideoPlayer
        video={{
          id: 'video-3',
          sourceType: 'hls',
          sourceKey: 'local:videos/work-1/hls/master.m3u8',
          playbackUrl: '/media/videos/work-1/hls/master.m3u8',
          mimeType: 'application/vnd.apple.mpegurl',
          sortOrder: 0,
        }}
      />,
    )

    const video = container.querySelector('video')
    await waitFor(() => expect(video).toHaveAttribute('src', '/media/videos/work-1/hls/master.m3u8'))
    expect(hlsMocks.constructor).not.toHaveBeenCalled()
  })

  it('lazily attaches hls.js when native HLS playback is unavailable', async () => {
    vi.spyOn(HTMLMediaElement.prototype, 'canPlayType').mockReturnValue('')

    const { container } = render(
      <WorkVideoPlayer
        video={{
          id: 'video-4',
          sourceType: 'hls',
          sourceKey: 'r2:videos/work-1/hls/master.m3u8',
          playbackUrl: 'https://cdn.example.com/videos/work-1/hls/master.m3u8',
          mimeType: 'application/vnd.apple.mpegurl',
          sortOrder: 0,
        }}
      />,
    )

    const video = container.querySelector('video')
    await waitFor(() => expect(hlsMocks.loadSource).toHaveBeenCalledWith('https://cdn.example.com/videos/work-1/hls/master.m3u8'))
    expect(hlsMocks.attachMedia).toHaveBeenCalledWith(video)
  })
})
