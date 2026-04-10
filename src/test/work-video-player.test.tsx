import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { WorkVideoPlayer } from '@/components/content/WorkVideoPlayer'

describe('WorkVideoPlayer', () => {
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
})
