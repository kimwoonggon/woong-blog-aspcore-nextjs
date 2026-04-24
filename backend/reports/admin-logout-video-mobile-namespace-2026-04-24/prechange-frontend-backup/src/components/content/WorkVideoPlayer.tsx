'use client'

import { useEffect, useMemo, useRef } from 'react'
import type { WorkVideo } from '@/lib/api/works'

interface WorkVideoPlayerProps {
  video: WorkVideo
}

const hlsMimeType = 'application/vnd.apple.mpegurl'

export function WorkVideoPlayer({ video }: WorkVideoPlayerProps) {
  const videoRef = useRef<HTMLVideoElement>(null)
  const isHlsVideo = useMemo(() => {
    return video.sourceType === 'hls'
      || video.mimeType === hlsMimeType
      || video.playbackUrl?.toLowerCase().endsWith('.m3u8') === true
  }, [video.mimeType, video.playbackUrl, video.sourceType])

  useEffect(() => {
    if (!isHlsVideo || !video.playbackUrl) {
      return
    }

    const element = videoRef.current
    if (!element) {
      return
    }

    if (element.canPlayType(hlsMimeType)) {
      element.src = video.playbackUrl
      return () => {
        element.removeAttribute('src')
      }
    }

    let disposed = false
    let hls: { loadSource: (source: string) => void; attachMedia: (media: HTMLMediaElement) => void; destroy: () => void } | null = null

    void import('hls.js').then(({ default: Hls }) => {
      if (disposed || !Hls.isSupported()) {
        return
      }

      hls = new Hls()
      hls.loadSource(video.playbackUrl!)
      hls.attachMedia(element)
    })

    return () => {
      disposed = true
      hls?.destroy()
    }
  }, [isHlsVideo, video.playbackUrl])

  if (video.sourceType === 'youtube') {
    return (
      <iframe
        src={`https://www.youtube-nocookie.com/embed/${video.sourceKey}`}
        title={video.originalFileName ?? `YouTube video ${video.sourceKey}`}
        allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
        allowFullScreen
        className="aspect-video w-full rounded-xl border border-border/70 bg-black"
      />
    )
  }

  return (
    <video
      ref={videoRef}
      controls
      controlsList="nodownload noremoteplayback"
      disablePictureInPicture
      preload="metadata"
      playsInline
      onContextMenu={(event) => event.preventDefault()}
      className="aspect-video w-full rounded-xl border border-border/70 bg-black"
    >
      {!isHlsVideo ? <source src={video.playbackUrl ?? undefined} type={video.mimeType ?? 'video/mp4'} /> : null}
    </video>
  )
}
