import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { InteractiveRenderer } from '@/components/content/InteractiveRenderer'
import { buildWorkVideoEmbedMarkup } from '@/lib/content/work-video-embeds'

describe('InteractiveRenderer', () => {
  it('renders inline work video embeds between html segments', () => {
    render(
      <InteractiveRenderer
        html={`<p>Before</p>${buildWorkVideoEmbedMarkup('video-1')}<p>After</p>`}
        workVideos={[
          {
            id: 'video-1',
            sourceType: 'youtube',
            sourceKey: 'dQw4w9WgXcQ',
            sortOrder: 0,
          },
        ]}
      />,
    )

    expect(screen.getByText('Before')).toBeInTheDocument()
    expect(screen.getByText('After')).toBeInTheDocument()
    expect(screen.getByTitle(/YouTube video/i)).toHaveAttribute('src', 'https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ')
  })

  it('skips unknown inline video references on public render', () => {
    const { container } = render(
      <InteractiveRenderer
        html={`<p>Before</p>${buildWorkVideoEmbedMarkup('missing-video')}`}
        workVideos={[]}
      />,
    )

    expect(screen.getByText('Before')).toBeInTheDocument()
    expect(container.querySelector('iframe')).toBeNull()
    expect(container.querySelector('video')).toBeNull()
  })

  it('removes script tags, event handlers, and javascript urls from raw html', () => {
    const { container } = render(
      <InteractiveRenderer
        html={'<p onclick="window.evil=true">Safe</p><script>window.evil=true</script><a href="javascript:alert(1)">bad</a><img src="javascript:alert(1)" onerror="alert(1)" />'}
      />,
    )

    expect(screen.getByText('Safe')).toBeInTheDocument()
    expect(container.querySelector('script')).toBeNull()
    expect(container.querySelector('p')).not.toHaveAttribute('onclick')
    expect(container.querySelector('a')).not.toHaveAttribute('href')
    expect(container.querySelector('img')).not.toHaveAttribute('src')
    expect(container.querySelector('img')).not.toHaveAttribute('onerror')
  })
})
