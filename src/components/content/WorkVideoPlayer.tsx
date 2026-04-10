'use client'

import type { WorkVideo } from '@/lib/api/works'

interface WorkVideoPlayerProps {
  video: WorkVideo
}

export function WorkVideoPlayer({ video }: WorkVideoPlayerProps) {
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
      controls
      preload="metadata"
      playsInline
      className="aspect-video w-full rounded-xl border border-border/70 bg-black"
    >
      <source src={video.playbackUrl ?? undefined} type={video.mimeType ?? 'video/mp4'} />
    </video>
  )
}
